using System;
using System.Collections.Generic;
using System.Text;

namespace Maarg.Fatpipe.Plug.DataModel
{
    #region Interfaces
    public interface ITransformPlug
    {
        IDocumentPlug SourceDocument { get; }
        IDocumentPlug TargetDocument { get; }
        IList<ITransformGroup> Facets { get; }
        string SourceLocation { get; }
        string TargetLocation { get; }
    }

    /// <summary>
    /// This is the fundamental data structure that performs transformation of documents.
    /// Transform contains lot of business logic. It's important can not be understated
    /// Eg. a transform in Plug format is almost 6000 lines for important schemas like PO
    /// Imagine so much of code is being auto-generated with innovation. Otherwise, it is hand generated
    /// In either case, whther is auto or hand generated, it's a lot of code to be maintained and serviced
    /// Hence, we need major simplification
    /// 
    /// If you have 1000 such transforms, you are almost hitting million lines of code which the service
    /// has to host, run and service. That's a tall order
    /// 
    /// A Transform contains
    /// 
    /// 1. List of links. Each link has a source and destination
    /// 2. Link can point to 'Path' inside a document or a formula
    /// 3. List of formulas
    /// 5. Formula have parameters, which point back to documents or are literals
    /// </summary>
    public interface ITransformGroup
    {
        string Name { get; }
        IList<ITransformLink> Links { get; }
        IList<IFormula> Formulas { get; }
    }

    public interface ITransformPluglet
    {
        string Name { get; set; }
        string Address { get; set; }
    }

    public interface IReferenceableElement : ITransformPluglet
    {
        ReferenceType ReferenceType { get; set; }
    }

    public interface ITransformLink : ITransformPluglet
    {
        IReferenceableElement Source { get; set; }
        IReferenceableElement Target { get; set; }

        bool Ignore { get; set; }
    }



    public interface IFormula : ITransformPluglet
    {
        string Description { get; }
        FormulaType FormulaType { get; }
        IList<IParameter> Parameters { get; }
        string Expression { get; set; }
        bool Ignore { get; set; }
    }

    public interface IParameter : ITransformPluglet
    {
        IReferenceableElement Reference { get; set; }

        //Likely not needed, retire in future
        string LinkIndex { get; set; }
    }
    #endregion

    #region Enums
    public enum ReferenceType
    {
        Document,
        Formula,
        Literal
    }

    public enum FormulaType
    {
        Copy,     // ignored since it is about copying intermediate nodes
        Equality, // (a == b)
        LogicalOr, // bool1 OR bool2 OR bool3..
        ValueMapping, // if (bool1) then Copy-Value
        StringLeft, //102
        StringRight, //104
        Scripting, //260
        Uppercase, //110
        Concatenate, //107
        Date, //123
        Time, //124
        GreaterThan, //311
        Size, //105
        LogicalExistence, //701
        RecordCount, //322
        Trim, //108 & 109
        Addition, //118
        LogicalString, //317
        NotEqual, // 316
        LogicalAnd, // 321
        CumulativeSum, //324
        Iteration, //474
        Looping, //424
        TableLooping, // 703
        TableExtractor, // 704
        MassCopy, //802
        LogicalNumeric, //319
        DateAndTime, //125
        Index, //323
        NotSupported // default
    }
    #endregion

    #region Implementation
    public abstract class BaseTransformPluglet : ITransformPluglet
    {
        protected string name;
        protected string address;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Address
        {
            get { return this.address; }
            set { this.address = value; }
        }

    }


    public class Parameter : BaseTransformPluglet, IParameter
    {
        IReferenceableElement reference;
        string linkIndex;

        public IReferenceableElement Reference 
        {
            get
            {
                if (this.reference == null)
                {
                    this.reference = new ReferenceableElement();
                }

                return this.reference;
            }

            set { this.reference = value; }
        }

        public string LinkIndex
        {
            get { return this.linkIndex; }
            set { this.linkIndex = value; }
        }
    }

    public class TransformGroup : BaseTransformPluglet, ITransformGroup
    {
        IList<ITransformLink> links;
        IList<IFormula> formulas;

        public TransformGroup(string name)
        {
            this.name = name;
        }

        public IList<ITransformLink> Links 
        {
            get
            {
                if (this.links == null)
                {
                    this.links = new List<ITransformLink>();
                }

                return this.links;
            }
        }

        public IList<IFormula> Formulas 
        {
            get
            {
                if (this.formulas == null)
                {
                    this.formulas = new List<IFormula>();
                }

                return this.formulas;
            }
        }
    }

    public class Formula : BaseTransformPluglet, IFormula
    {
        string description;
        FormulaType formulaType;
        IList<IParameter> parameters;
        bool ignore;
        string expression;

        public Formula(string name, string description, FormulaType formulaType)
        {
            this.name = name;
            this.description = description;
            this.formulaType = formulaType;
            this.ignore = false;
        }


        public string Description
        {
            get { return this.description; }
        }

        public string Expression
        {
            get { return this.expression; }
            set { this.expression = value; }
        }

        public IList<IParameter> Parameters
        {
            get 
            {
                if (this.parameters == null)
                {
                    this.parameters = new List<IParameter>();
                }

                return this.parameters; 
            }
        }

        public FormulaType FormulaType
        {
            get { return this.formulaType; }
        }

        public bool Ignore
        {
            get { return this.ignore; }
            set { this.ignore = value; }
        }
    }
    
    public class TransformPlug : ITransformPlug
    {
        IDocumentPlug sourceDocument;
        IDocumentPlug targetDocument;
        IList<ITransformGroup> transformGroup;

        public TransformPlug(IDocumentPlug source, IDocumentPlug target, IList<ITransformGroup> transformGroup)
        {
            this.sourceDocument = source;
            this.targetDocument = target;
            this.transformGroup = transformGroup;
        }

        public TransformPlug(IDocumentPlug source, IDocumentPlug target, IList<ITransformGroup> transformGroup, string sourceLocation, string targetLocation)
            :this(source, target, transformGroup)
        {
            this.SourceLocation = sourceLocation;
            this.TargetLocation = targetLocation;
        }


        public IDocumentPlug SourceDocument 
        {
            get { return this.sourceDocument; }
        }

        public IDocumentPlug TargetDocument 
        {
            get { return this.targetDocument; } 
        }

        public string SourceLocation { get; private set; }
        public string TargetLocation { get; private set; }

        public IList<ITransformGroup> Facets 
        {
            get 
            {
                if (this.transformGroup == null)
                {
                    this.transformGroup = new List<ITransformGroup>();
                }

                return this.transformGroup; 
            }
        }
    }

    public class ReferenceableElement : BaseTransformPluglet, IReferenceableElement
    {
        protected ReferenceType referenceType;

        public ReferenceType ReferenceType 
        {
            get { return this.referenceType; }
            set { this.referenceType = value; }
        }

        public override string ToString()
        {
            if(string.IsNullOrWhiteSpace(Address))
                return string.Format("{0} - {1}", ReferenceType, Name);

            return string.Format("{0} - {1} - {2}", ReferenceType, Name, Address);
        }
    }

    public class TransformLink : BaseTransformPluglet, ITransformLink
    {
        IReferenceableElement source;
        IReferenceableElement target;

        bool ignore;

        public TransformLink(string name)
        {
            this.name = name;
            ignore = false;
        }

        public IReferenceableElement Source
        {
            get 
            {
                if (this.source == null)
                {
                    this.source = new ReferenceableElement();
                }

                return this.source; 
            }

            set { this.source = value; }
        }

        public IReferenceableElement Target
        {
            get 
            {
                if (this.target == null)
                {
                    this.target = new ReferenceableElement();
                }

                return this.target; 
            }

            set { this.target = value; }
        }

        public bool Ignore
        {
            get { return this.ignore; }
            set { this.ignore = value; }
        }
    }
#endregion

}
