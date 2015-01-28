using Maarg.Fatpipe.Plug.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecCertGenerator
{
    static class Validator
    {
        public static void ValidateInstanceFile(string btmFileName, string instanceFileName)
        {
            string outputDirName = Path.GetFileNameWithoutExtension(btmFileName);
            if (Directory.Exists(outputDirName) == false)
                Directory.CreateDirectory(outputDirName);

            string btmFileDirName = Path.GetDirectoryName(btmFileName);

            ConsoleExtensions.WriteInfo("Reading {0}", Path.GetFileName(btmFileName));

            BTMFileInfo btmFileInfo = BTMFileReader.ReadBTMFile(btmFileName);

            ConsoleExtensions.WriteInfo("\t{0}", btmFileInfo.SourceTree);
            SpecCertGenerator srcSpecCert = new SpecCertGenerator(btmFileInfo.SourceTree, btmFileDirName, outputDirName);
            srcSpecCert.Generate();
            IDocumentPlug srcDocumentPlug = srcSpecCert.DocumentPlug;

            ConsoleExtensions.WriteInfo("\t{0}", btmFileInfo.TargetTree);
            SpecCertGenerator trgSpecCert = new SpecCertGenerator(btmFileInfo.TargetTree, btmFileDirName, outputDirName);
            trgSpecCert.Generate();
            IDocumentPlug trgDocumentPlug = srcSpecCert.DocumentPlug;
        }
    }
}
