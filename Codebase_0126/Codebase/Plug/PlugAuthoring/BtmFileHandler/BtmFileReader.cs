using Maarg.Fatpipe.Plug.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maarg.Fatpipe.Plug.Authoring.BtmFileHandler
{
    public class BtmFileReader
    {
        public const bool TraceOn = false;
        public static List<MapDetail> ReadMap(Stream zipFile, string zipFileName, string specCertType)
        {
            string currentFileName = string.Empty;
            List<MapDetail> mapDetailList = new List<MapDetail>();

            //try
            //{
                string extractToFolderName = Path.GetTempPath();

                List<string> filePathList = new List<string>();
                using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(archiveEntry.Name))
                            continue;

                        currentFileName = archiveEntry.Name;
                        string folderName = Path.GetDirectoryName(archiveEntry.FullName);
                        if (folderName.LastIndexOf('\\') != -1)
                            folderName = folderName.Substring(folderName.LastIndexOf('\\') + 1);

                        if (string.Equals(folderName, "deployment", StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        if (string.Equals(folderName, "development", StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        if (string.Equals(folderName, "bin", StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        if (string.Equals(folderName, "debug", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        if (!string.Equals(Path.GetExtension(currentFileName), ".btm", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        if (! (currentFileName.StartsWith("inbound", StringComparison.OrdinalIgnoreCase) 
                            || currentFileName.StartsWith("outbound", StringComparison.OrdinalIgnoreCase)
                            || currentFileName.StartsWith("oubound", StringComparison.OrdinalIgnoreCase)
                            || currentFileName.StartsWith("xml", StringComparison.OrdinalIgnoreCase)))
                            continue;

                        Console.WriteLine("Reading {0}", currentFileName);
                        string extractToPath = Path.Combine(extractToFolderName, archiveEntry.Name);
                        if (File.Exists(extractToPath) == true)
                            File.Delete(extractToPath);

                        archiveEntry.ExtractToFile(extractToPath, true);

                        using (StreamReader sr = new StreamReader(extractToPath))
                        {
                            TransformPlug transformPlug = ReadMap(extractToPath, sr.BaseStream);

                            if (specCertType == "flatfile" || specCertType == "xml")
                            {
                                bool notSupportedBtmFile = false;

                                if (currentFileName.StartsWith("inbound", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (transformPlug.SourceLocation.StartsWith("GCommerce") == true
                                        || transformPlug.TargetLocation.StartsWith("GCommerce") == false)
                                        notSupportedBtmFile = true;
                                }
                                else
                                    if (currentFileName.StartsWith("outbound", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (transformPlug.SourceLocation.StartsWith("GCommerce") == false
                                            || transformPlug.TargetLocation.StartsWith("GCommerce") == true)
                                            notSupportedBtmFile = true;
                                    }

                                if(notSupportedBtmFile == true)
                                    transformPlug = null;
                            }

                            if (transformPlug != null)
                            {
                                mapDetailList.Add(new MapDetail(currentFileName, folderName, transformPlug, specCertType));
                            }
                        }

                        File.Delete(extractToPath);
                    }
                }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(string.Format("Error occured during reading map file. Error: {0}", e.Message));
            //}

            return mapDetailList;
        }

        private static TransformPlug ReadMap(string fileName, Stream mapFile)
        {
            TransformPlug transformPlug = null;

            if (TraceOn)
                Console.WriteLine("Reading map from {0}", fileName);

            transformPlug = (TransformPlug)TransformPlugFactory.CreateTransformPlugFromBTM(mapFile);
            return transformPlug;
        }
    }
}
