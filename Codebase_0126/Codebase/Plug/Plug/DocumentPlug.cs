using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Contracts;

namespace Maarg.Fatpipe.Plug.DataModel
{
    
    public interface IDocumentPlug:IIdentifier
    {
        int DocumentType { get; } // Used for X12 document plugs only
        string Name { get; }
        IPluglet RootPluglet { get; set; }
        BusinessDomain BusinessDomain { get; }
        IList<string> Error { get; }
        string Identifier { get; set; }
        // Element and segment delimiters are mainly for flat file
        // since it does not have ISA/GS segments
        List<int> ElementDelimiters { get; set; }
        List<int> SegmentDelimiters { get; set; }
    }

    public enum BusinessDomain
    {
        X12,
        Xml,
        XmlEdi,
        FlatFile,
        Hipaa,
        Other
    }
    [Serializable]
    public class DocumentPlug : IDocumentPlug
    {
        IPluglet rootPluglet;
        BusinessDomain businessDomain;
        IList<string> error;

        public DocumentPlug(IPluglet root, BusinessDomain domain)
        {
            this.rootPluglet = root;
            this.businessDomain = domain;
            error = new List<string>(2);
        }

        public DocumentPlug()
            : this(null, BusinessDomain.Other)
        {
        }

        #region Properties
        public int DocumentType { get; set; }

        public string Name 
        {
            get { return this.rootPluglet != null ? this.rootPluglet.Name : string.Empty; } 
        }

        public IPluglet RootPluglet 
        {
            get { return this.rootPluglet; } 
            set { this.rootPluglet = value; } 
        }

        public BusinessDomain BusinessDomain 
        {
            get { return this.businessDomain; }
        }

        public IList<string> Error 
        {
            get { return this.error; }
        }

        public string Identifier { get; set; }

        // Element and segment delimiters are mainly for flat file
        // since it does not have ISA/GS segments
        public List<int> ElementDelimiters { get; set; }
        public List<int> SegmentDelimiters { get; set; }

        #endregion
    }

    public class DocumentPlugHelper
    {
        const string Format1Indented = "{0} {1} {2} {3}";
        const string Format2Flat = "{0} {1} {2}";
        private static void PrintDocumentPlug(IPluglet root, StringBuilder sb, int indent)
        {
            //Format1
            //for (int i = 0; i < indent; i++) sb.Append("\t");
            //string data = string.Format(Format1Indented, root.Name, root.Description, root.PlugletType, root.IsMandatory);
            //sb.Append(data);
            //sb.AppendLine();
            //end Format1

            //Format2
            string description = root.Definition;
            if (!string.IsNullOrEmpty(description))
            {
                description = description.Replace(' ', '#');
            }
            string data = string.Format(Format2Flat, root.PlugletType, root.IsRecursiveMandatory ? "Y" : "N", description);
            sb.Append(data);
            for (int i = 0; i < indent; i++) sb.Append("\t");
            sb.Append(root.Name);
            sb.AppendLine();
            //end Format2
            
            foreach (IPluglet child in root.Children)
            {
                PrintDocumentPlug(child, sb, indent + 1);
            }
        }

        public static void PrintDocumentPlug(IDocumentPlug plug, string path)
        {
            StringBuilder builder = new StringBuilder(10 * 1000);
            PrintDocumentPlug(plug.RootPluglet, builder, 0);
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
            {
                sw.Write("NodeType Mandatory Description TopLevelName Level2Name, Level3Name, Level4Name, Level5Name");
                sw.WriteLine();
                sw.WriteLine();
                sw.Write(builder.ToString());
            }

            Console.WriteLine("Written to " + path);
        }
    }
}
