using Maarg.Fatpipe.Plug.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class MapFilesReader
    {
        public const bool TraceOn = true;

        public static bool assemblyResolveSet = false;
        public static string assemblyPath = AppDomain.CurrentDomain.BaseDirectory;

        public static Assembly AssemblyResolver(object sender, ResolveEventArgs args)
        {
            string s = assemblyPath + args.Name.Remove(args.Name.IndexOf(',')) + ".dll";
            return Assembly.LoadFile(s);
        }

        public static BtsAssemblyFilesDetail ReadMapFiles(Stream zipFile, string zipFileName)
        {
            if (assemblyResolveSet == false)
            {
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolver);
                assemblyResolveSet = true;
            }

            BtsAssemblyFilesDetail mapsDetail = new BtsAssemblyFilesDetail()
                {
                    MapName = Path.GetFileNameWithoutExtension(zipFileName)
                };

            string currentFileName = string.Empty;
            try
            {
                string extractToFolderName = Path.GetTempPath();
                assemblyPath = extractToFolderName;
                //using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                //{
                //    foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                //    {
                //        if (string.IsNullOrWhiteSpace(archiveEntry.Name))
                //            continue;

                //        currentFileName = archiveEntry.Name;

                //        string extractToPath = Path.Combine(extractToFolderName, archiveEntry.Name);
                //        archiveEntry.ExtractToFile(extractToPath, true);
                //        using (StreamReader sr = new StreamReader(extractToPath))
                //        {
                //            ReadMapAssembly(archiveEntry.Name, sr.BaseStream, mapsDetail);
                //        }

                //        File.Delete(extractToPath);
                //    }

                //    currentFileName = string.Empty;

                //    ImportSchemas(mapsDetail);
                //}

                List<string> filePathList = new List<string>();
                using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(archiveEntry.Name))
                            continue;

                        currentFileName = archiveEntry.Name;

                        if (!string.Equals(Path.GetExtension(currentFileName), ".dll", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        string extractToPath = Path.Combine(extractToFolderName, archiveEntry.Name);
                        if(File.Exists(extractToPath) == false)
                            archiveEntry.ExtractToFile(extractToPath, true);
                        filePathList.Add(extractToPath);
                    }

                    foreach (string filePath in filePathList)
                    {
                        currentFileName = Path.GetFileName(filePath);
                        using (StreamReader sr = new StreamReader(filePath))
                        {
                            ReadMapAssembly(currentFileName, sr.BaseStream, mapsDetail);
                        }
                    }

                    foreach (string filePath in filePathList)
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch
                        {
                        }
                    }

                    currentFileName = string.Empty;

                    ImportSchemas(mapsDetail);
                }
            }
            catch (Exception e)
            {
                if(string.IsNullOrWhiteSpace(currentFileName) == false)
                    mapsDetail.Errors.Add(string.Format("Error occured while reading {0} file. Error: {1} {2}", currentFileName, e.Message, 
                        e.InnerException == null ? "" : e.InnerException.Message));
                else
                    mapsDetail.Errors.Add(string.Format("Error occured during reading zip file. Error: {0}", e.Message));
            }

            return mapsDetail;
        }

        // Go over all schema, if the schema contains xs:import then add the imported schema to 
        // main schema
        private static void ImportSchemas(BtsAssemblyFilesDetail mapsDetail)
        {
            List<string> finalSchemaNames = mapsDetail.SchemaCollection.Keys.ToList();

            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            XmlSchema schema, mainSchema = null;
            XmlSchemaImport import;
            foreach (string schemaName in mapsDetail.SchemaCollection.Keys)
            {
                if(TraceOn)
                    Console.WriteLine("Adding {0}", schemaName);
                schema = mapsDetail.SchemaCollection[schemaName];

                if (schema == null)
                    continue;

                schemaSet.Add(schema);

                if (schema.Includes != null)
                {
                    if (schema.TargetNamespace == "http://schemas.microsoft.com/BizTalk/EDI/EDIFACT/2006/EnrichedMessageXML")
                        mainSchema = schema;

                    foreach (XmlSchemaExternal external in schema.Includes)
                    {
                        if (mapsDetail.SchemaCollection.ContainsKey(external.SchemaLocation))
                        {
                            import = external as XmlSchemaImport;
                            import.Schema = mapsDetail.SchemaCollection[external.SchemaLocation];
                            finalSchemaNames.Remove(external.SchemaLocation);
                        }
                        else
                        {
                            mapsDetail.Errors.Add(string.Format("Schema location {0} referred in schema {1} not found", external.SchemaLocation, schemaName));
                        }
                    }
                }
            }

            schemaSet.Compile();

            // Get all source schema names
            List<string> sourceSchemaList = new List<string>();
            foreach (BtsAssemblyDetail mapDetail in mapsDetail.Maps)
                foreach (string sourceSchema in mapDetail.SourceSchemas)
                    sourceSchemaList.Add(sourceSchema);

            if (TraceOn)
            {
                foreach (BtsAssemblyDetail detail in mapsDetail.Maps)
                {
                    StringBuilder strB = new StringBuilder();
                    strB.AppendLine(string.Format("Name: {0}", detail.ClassName));
                    foreach(string sourceSchemaName in detail.SourceSchemas)
                        strB.AppendLine(string.Format("Source Schema: {0}", sourceSchemaName));
                    foreach (string targetSchemaName in detail.TargetSchemas)
                        strB.AppendLine(string.Format("Target Schema: {0}", targetSchemaName));
                    File.WriteAllText(string.Format("Map_{0}.txt", detail.ClassName), strB.ToString());
                    File.WriteAllText(string.Format("Map_{0}.xml", detail.ClassName), detail.Map.ToString());
                }
            }

            // Remove all schemas which are imported in other schemas.
            List<string> keys = mapsDetail.SchemaCollection.Keys.ToList();
            foreach (string schemaName in keys)
            {
                // If this condition is changed then update MapsDetail.GetSchemaDetails too
                if (finalSchemaNames.Contains(schemaName) == false
                     ||
                     (schemaName.Contains("Enriched") == false
                     && schemaName.Contains("Interchange") == false)
                    )
                {
                    // TODO: For debugging
                    //using (StreamWriter writer = File.CreateText(schemaName))
                    //    mapsDetail.SchemaCollection[schemaName].Write(writer);

                    mapsDetail.SchemaCollection.Remove(schemaName);
                }
            }

            // Keep schema only if it's part of source schema list
            //List<string> keys = mapsDetail.SchemaCollection.Keys.ToList();
            //foreach (string schemaName in keys)
            //{
            //    if (sourceSchemaList.Contains(schemaName) == false)
            //        mapsDetail.SchemaCollection.Remove(schemaName);
            //}

            //IDocumentPlug documentPlug = XmlSchemaHelper.CreateDocumentPlug(mainSchema);
            //XElement schemaXml = documentPlug.SerializeToXml();
            //schemaXml.Save(@"DocumentPlug.xml");
        }

        private static void ReadMapAssembly(string fileName, Stream stream, BtsAssemblyFilesDetail mapsDetail)
        {
            if (!string.Equals(Path.GetExtension(fileName), ".dll", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (TraceOn)
                Console.WriteLine("Processing {0}", fileName);

            byte[] arr = new Byte[stream.Length];
            stream.Read(arr, 0, (int)stream.Length);
            Assembly assembly = Assembly.Load(arr);

            try
            {
                // Some assemblies throw reflection type load exception if they are not build on same .net version
                // Ignore such libraries
                assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                if (TraceOn)
                    Console.WriteLine("Ignoring processing of {0} due to ReflectionTypeLoadException exception {1}", fileName, 
                        e.LoaderExceptions == null || e.LoaderExceptions.Length == 0 ? "" : e.LoaderExceptions[0].Message);
                return;
            }

            foreach (Type cls in assembly.GetTypes())
            {
                if (cls.IsClass && !cls.IsAbstract)
                {
                    string xmlContent = GetPropertyValue(cls, "XmlContent");
                    string[] sourceSchemas = GetPropertyValues(cls, "SourceSchemas");
                    string[] targetSchemas = GetPropertyValues(cls, "TargetSchemas");

                    // If source schema property is not present then just add schema, otherwise add it as map
                    if (sourceSchemas == null)
                    {
                        if (!string.IsNullOrWhiteSpace(xmlContent) && !mapsDetail.SchemaCollection.ContainsKey(cls.FullName))
                        {
                            XmlSchema schema = XmlSchema.Read(new StringReader(xmlContent), ValidationCallback);
                            mapsDetail.SchemaCollection.Add(cls.FullName, schema);
                            if (TraceOn && cls.FullName.StartsWith("Microsoft.") == false)
                            {
                                Console.WriteLine("\tAdded schema from {0} class", cls.FullName);
                                File.WriteAllText(string.Format("{0}.xml", cls.FullName), xmlContent);
                            }
                        }
                    }
                    else
                    {
                        BtsAssemblyDetail mapDetail = new BtsAssemblyDetail()
                        {
                            ClassName = cls.FullName,
                            Map = XElement.Parse(xmlContent),
                            SourceSchemas = new List<string>(sourceSchemas),
                            TargetSchemas = new List<string>(targetSchemas),
                        };
                        mapsDetail.Maps.Add(mapDetail);

                        //Console.WriteLine("\tAdded MapDetail from {0} class", cls.FullName);
                        //Console.WriteLine("\t\tSource Schemas");
                        //foreach (string sourceSchema in sourceSchemas)
                        //    Console.WriteLine("\t\t\t{0}", sourceSchema);
                        //Console.WriteLine("\t\tTarget Schemas");
                        //foreach (string targetSchema in targetSchemas)
                        //    Console.WriteLine("\t\t\t{0}", targetSchema);
                        //if (sourceSchemas.Length != targetSchemas.Length || sourceSchemas.Length > 1 || targetSchemas.Length > 1)
                        //    Console.WriteLine("More than 1 source or target schema exists");
                    }
                }
            }

            if (TraceOn)
                Console.WriteLine("Completed processing {0}", fileName);
        }

        private static string GetPropertyValue(Type cls, string propertyName)
        {
            string propertyValue = null;
            PropertyInfo property = cls.GetProperty(propertyName);
            if (property != null)
            {
                object obj = Activator.CreateInstance(cls);
                MethodInfo propertyGet = property.GetMethod;
                propertyValue = propertyGet.Invoke(obj, null) as string;
            }

            return propertyValue;
        }

        private static string[] GetPropertyValues(Type cls, string propertyName)
        {
            string[] propertyValues = null;
            PropertyInfo property = cls.GetProperty(propertyName);
            if (property != null)
            {
                object obj = null;

                try
                {
                    obj = Activator.CreateInstance(cls);
                }
                catch (MissingMethodException)
                {
                    if (TraceOn)
                        Console.WriteLine("Ignoring {0} class since CreateInstance throw MissingMethodException", cls.FullName);
                    return null;
                }

                MethodInfo propertyGet = property.GetMethod;
                IEnumerable<string> values = propertyGet.Invoke(obj, null) as IEnumerable<string>;

                if (values != null)
                {
                    propertyValues = values.ToArray();                    
                }
            }

            return propertyValues;
        }

        static void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (TraceOn)
            {
                if (args.Severity == XmlSeverityType.Warning)
                    Console.Write("\tWarning: ");
                else if (args.Severity == XmlSeverityType.Error)
                    Console.Write("\tError: ");

                Console.WriteLine(args.Message);
            }
        }
    }
}
