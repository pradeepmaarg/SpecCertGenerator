using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public interface IPluglet
    {
        //Identification
        string Name { get; set; }
        string Tag { get; } // Tag is Name.Substring(till '_')
        string Definition { get; set; }
        PlugletType PlugletType { get; set; }

        //only applies to data elements
        string DEStandard { get; set; } //Element->AppInfo->STD_Info@Name
        string DENumber { get; set; } //Element->AppInfo->STD_Info@Number

        //Cardinality
        bool IsMandatory { get; set; }
        bool IsRecursiveMandatory { get; set; }
        bool IsRepeatable { get; }
        RepetitionInfo RepetitionInfo { get; }
        bool IsIgnore { get; set; }
        
        //Location and hierarchy
        IList<IPluglet> Children { get; }
        IPluglet Parent { get; set; }
        string Path { get; }
        IList<IPluglet> Attributes { get; }

        string PathSeperator { get; }

        X12BaseDataType DataType { get; set; }

        // Properties added for use in EDIReader
        // Note that these properties are not initialized by default. 
        // EDIPlug\PlugletExtensions.cs has code to initialize these properties
        List<IPluglet> StartSegmentList { get; set; }

        //Required to uniquely identify the node i.e edited
        int NodeId { get; set; }

        // TODO: This should ideally be somewhere else as Pluglets will be shared
        int CurrentOccurrences { get; set; }

        // Following 2 properties are introduced to handle segments like HL where trigger 
        // data field decides which HL segment to select
        // This property will be set to true at parent level if any child pluglet is marked as trigger field
        bool ContainsTriggerChildField { get; set; }
        // This property will be set to true at child (data) level if this pluglet is marked as trigger field
        bool IsTriggerField { get; set; }
    }
    [Serializable]
    public class RepetitionInfo
    {
        int minOccurs;
        int maxOccurs;
        bool isMandatory;

        public RepetitionInfo()
        {
            minOccurs = maxOccurs = -1;
        }

        public RepetitionInfo(int minOccurs, int maxOccurs)
        {
            this.minOccurs = minOccurs;
            this.maxOccurs = maxOccurs;
        }

        #region Properties
        public int MinOccurs
        {
            get { return this.minOccurs; }
            set { this.minOccurs = value; }
        }

        public int MaxOccurs
        {
            get { return this.maxOccurs; }
            set { this.maxOccurs = value; }
        }

        public bool IsMandatory
        {
            get { return this.isMandatory; }
            set { this.isMandatory = value; }
        }

        public bool IsRepeatable
        {
            get { return maxOccurs > 1 || maxOccurs == -1; }
        }
        #endregion
    }

    public class PlugletInput
    {
        //string name, string definition, PlugletType type, IPluglet parent, int minOccurs, int maxOccurs, bool isIgnore, bool addToParent
        public string Name { get; set; }
        public string Definition { get; set; }
        public PlugletType Type { get; set; }
        public IPluglet Parent { get; set; }
        public int MinOccurs { get; set; }
        public int MaxOccurs { get; set; }
        public bool IsIgnore { get; set; }
        public bool AddToParent { get; set; }
        public bool IsTagSameAsName { get; set; }
        public bool ContainsTriggerChildField { get; set; }
        public bool IsTriggerField { get; set; }

        public PlugletInput()
        {
            MinOccurs = 1;
            MaxOccurs = 1;
            IsIgnore = false;
            AddToParent = true;
            IsTagSameAsName = false;
            ContainsTriggerChildField = false;
            IsTriggerField = false;
        }
    }

    [Serializable]
    public class Pluglet : IPluglet
    {
        PlugletType plugletType;
        string name;
        string definition;
        RepetitionInfo repetitionInfo;
        IList<IPluglet> children;
        IPluglet parent;
        string path;
        bool recursiveMandatory;
        string deStandard; //Element->AppInfo->STD_Info@Name
        string deNumber; //Element->AppInfo->STD_Info@Number
        IList<IPluglet> attributes;

        // Depreciated - Use Pluglet(PlugletInput) instead
        public Pluglet(string name, string desc, PlugletType type, IPluglet parent)
            : this (name, desc, type, parent, 1, 1)
        {
        }

        // Depreciated - Use Pluglet(PlugletInput) instead
        public Pluglet(string name, string definition, PlugletType type, IPluglet parent,
            int minOccurs, int maxOccurs) : this(name, definition, type, parent, minOccurs, maxOccurs, false)
        {
        }

        // Depreciated - Use Pluglet(PlugletInput) instead
        public Pluglet(string name, string definition, PlugletType type, IPluglet parent,
            int minOccurs, int maxOccurs, bool isIgnore)
            : this(new PlugletInput() 
                    {
                        Name = name,
                        Definition = definition,
                        Type = type,
                        Parent = parent,
                        MinOccurs = minOccurs,
                        MaxOccurs = maxOccurs,
                        IsIgnore = isIgnore,
                    })
        {
            
        }

        // Having PlutletInput as constructor parameter will make it easier to add
        // more overrides easily by setting default value of that property in PlugletInput
        // c'tor and then setting it wherever we need to override it.
        public Pluglet(PlugletInput plugletInput)
        {
            if (string.IsNullOrWhiteSpace(plugletInput.Name))
            {
                throw new Exception("Element/segment name cannot be empty");
            }

            this.name = plugletInput.Name;

            if (plugletInput.IsTagSameAsName)
                this.Tag = this.name;
            else
            {
                int index = name.IndexOf('_');
                this.Tag = name.Substring(0, index == -1 ? name.Length : index);
            }

            this.definition = plugletInput.Definition;
            this.plugletType = plugletInput.Type;
            this.parent = plugletInput.Parent;
            this.repetitionInfo = new RepetitionInfo(plugletInput.MinOccurs, plugletInput.MaxOccurs);
            this.repetitionInfo.IsMandatory = (this.repetitionInfo.MinOccurs >= 1) ? true : false;
            this.recursiveMandatory = this.repetitionInfo.IsMandatory;
            this.IsIgnore = plugletInput.IsIgnore;
            this.ContainsTriggerChildField = plugletInput.ContainsTriggerChildField;
            this.IsTriggerField = plugletInput.IsTriggerField;
            if (parent != null)
            {
                this.recursiveMandatory = parent.IsRecursiveMandatory && this.IsMandatory;

                if (IsTriggerField)
                    parent.ContainsTriggerChildField = true;
            }

            if (plugletInput.AddToParent && this.parent != null && !this.parent.Children.Contains(this))
            {
                this.parent.Children.Add(this);

                //set Parent's PlugletType appropriately
                switch (this.plugletType)
                {
                    case PlugletType.Data:
                        if (this.parent.PlugletType != PlugletType.CompositeData) this.parent.PlugletType = PlugletType.Segment;
                        break;

                    case PlugletType.Segment:
                        this.parent.PlugletType = PlugletType.Loop;
                        break;

                    case PlugletType.Loop:
                        this.parent.PlugletType = PlugletType.Loop;
                        break;

                    case PlugletType.CompositeData:
                        this.parent.PlugletType = PlugletType.Loop;
                        break;
                }
            }
        }

        #region Properties
        public PlugletType PlugletType 
        {
            get { return this.plugletType; }
            set { this.plugletType = value; }
        }
        
        public string Name 
        {
            get { return this.name; }
            set { this.name = value; }
        }
        
        public string Definition 
        {
            get { return this.definition; }
            set { this.definition = value; }
        }

        public string DEStandard
        {
            get { return this.deStandard; }
            set { this.deStandard = value; }
        }

        public string DENumber
        {
            get { return this.deNumber; }
            set { this.deNumber = value; }
        }

        public bool IsMandatory 
        {
            get { return this.repetitionInfo.IsMandatory; }
            set { this.repetitionInfo.IsMandatory = value; }
        }

        public bool IsRecursiveMandatory
        {
            get { return this.recursiveMandatory; }
            set { this.recursiveMandatory = value; }
        }

        public bool IsRepeatable 
        {
            get { return this.repetitionInfo.IsRepeatable; }
        }

        public RepetitionInfo RepetitionInfo 
        {
            get { return this.repetitionInfo; }
        }

        public bool IsIgnore { get; set; }

        public bool ContainsTriggerChildField { get; set; }

        public bool IsTriggerField { get; set; }

        public IList<IPluglet> Children 
        {
            get 
            {
                //create the collection on demand only. this will ensure dataelements will not waste space by creating an empty list
                if (this.children == null)
                {
                    children = new List<IPluglet>(10);
                }

                return this.children; 
            }
        }

        public IPluglet Parent 
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        public IList<IPluglet> Attributes
        {
            get
            {
                if (this.attributes == null)
                {
                    this.attributes = new List<IPluglet>(10);
                }

                return this.attributes;
            }
        }

        public string PathSeperator { get { return @"->"; } }
        public string Path 
        { 
            get 
            {
                if (string.IsNullOrEmpty(this.path))
                {
                    this.path = this.parent != null ? this.parent.Path + PathSeperator + this.Name : this.Name; 
                }

                return this.path;
            }
        }

        public X12BaseDataType DataType { get; set; }

        // Properties added for use in EDIReader
        // Note that these properties are not initialized by default. 
        // EDIPlug\PlugletExtensions.cs has code to initialize these properties
        public string Tag { get; private set; }
        public List<IPluglet> StartSegmentList { get; set; }
        public override string ToString()
        {
            return Path;
        }

        public int NodeId
        {
            get;
            set;
        }

        // TODO: This should ideally be somewhere else as Pluglets will be shared
        public int CurrentOccurrences { get; set; }
        #endregion
    }

    /// <summary>
    /// Right now, this covers Xml and EDI data where the following rules apply
    /// Loop -> (Loop+, Segment+)
    /// Segment -> Data+
    /// Data -> literal
    /// </summary>
    public enum PlugletType
    {
        Data,
        CompositeData,
        Segment,
        Loop,
        Unknown
    }
}
