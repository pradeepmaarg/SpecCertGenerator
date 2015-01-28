using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class BizRuleCertValidator
    {
        public static BizRuleCertValidationResult ValidateBizRuleCert(string certFileName, Stream certFileStream, IDalManager dalManager)
        {
            if (GCValidatorHelper.AddDummyDataForCertValidation)
            {
                BizRuleCertValidationResult bizRuleCertValidationResult = new BizRuleCertValidationResult();

                bizRuleCertValidationResult.RuleDefinitionValidationResults = new List<RuleDefinitionValidationResult>();
                bizRuleCertValidationResult.RuleDefinitionValidationResults.Add(new RuleDefinitionValidationResult()
                    {
                        Type = ResultType.Warning,
                        ColumnIndex = "D",
                        RowIndex = 1,
                        Description = "Invalid message domain id."
                    });
                bizRuleCertValidationResult.RuleDefinitionValidationResults.Add(new RuleDefinitionValidationResult()
                    {
                        Type = ResultType.Error,
                        ColumnIndex = "B",
                        RowIndex = 5,
                        Description = "Invalid value for \"Rule Matching Option\"."
                    });

                return bizRuleCertValidationResult;
            }

            GCExcelToBizRuleSet gcExcelToBizRuleSet = new GCExcelToBizRuleSet();

            try
            {
                BizRuleCertMetadata bizRuleCertMetadata = new BizRuleCertMetadata();
                List<string> errors = bizRuleCertMetadata.Initialize(certFileName);

                if (errors.Count == 0)
                {
                    // Check if this cert file already exist
                    List<BizRuleCertMetadata> bizRuleCertMetadataList = dalManager.GetBizRuleCertFileList(bizRuleCertMetadata.TradingPartnerName);

                    if (bizRuleCertMetadataList.Any(t => t.TradingPartnerName == bizRuleCertMetadata.TradingPartnerName
                            && t.RuleCertFileName == bizRuleCertMetadata.RuleCertFileName))
                    {
                        gcExcelToBizRuleSet.BizRuleCertValidationResult.RuleDefinitionValidationResults.Add(new RuleDefinitionValidationResult()
                            {
                                ColumnIndex = "N/A",
                                RowIndex = -1,
                                Type = ResultType.Warning,
                                Description = "Cert file already exist."
                            });
                    }

                    gcExcelToBizRuleSet.GenerateBizRuleSet(certFileStream);
                }
                else
                {
                    foreach (string error in errors)
                    {
                        gcExcelToBizRuleSet.BizRuleCertValidationResult.RuleDefinitionValidationResults.Add(new RuleDefinitionValidationResult()
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
                gcExcelToBizRuleSet.BizRuleCertValidationResult.RuleDefinitionValidationResults.Add(new RuleDefinitionValidationResult()
                    {
                        ColumnIndex = "N/A",
                        RowIndex = -1,
                        Type = ResultType.Error,
                        Description = string.Format("Unknown error occured ({0}), please contact Maarg", ex.Message)
                    });
            }

            return gcExcelToBizRuleSet.BizRuleCertValidationResult;
        }

        // Should we always overwrite the existing one?
        public static void UploadBizRuleCert(string certFileName, Stream certFileStream, string userName, IDalManager dalManager)
        {
            if (GCValidatorHelper.AddDummyDataForCertValidation)
            {
                return;
            }

            BizRuleCertMetadata bizRuleCertMetadata = new BizRuleCertMetadata();

            // Purposely ignoring Initialize function return type (errors) since I don't expect errors here.
            bizRuleCertMetadata.Initialize(certFileName, userName, DateTime.UtcNow);

            dalManager.SaveBizRuleCert(certFileStream, bizRuleCertMetadata);

            dalManager.SaveBizRuleCertMetadata(bizRuleCertMetadata);

            SchemaCache.RemoveBizRuleCert(bizRuleCertMetadata.RuleCertFileName);
        }
    }
}
