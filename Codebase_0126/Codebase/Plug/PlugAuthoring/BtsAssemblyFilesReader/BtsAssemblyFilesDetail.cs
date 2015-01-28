using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class BtsAssemblyDetail
    {
        //public int documentType { get; set; }
        public string ClassName { get; set; }
        public List<string> SourceSchemas { get; set; }
        public List<string> TargetSchemas { get; set; }
        public XElement Map { get; set; }

        public BtsAssemblyDetail()
        {
            SourceSchemas = new List<string>();
            TargetSchemas = new List<string>();
        }
    }

    public class SchemaDetails
    {
        public int DocumentType { get; set; }
        public string Direction { get; set; }
        public string version { get; set; }
    }

    public class BtsAssemblyFilesDetail
    {
        // MapName = Zip file name without extension
        public string MapName { get; set; }

        public List<BtsAssemblyDetail> Maps { get; set; }
        public Dictionary<string, XmlSchema> SchemaCollection { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public BtsAssemblyFilesDetail()
        {
            Maps = new List<BtsAssemblyDetail>();
            SchemaCollection = new Dictionary<string, XmlSchema>();
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public static SchemaDetails GetSchemaDetails(string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
                return null;

            SchemaDetails schemaDetails = new SchemaDetails();

            try
            {
                string str = schemaName.Substring(schemaName.LastIndexOf(".")+1);
                string[] subParts = str.Split('_');
                if (subParts == null || subParts.Length != 4)
                    throw new Exception(string.Format("Couldn't extract version, document type etc. from schema name {0}", schemaName));

                if (string.Equals(subParts[0], "Enriched", StringComparison.InvariantCultureIgnoreCase))
                    schemaDetails.Direction = "Receive";
                else
                    schemaDetails.Direction = "Send";

                schemaDetails.version = subParts[2];
                schemaDetails.DocumentType = int.Parse(subParts[3]);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Extracting schema details from schema name {0} failed. Exception: {1}", schemaName, e.Message));
            }

            return schemaDetails;
        }
    }

    //// Class is marked as internal since this is used only during maps file reading and 
    //// should not be used outside reading map file functionality
    //internal class MapAssemblyDetail
    //{
    //    public int documentType { get; set; }
    //    public string SourceSchemaName { get; set; }
    //    public string TargetSchemaName { get; set; }
    //    public XElement Map { get; set; }
    //    public Dictionary<string, XElement> SchemaCollection { get; set; }

    //    internal MapAssemblyDetail()
    //    {
    //        SchemaCollection = new Dictionary<string, XElement>();
    //    }
    //}
}
