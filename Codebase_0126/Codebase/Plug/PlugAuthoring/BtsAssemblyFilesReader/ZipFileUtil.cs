using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public static class ZipFileUtil
    {
        public static List<ZipFileEntry> ExtractZipFile(string zipFilePath)
        {
            List<ZipFileEntry> results = null;
            using (ZipArchive zipArchive = ZipFile.OpenRead(zipFilePath))
            {
                results = GetFileEntries(zipArchive);
            }

            return results;
        }

        public static List<ZipFileEntry> ExtractZipFile(Stream zipFile)
        {
            List<ZipFileEntry> results = null;
            using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
            {
                results = GetFileEntries(zipArchive);
            }

            return results;
        }

        public static List<ZipFileEntry> GetFileEntries(ZipArchive zipArchive)
        {
            string extractToFolderName = Path.GetTempPath();
            List<ZipFileEntry> results = new List<ZipFileEntry>();

            foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
            {
                string extractToPath = Path.Combine(extractToFolderName, archiveEntry.Name);
                archiveEntry.ExtractToFile(extractToPath, true);
                ZipFileEntry zfe = new ZipFileEntry();
                using (StreamReader sr = new StreamReader(extractToPath))
                {
                    zfe.FileName = archiveEntry.Name;
                    zfe.Content = sr.ReadToEnd();
                }

                results.Add(zfe);

                File.Delete(extractToPath);
            }

            return results;
        }

        public static void CreateZipFile(string zipFilePath, List<ZipFileEntry> filesToBeZipped)
        {
            if (filesToBeZipped == null || filesToBeZipped.Count == 0)
            {
                return;
            }

            using (FileStream fs = new FileStream(zipFilePath, FileMode.Create))
            {
                using (ZipArchive zipArchive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    foreach (ZipFileEntry fileEntry in filesToBeZipped)
                    {
                        ZipArchiveEntry archiveEntry = zipArchive.CreateEntry(fileEntry.FileName);
                        using (StreamWriter sw = new StreamWriter(archiveEntry.Open()))
                        {
                            sw.Write(fileEntry.Content);
                        }
                    }
                }
            }
        }
    }

    public class ZipFileEntry
    {
        public string FileName { get; set; }

        public string Content { get; set; }
    }
}
