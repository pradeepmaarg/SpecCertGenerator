using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Fatpipe.Plug.Authoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace SpecCertGenerator
{
    class SpecCertGenerator
    {
        public IDocumentPlug DocumentPlug { get; set; }
        private static bool SetAssemblyResolver = true;
        private string TreeReference { get; set; }
        private string OutputDirName { get; set; }
        private string BtmFileDirName { get; set; }
        
        public SpecCertGenerator(string treeReference, string btmFileDirName, string outputDirName)
        {
            if (string.IsNullOrWhiteSpace(treeReference))
                throw new ArgumentNullException("treeReference");
            if (string.IsNullOrWhiteSpace(outputDirName))
                throw new ArgumentNullException("outputDirName");
            if (string.IsNullOrWhiteSpace(btmFileDirName))
                throw new ArgumentNullException("btmFileDirName");

            //if (SetAssemblyResolver == true)
            //{
            //    AppDomain.CurrentDomain.AssemblyResolve += LoadAssembly;
            //    SetAssemblyResolver = false;
            //}

            this.TreeReference = treeReference;
            this.OutputDirName = outputDirName;
            this.BtmFileDirName = btmFileDirName;
        }

        public bool Generate()
        {
            bool success = false;
            string treeAssemblyPath = GetAssemblyPath();

            if (string.IsNullOrWhiteSpace(treeAssemblyPath) == false)
            {
                string schema = ReadSchema(treeAssemblyPath);

                if (string.IsNullOrWhiteSpace(schema) == false)
                {
                    GenerateDocumentPlug(schema);
                    //GenerateSpecCert();
                }
            }

            return success;
        }

        private void GenerateSpecCert()
        {
            string specCertFileName = Path.ChangeExtension(TreeReference, "xlsx");

            DocumentPlugtoExcel.GenerateExcelFromDocumentPlug(specCertFileName, DocumentPlug);
        }

        private string GetAssemblyPath()
        {
            string assemblyName = null;
            string assemblyPath = null;

            if (TreeReference.StartsWith("x12", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = TreeReference;
                assemblyName = assemblyName.Replace(".transactions", "");
            }
            else if (TreeReference.StartsWith("GCommerce.EDI", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = TreeReference;
                assemblyName = assemblyName.Replace("._", ".");
                assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(".Schemas"));
            }

            if (string.IsNullOrWhiteSpace(assemblyName) == false)
            {
                assemblyName += ".dll";
                assemblyPath = Directory.GetFiles(BtmFileDirName, assemblyName, SearchOption.AllDirectories).FirstOrDefault();

                if (assemblyPath != null)
                {
                    ConsoleExtensions.WriteInfo("Found {0}", Path.GetFileName(assemblyPath));
                }
                else
                {
                    ConsoleExtensions.WriteError("No assembly {0} found for {1} ", assemblyName, TreeReference);
                }
            }

            return assemblyPath;
        }

        private string ReadSchema(string treeAssemblyPath)
        {
            string schema = null;

            Assembly tree = Assembly.LoadFrom(treeAssemblyPath);
            Type schemaType = tree.GetType(TreeReference);
            PropertyInfo xmlContents = schemaType.GetProperty("XmlContent");
            object obj = Activator.CreateInstance(schemaType);
            MethodInfo xmlContentsGet = xmlContents.GetMethod;
            schema = xmlContentsGet.Invoke(obj, null) as string;

            if (TreeReference.Contains("Interchange_"))
            {
                string importSchemaReference = TreeReference.Replace("Interchange_", "");
                schemaType = tree.GetType(TreeReference);
                xmlContents = schemaType.GetProperty("XmlContent");
                obj = Activator.CreateInstance(schemaType);
                xmlContentsGet = xmlContents.GetMethod;
                string importSchema = xmlContentsGet.Invoke(obj, null) as string;

                //http://msdn.microsoft.com/en-us/library/ms256237%28v=vs.110%29.aspx
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                
            }

            // To avoid XmlException: There is no Unicode byte order mark. Cannot switch to Unicode.
            // http://social.msdn.microsoft.com/Forums/en-US/750b76df-6728-47f3-8199-d9c2b9c9db44/there-is-no-unicode-byte-order-markcannot-switch-to-unicode-error-while-loading-with-xmldocument?forum=xmlandnetfx
            schema = schema.Replace(" encoding=\"utf-16\"", "");

            string tempSchemaFileName = Path.ChangeExtension(TreeReference, ".xsd");

            if(File.Exists(tempSchemaFileName))
                File.Delete(tempSchemaFileName);
            File.AppendAllText(tempSchemaFileName, schema);

            return schema;
        }

        private void GenerateDocumentPlug(string schema)
        {
            if (string.IsNullOrWhiteSpace(schema))
                throw new ArgumentNullException("schema");

            using (Stream schemaStream = GenerateStreamFromString(schema))
            {
                IDocumentPlug documentPlug = DocumentPlugFactory.CreateDocumentPlugFromXmlSchema(schemaStream);

                XElement schemaXml = documentPlug.SerializeToXml();
                schemaXml.Save(Path.Combine(OutputDirName, Path.ChangeExtension(TreeReference, "xml")));

                DocumentPlug = documentPlug;
            }
        }

        //static Assembly LoadAssembly(object sender, ResolveEventArgs args)
        //{
        //    string assemblyName = new AssemblyName(args.Name).Name;
        //    string path = assemblyName + ".dll";

        //    if (!File.Exists(path)) return null;
        //    return Assembly.LoadFrom(path);
        //}

        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
