using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Maarg.AllAboard;
using Maarg.Contracts.GCValidate;
using Maarg.Fatpipe.Plug.Authoring;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    /// <summary>
    /// Interface for schema upload functionality for Ux.
    /// Note that UploadCertFile function may throw an exception if upload process fails.
    /// </summary>
    public static class SpecCertValidator
    {
        public static SpecCertValidationResult ValidateSpecCert(string certFileName, Stream certFileStream, IDalManager dalManager, out SpecCertFileType certFileType)
        {
            // For error reporting lets create X12 instance of GCExcelToDocumentPlug
            // Once TradingPartnerSpecCertMetadata is created we will change it to appropriate instance
            GCExcelToDocumentPlug gcExcelToDocumentPlug = new X12GCExcelToDocumentPlug();

            certFileType = SpecCertFileType.X12;
            try
            {
                TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata = new TradingPartnerSpecCertMetadata();
                List<string> errors = tradingPartnerSpecCertMetadata.Initialize(certFileName);
                certFileType = (SpecCertFileType)tradingPartnerSpecCertMetadata.FileType;

                gcExcelToDocumentPlug = GCExcelToDocumentPlug.CreateInstance(certFileType);

                if (errors.Count == 0)
                {
                    // Check if this cert file already exist
                    List<TradingPartnerSpecCertMetadata> tradingPartnerSpecCertMetadataList 
                        = dalManager.GetTradingPartnerList(tradingPartnerSpecCertMetadata.DocumentType, string.Empty);

                    if (tradingPartnerSpecCertMetadataList.Any(t => t.TradingPartnerName == tradingPartnerSpecCertMetadata.TradingPartnerName
                            && t.DocumentType == tradingPartnerSpecCertMetadata.DocumentType
                            && t.Direction == tradingPartnerSpecCertMetadata.Direction))
                    {
                        gcExcelToDocumentPlug.SpecCertValidationResult.SegmentDefinitionValidationResults.Add(new SegmentDefinitionValidationResult()
                        {
                            ColumnIndex = "N/A",
                            RowIndex = -1,
                            Type = ResultType.Warning,
                            Description = "Cert file already exist."
                        });
                    }

                    gcExcelToDocumentPlug.GenerateDocumentPlug(certFileStream, tradingPartnerSpecCertMetadata.TradingPartnerName,
                        tradingPartnerSpecCertMetadata.DocumentType, tradingPartnerSpecCertMetadata.Direction, (SpecCertFileType)tradingPartnerSpecCertMetadata.FileType);
                }
                else
                {
                    foreach (string error in errors)
                    {
                        gcExcelToDocumentPlug.SpecCertValidationResult.SegmentDefinitionValidationResults.Add(new SegmentDefinitionValidationResult()
                        {
                            ColumnIndex = "N/A",
                            RowIndex = -1,
                            Type = ResultType.Error,
                            Description = string.Format("Cert file name error: {0}", error)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                gcExcelToDocumentPlug.SpecCertValidationResult.SegmentDefinitionValidationResults.Add(new SegmentDefinitionValidationResult() 
                {
                    ColumnIndex = "N/A",
                    RowIndex = -1,
                    Type = ResultType.Error,
                    Description = string.Format("Unknown error occured ({0}), please contact Maarg", ex.Message)
                });
            }

            return gcExcelToDocumentPlug.SpecCertValidationResult;
        }

        // Should we always overwrite the existing one?
        public static void UploadSpecCert(string certFileName, Stream certFileStream, string userName, IDalManager dalManager)
        {
            TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata = new TradingPartnerSpecCertMetadata();

            // Purposely ignoring Initialize function return type (errors) since I don't expect errors here.
            tradingPartnerSpecCertMetadata.Initialize(certFileName, userName, DateTime.UtcNow);

            dalManager.SaveTradingPartnerSpecCert(certFileStream, tradingPartnerSpecCertMetadata);

            dalManager.SaveTradingPartnerSpecCertMetadata(tradingPartnerSpecCertMetadata);

            SchemaCache.RemoveDocumentPlug(tradingPartnerSpecCertMetadata.SchemaFileName);
        }
    }
}
