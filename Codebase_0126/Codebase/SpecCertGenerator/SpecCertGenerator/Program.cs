using Maarg.Fatpipe.Plug.Authoring;
using Maarg.Fatpipe.Plug.Authoring.BtmFileHandler;
using Maarg.Fatpipe.Plug.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Maarg.Fatpipe.Plug.Authoring;
using Maarg.Fatpipe.EDIPlug.GCEdiValidator;
using Maarg.Contracts.GCValidate;

namespace SpecCertGenerator
{
    class Program
    {
        class Parameters
        {
            public string Option { get; set; }
            public string ZipFileName { get; set; }
            public string SpecCertName { get; set; }
            public string InstanceFileName { get; set; }
            public bool UploadSpecCert { get; set; }
            public string SpecCertType { get; set; }
        }

        static int Main(string[] args)
        {
            Parameters parameters = ParseArguments(args);

            if (parameters == null)
            {
                ConsoleExtensions.WriteError("Invalid arguments");
                ConsoleExtensions.WriteInfo("Usage: SpecCertGenerator -generate Edi|Xml|FlatFile [-upload] <maps zip file>");
                ConsoleExtensions.WriteInfo("Usage: SpecCertGenerator -validate <spec cert file> <instance file>");
                return -1;
            }

            int returnValue = 0;

            switch (parameters.Option)
            {
                case "generate" :
                    returnValue = ReadAllBtmFiles(parameters);
                    break;

                case "validate":
                    returnValue = ValidateInstanceFile(parameters);
                    break;
            }

            return returnValue;
        }

        private static Parameters ParseArguments(string[] args)
        {
            Parameters parameters = null;

            if (args.Length >= 2)
            {
                parameters = new Parameters();

                switch (args[0].ToLower())
                {
                    case "-generate":
                        parameters.Option = "generate";
                        parameters.SpecCertType = args[1].ToLower();
                        if (!(parameters.SpecCertType == "edi"
                            || parameters.SpecCertType == "xml"
                            || parameters.SpecCertType == "flatfile"))
                            parameters = null;
                        else
                        {
                            if (args.Length < 4)
                            {
                                parameters.ZipFileName = args[2];
                            }
                            else
                            {
                                if (string.Equals(args[2], "-upload", StringComparison.OrdinalIgnoreCase))
                                {
                                    parameters.UploadSpecCert = true;
                                    parameters.ZipFileName = args[3];
                                }
                                else
                                    parameters = null;
                            }
                        }
                        break;

                    case "-validate":
                        if (args.Length < 3)
                            parameters = null;
                        else
                        {
                            parameters.Option = "validate";
                            parameters.SpecCertName = args[1];
                            parameters.InstanceFileName = args[2];
                        }
                        break;

                    default:
                        parameters = null;
                        break;
                }
            }

            return parameters;
        }

        #region Generate spec cert

        private static int ReadAllBtmFiles(Parameters parameters)
        {
            string fileName = parameters.ZipFileName;
            if (File.Exists(fileName) == false)
            {
                ConsoleExtensions.WriteError("{0} does not exist.", fileName);
                return 1;
            }

            if (Path.GetExtension(fileName) != ".zip")
            {
                ConsoleExtensions.WriteError("{0} is not a zip file.", fileName);
                return 1;
            }

            string logFileName = Path.ChangeExtension(Path.GetFileName(fileName), "log");
            ConsoleExtensions.WriteInfo("Log file for this processing: {0}", logFileName);
            using (StreamWriter logFile = new StreamWriter(logFileName))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(fileName))
                    {
                        LogInformation(logFile, string.Format("Reading maps from {0}", fileName));

                        List<MapDetail> mapsDetailList = BtmFileReader.ReadMap(sr.BaseStream, fileName, parameters.SpecCertType);

                        if (mapsDetailList != null)
                        {
                            LogInformation(logFile, string.Format("{0} maps retrieved.", mapsDetailList.Count));
                            int specCertCount = 0;

                            foreach (MapDetail mapDetail in mapsDetailList)
                            {
                                if (mapDetail.DocumentType != 810
                                    && mapDetail.DocumentType != 850
                                    && mapDetail.DocumentType != 856)
                                {
                                    //LogInformation(logFile, string.Format("Spec cert generation for document type {0} is not supported", mapDetail.DocumentType));
                                    continue;
                                }

                                LogInformation(logFile, "======================================");
                                LogInformation(logFile, "Map detail:");
                                LogInformation(logFile, string.Format("\t{0}", mapDetail.FileName));
                                LogInformation(logFile, string.Format("\t{0}", mapDetail.FolderName));

                                Maarg.Fatpipe.Plug.Authoring.BtmFileHandler.SpecCertGenerator specCertGenerator = new Maarg.Fatpipe.Plug.Authoring.BtmFileHandler.SpecCertGenerator();
                                try
                                {
                                    SpecCertGenerationResult result = specCertGenerator.GenerateSpecCert(mapDetail, parameters.SpecCertType);

                                    if (result.PathsUsed.Count == 0)
                                    {
                                        LogError(logFile, "No path exist from map in template. No spec cert generated");
                                    }
                                    else
                                    {
                                        LogInformation(logFile, string.Format("Paths used {0}", result.PathsUsed.Count));
                                        specCertCount++;
                                    }

                                    if (result.Errors.Count != 0 || result.PathsUsed.Count == 0)
                                        LogWarning(logFile, "Spec cert generated with errors");
                                    else
                                        LogInformation(logFile, "Spec cert generated successfully");

                                    if (result.SpecCertGenerated)
                                    {
                                        ValidateAndUploadSpecCert(mapDetail, result, logFile, parameters.UploadSpecCert, parameters.SpecCertType);
                                    }

                                    if (result.Errors.Count > 0)
                                    {
                                        CreateErrorLogFile(fileName, mapDetail, result, logFile);
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogError(logFile, string.Format("Spec cert generation failed. Exception: {0}", e.ToString()));
                                }
                            }

                            if(specCertCount == 0)
                                LogError(logFile, "No valid spec cert generated");
                        }
                        else
                            LogInformation(logFile, string.Format("No map present in {0}", fileName));
                    }
                }
                catch (Exception ex)
                {
                    LogError(logFile, string.Format("Error encountered during processing {0} file. Error: {1}", fileName, ex.ToString()));
                }
            }

            return 0;
        }

        private static void CreateErrorLogFile(string fileName, MapDetail mapDetail, SpecCertGenerationResult result, StreamWriter logFile)
        {
            string errorFileName = string.Format("{0} - {1}", Path.GetFileNameWithoutExtension(fileName), Path.ChangeExtension(mapDetail.FileName, "err"));
            LogInformation(logFile, string.Format("Writing errors to {0} file", errorFileName));

            using (StreamWriter errorFile = new StreamWriter(errorFileName))
            {
                errorFile.WriteLine("Errors encountered during spec cert generation/validation from {0} file", mapDetail.FileName);
                errorFile.WriteLine("Errors:");
                foreach (string error in result.Errors)
                {
                    errorFile.WriteLine(error);
                }
            }
        }

        private static void ValidateAndUploadSpecCert(MapDetail mapDetail, SpecCertGenerationResult result, StreamWriter logFile, bool uploadSpecCert, string specCertType)
        {
            LogInformation(logFile, string.Format("Generating document plug from spec cert {0}", Path.GetFileName(result.SpecCertPath)));

            using (StreamReader stream = new StreamReader(result.SpecCertPath))
            {
                try
                {
                    GCExcelToDocumentPlug excelToDocumentPlug;
                    switch (specCertType)
                    {
                        case "edi":
                            excelToDocumentPlug = new X12GCExcelToDocumentPlug();
                            break;

                        case "xml":
                            excelToDocumentPlug = new XmlGCExcelToDocumentPlug();
                            break;

                        case "flatfile":
                            excelToDocumentPlug = new FlatFileGCExcelToDocumentPlug();
                            break;

                        default:
                            throw new NotSupportedException(string.Format("Spec cert type {0} is not supported", specCertType));
                            break;
                    }

                    IDocumentPlug plug = excelToDocumentPlug.GenerateDocumentPlug(stream.BaseStream, mapDetail.OrgName, mapDetail.DocumentType, mapDetail.Direction, Maarg.Contracts.GCValidate.SpecCertFileType.X12);

                    // Serialize document plug for investigation
                    plug.SerializeToXml().Save(Path.ChangeExtension(result.SpecCertPath, "xml"));

                    if (uploadSpecCert)
                    {
                        // TODO: Add logic to upload to Azure blob
                        LogWarning(logFile, "Upload functionality will be added soon.");
                    }

                    LogInformation(logFile, string.Format("Document plug generated successfully"));
                }
                catch (Exception ex)
                {
                    result.Errors.Add(string.Format("Spec cert validation failed. Error: {0}", ex.ToString()));
                    LogError(logFile, string.Format("Spec cert validation failed. Error: {0}", ex.ToString()));
                }
            }
        }

        #endregion

        #region Validate instance file

        private static int ValidateInstanceFile(Parameters parameters)
        {
            string specCertFullPath = parameters.SpecCertName;
            string specCertFileName = Path.GetFileName(specCertFullPath);
            string instanceFilePath = parameters.InstanceFileName;
            string instanceFileName = Path.GetFileName(instanceFilePath);

            if (File.Exists(specCertFullPath) == false)
            {
                ConsoleExtensions.WriteError("{0} does not exist.", specCertFullPath);
                return 1;
            }

            if (Path.GetExtension(specCertFullPath) != ".xlsx")
            {
                ConsoleExtensions.WriteError("{0} is not a zip file.", specCertFullPath);
                return 1;
            }

            if (File.Exists(instanceFilePath) == false)
            {
                ConsoleExtensions.WriteError("{0} does not exist.", instanceFilePath);
                return 1;
            }

            string logFileName = Path.ChangeExtension(instanceFileName, "log");
            ConsoleExtensions.WriteInfo("Log file for this processing: {0}", logFileName);

            string instanceFileData = File.ReadAllText(instanceFilePath);

            using (StreamWriter logFile = new StreamWriter(logFileName))
            {
                try
                {
                    TradingPartnerSpecCertMetadata metadata = new TradingPartnerSpecCertMetadata();
                    metadata.Initialize(specCertFileName);

                    GCExcelToDocumentPlug excelToDocumentPlug;
                    switch ((SpecCertFileType)metadata.FileType)
                    {
                        case SpecCertFileType.X12:
                            excelToDocumentPlug = new X12GCExcelToDocumentPlug();
                            break;

                        case SpecCertFileType.Xml:
                            excelToDocumentPlug = new XmlGCExcelToDocumentPlug();
                            break;

                        case SpecCertFileType.FlatFile:
                            excelToDocumentPlug = new FlatFileGCExcelToDocumentPlug();
                            break;

                        default:
                            throw new NotSupportedException(string.Format("Spec cert type {0} is not supported", (SpecCertFileType)metadata.FileType));
                            break;
                    }

                    LogInformation(logFile, string.Format("Generating document plug from spec cert {0}", specCertFileName));

                    TradingPartnerSpecCertMetadata specCertMetadata = new TradingPartnerSpecCertMetadata();
                    specCertMetadata.Initialize(specCertFileName);

                    IDocumentPlug documentPlug = null;
                    using (StreamReader stream = new StreamReader(specCertFullPath))
                        documentPlug = excelToDocumentPlug.GenerateDocumentPlug(
                            stream.BaseStream, specCertMetadata.TradingPartnerName, specCertMetadata.DocumentType, 
                            specCertMetadata.Direction, Maarg.Contracts.GCValidate.SpecCertFileType.X12);

                    if (documentPlug == null)
                    {
                        LogError(logFile, "Document plug generation failed");
                        return -1;
                    }
                    else
                        LogInformation(logFile, "Document plug generated successfully");
                    LogInformation(logFile, "Validating instance file");

                    IFatpipeDocument fatpipeDocument;
                    EDIValidationResult result = EdiValidator.ValidateEdi(instanceFileData, instanceFileName, specCertFileName, documentPlug, out fatpipeDocument);

                    LogValidationResult(result, logFile);
                }
                catch (Exception ex)
                {
                    LogError(logFile, string.Format("Error encountered during validating {0} file. Error: {1}", instanceFileName, ex.ToString()));
                }
            }

            return 0;
        }

        private static void LogValidationResult(EDIValidationResult result, StreamWriter logFile)
        {
            if (result != null && result.SegmentValidationResults != null && result.SegmentValidationResults.Count > 0)
            {
                bool errors = false;
                foreach (SegmentValidationResult segmentValidationResult in result.SegmentValidationResults)
                {
                    if (segmentValidationResult.Type == ResultType.Error)
                    {
                        errors = true;
                    }

                    string message = string.Format("SequenceNumber: {0}, Name: {1}, StartIndex: {2}, EndIndex: {3}, Description: {4}"
                        , segmentValidationResult.SequenceNumber, segmentValidationResult.Name, segmentValidationResult.StartIndex
                        , segmentValidationResult.EndIndex, segmentValidationResult.Description);

                    if (segmentValidationResult.Type == ResultType.Error)
                        LogError(logFile, message);
                    else
                        LogWarning(logFile, message);
                }

                if (errors)
                    LogError(logFile, "Validation result: Failed");
                else
                    LogWarning(logFile, "Validation result: Passed with warnings");
            }
            else
            {
                LogInformation(logFile, "Validation result: Passed");
            }
        }

        #endregion

        #region Common Functions


        private static void LogInformation(StreamWriter logFile, string message)
        {
            LogMessage(logFile, message, 1);
        }

        private static void LogWarning(StreamWriter logFile, string message)
        {
            LogMessage(logFile, message, 2);
        }

        private static void LogError(StreamWriter logFile, string message)
        {
            LogMessage(logFile, message, 3);
        }

        private static void LogMessage(StreamWriter logFile, string message, int type)
        {
            logFile.WriteLine(string.Format("{0} - {1}", type == 1 ? "INFO" : type == 2 ? "WARNING" : "ERROR", message));

            message = message.Replace('{', '[');
            message = message.Replace('}', ']');

            switch (type)
            {
                case 1:
                    ConsoleExtensions.WriteInfo(message);
                    break;
                case 2:
                    ConsoleExtensions.WriteWarning(message);
                    break;
                case 3:
                    ConsoleExtensions.WriteError(message);
                    break;
            }
        }

        #endregion
    }
}
