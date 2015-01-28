using System;
using System.Collections.Generic;
using Maarg.AllAboard;
using Maarg.Contracts.GCValidate;
using System.IO.Compression;
using Maarg.Fatpipe.Plug.DataModel;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Maarg.Fatpipe.Plug.Authoring;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    public static class BizRuleValidator
    {
        // This function is invoked from Ux
        public static BizRulesValidationResult ValidateBizRules(string userName, string homeOrg, ZipArchive zipArchive, 
            BizRuleCertMetadata bizRuleCertMetadata, IDalManager dalManager)
        {
            if (zipArchive == null)
                throw new ArgumentNullException("zipArchive", "zipFile cannot be empty");

            if (bizRuleCertMetadata == null)
                throw new ArgumentNullException("bizRuleCertMetadata", "bizRuleCertMetadata cannot be empty");

            if (dalManager == null)
                throw new ArgumentNullException("dalManager", "dalManager cannot be null");

            #region DummyCode
            if (GCValidatorHelper.AddDummyDataForInstanceValidation)
            {
                List<BizRuleInfo> ruleInfo1 = new List<BizRuleInfo>();
                ruleInfo1.Add(new BizRuleInfo() { FileName = "Delphi Corp - Spec Cert - 810 - Send.dat", SegmentPath = "BIG->BIG03", Value = "1/2/2014" });
                ruleInfo1.Add(new BizRuleInfo() { FileName = "Delphi Corporation - Spec Cert - 4010 - 856 - receive - Comments.dat", SegmentPath = "PRF->PRF04", Value = "1/3/2014" });
                ruleInfo1.Add(new BizRuleInfo() { FileName = "Delphi Corporation - Spec Cert - 4010 - 850 - send - Comments.dat", SegmentPath = "BEG->BEG05", Value = "1/2/2014" });

                List<BizRuleInfo> ruleInfo2 = new List<BizRuleInfo>();
                ruleInfo2.Add(new BizRuleInfo() { FileName = "Delphi Corp - Spec Cert - 810 - Send", SegmentPath = "BIG->BIG04", Value = "122014" });
                ruleInfo2.Add(new BizRuleInfo() { FileName = "Delphi Corporation - Spec Cert - 4010 - 856 - receive - Comments.dat", SegmentPath = "PRF->PRF01", Value = "122014" });
                ruleInfo2.Add(new BizRuleInfo() { FileName = "Delphi Corporation - Spec Cert - 4010 - 850 - send - Comments.dat", SegmentPath = "BEG->BEG03", Value = "122014" });

                List<BizRuleInfo> ruleInfo3 = new List<BizRuleInfo>();
                ruleInfo3.Add(new BizRuleInfo() { FileName = "Delphi Corp - Spec Cert - 810 - Send.dat", SegmentPath = "IT1->IT101", Value = "123" });
                ruleInfo3.Add(new BizRuleInfo() { FileName = "Delphi Corporation - Spec Cert - 4010 - 856 - receive - Comments.dat", SegmentPath = "LIN->LIN01", Value = "234" });
                ruleInfo3.Add(new BizRuleInfo() { FileName = "Delphi Corporation - Spec Cert - 4010 - 850 - send - Comments.dat", SegmentPath = "PO1->PO1O1", Value = "345" });

                BizRulesValidationResult bizRulesValidationResultDummy = new BizRulesValidationResult() { BizRuleCertName = "3M - Biz Rule - X12 810-856-850" };
                bizRulesValidationResultDummy.BizRuleValidationResults = new List<BizRuleValidationResult>();
                bizRulesValidationResultDummy.BizRuleValidationResults.Add(new BizRuleValidationResult()
                    {
                        RuleName = "Purchase Order Date",
                        Type = ResultType.Error,
                        RuleInfo = ruleInfo1
                    });
                bizRulesValidationResultDummy.BizRuleValidationResults.Add(new BizRuleValidationResult()
                    {
                        RuleName = "Purchase Order Number",
                        Type = ResultType.Success,
                        RuleInfo = ruleInfo2
                    });
                bizRulesValidationResultDummy.BizRuleValidationResults.Add(new BizRuleValidationResult()
                    {
                        RuleName = "Assigned Identification",
                        Type = ResultType.Warning,
                        RuleInfo = ruleInfo3
                    });

                bizRulesValidationResultDummy.EdiValidationResults = new List<EDIValidationResult>();
                List<SegmentValidationResult> segmentValidationResult = new List<SegmentValidationResult>();
                segmentValidationResult.Add(new SegmentValidationResult()
                    {
                        Description = "Invalid Segment XYZ",
                        Name = "XYZ",
                        SequenceNumber = 5,
                        Type = ResultType.Error,
                        StartIndex = 300,
                        EndIndex = 305,
                    });

                bizRulesValidationResultDummy.EdiValidationResults.Add(new EDIValidationResult()
                    {
                        BeautifiedOriginalPayload = "EDI File Contents",
                        FileName = "Delphi Corp - Spec Cert - 810 - Send.dat",
                        SchemaName = "Delphi Corp - Spec Cert - 810 - Send",
                        SegmentValidationResults = new List<SegmentValidationResult>(), // No validation failures
                        TransactionNumbers = new List<string> { "1" },
                        DisplayName = "Delphi Corp - 810 - Send",
                        Type = "X12_810"
                    });
                bizRulesValidationResultDummy.EdiValidationResults.Add(new EDIValidationResult()
                    {
                        BeautifiedOriginalPayload = "EDI File Contents",
                        FileName = "Delphi Corporation - Spec Cert - 4010 - 850 - send - Comments.dat",
                        SchemaName = "Delphi Corporation - Spec Cert - 4010 - 850 - send",
                        SegmentValidationResults = segmentValidationResult,
                        TransactionNumbers = new List<string> { "1" },
                        DisplayName = "Delphi Corporation - 850 - send",
                        Type = "X12_850"
                    });
                bizRulesValidationResultDummy.EdiValidationResults.Add(new EDIValidationResult()
                    {
                        BeautifiedOriginalPayload = "EDI File Contents",
                        FileName = "Delphi Corporation - Spec Cert - 4010 - 856 - receive - Comments.dat",
                        SchemaName = "Delphi Corporation - Spec Cert - 4010 - 856 - receive",
                        SegmentValidationResults = new List<SegmentValidationResult>(), // No validation failures
                        TransactionNumbers = new List<string> { "1" },
                        DisplayName = "Delphi Corporation - 856 - receive",
                        Type = "X12_856"
                    });

                return bizRulesValidationResultDummy;
            }
            #endregion

            BizRulesValidationResult bizRulesValidationResult = null;
            string errorMessage = null;

            try
            {
                BizRuleSet bizRuleSet = SchemaCache.GetBizRuleSet(bizRuleCertMetadata, dalManager);

                List<ZipFileEntry> fileEntries = ZipFileUtil.GetFileEntries(zipArchive);
                List<DocumentPlugValidationInfo> documentPlugValidationInfoList = new List<DocumentPlugValidationInfo>();
                foreach (ZipFileEntry fileEntry in fileEntries)
                {
                    documentPlugValidationInfoList.Add(GetDocumentPlugValidationInfo(fileEntry, dalManager));
                }

                bizRulesValidationResult = ValidateBizRules(userName, homeOrg, bizRuleCertMetadata.RuleCertFileName,
                    bizRuleSet, documentPlugValidationInfoList, dalManager);
            }
            catch (GCEdiValidatorException gcException)
            {
                errorMessage = gcException.Message;
            }
            catch (Exception)
            {
                errorMessage = "Internal error occurred, please contact Maarg";
            }

            if (errorMessage != null)
            {
                // TODO: Add generic error
            }

            return bizRulesValidationResult;
        }

        // This function does not need DalManager and extracted from above method for unit testing purpose.
        public static BizRulesValidationResult ValidateBizRules(string userName, string homeOrg, string ruleCertFileName, BizRuleSet bizRuleSet, 
            List<DocumentPlugValidationInfo> documentPlugValidationInfoList, IDalManager dal)
        {
            BizRulesValidationResult bizRulesValidationResult = new BizRulesValidationResult();
            bizRulesValidationResult.BizRuleCertName = ruleCertFileName;

            // Validate All Edi if DocumentPlug is not null, if it is null then add error
            bizRulesValidationResult.EdiValidationResults = ValidateEdi(documentPlugValidationInfoList);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            bizRulesValidationResult.BizRuleValidationResults = ValidateBizRules(bizRuleSet, documentPlugValidationInfoList);
            sw.Stop();

            bizRulesValidationResult.BizRuleValidationExecutionTime = sw.Elapsed;

            AddUsageEvent(userName, homeOrg, bizRulesValidationResult, dal);

            return bizRulesValidationResult;
        }

        private static void AddUsageEvent(string userName, string homeOrg, BizRulesValidationResult bizRulesValidationResult, IDalManager dal)
        {
            if (bizRulesValidationResult == null)
                return;

            if (bizRulesValidationResult.EdiValidationResults != null)
            {
                string validationStatus = "Error";
                if (bizRulesValidationResult.BizRuleValidationResults != null)
                {
                    validationStatus = bizRulesValidationResult.GetAggregatedResult().ToString();
                }

                string bizRuleCertFileName = Path.GetFileNameWithoutExtension(bizRulesValidationResult.BizRuleCertName);

                foreach (EDIValidationResult result in bizRulesValidationResult.EdiValidationResults)
                {
                    string fileName = result.FileName;
                    string partnerName = result.FileName.Substring(0, result.FileName.IndexOf(" - "));

                    GCValidatorHelper.AddUsageEvent(userName, homeOrg, partnerName, result.DisplayName, result, result.ExecutionTime,
                        fileName, "Biz Rule", dal);

                    // We add usage event for each ST-SE segment in above AddUsageEvent call
                    // however for bizRule validation we currently support only one ST-SE segment.

                    // Add another usage event for each edi file for biz rule validations
                    if (bizRulesValidationResult.BizRuleValidationResults != null)
                    {
                        fileName = string.Format("{0} + {1}", fileName, bizRulesValidationResult.BizRuleCertName);

                        GCValidatorHelper.AddUsageEvent(userName, homeOrg, partnerName, bizRuleCertFileName, validationStatus, 
                            bizRulesValidationResult.BizRuleValidationExecutionTime, fileName, "Biz Rule", dal);
                    }
                }
            }
        }

        private static List<BizRuleValidationResult> ValidateBizRules(BizRuleSet bizRuleSet, List<DocumentPlugValidationInfo> documentPlugValidationInfoList)
        {
            if (bizRuleSet == null)
                throw new ArgumentNullException("bizRuleSet", "bizRuleSet cannot be null");

            if (documentPlugValidationInfoList == null || documentPlugValidationInfoList.Count <= 1)
                throw new ArgumentNullException("documentPlugValidationInfoList", "documentPlugValidationInfoList cannot be null and should have > 1 entries");

            List<BizRuleValidationResult> bizRuleValidationResults = new List<BizRuleValidationResult>();

            List<RuleSegments> ruleSegmentsList = new List<RuleSegments>();
            foreach (int messageDomainId in bizRuleSet.MessageDomainIds)
            {
                RuleSegments ruleSegments = null;
                foreach(DocumentPlugValidationInfo documentPlugValidationInfo in documentPlugValidationInfoList)
                    if (documentPlugValidationInfo.DocumentPlug != null && documentPlugValidationInfo.DocumentPlug.DocumentType == messageDomainId)
                    {
                        ruleSegments = new RuleSegments(documentPlugValidationInfo.FatpipeDocument, documentPlugValidationInfo.SpecCertName);
                        break;
                    }
                ruleSegmentsList.Add(ruleSegments);
            }

            // Conditional groups - first get all BizRuleValidationResult for all groups
            // then for each group check if at least one is successful, in case
            // of success, mark others as success?
            Dictionary<string, List<BizRuleValidationResult>> conditionalRulesResult = new Dictionary<string, List<BizRuleValidationResult>>();
            string segmentValue;
            foreach (string firstSegmentPath in bizRuleSet.BizRules.Keys)
            {
                // TODO: Handle segmentPath being null/empty

                List<BizRule> bizRules = bizRuleSet.BizRules[firstSegmentPath];
                
                bool loopHasValues = true;
                int loopOccurance = 0;
                while (loopHasValues)
                {
                    foreach (BizRule bizRule in bizRules)
                    {
                        string valueToMatch = null;
                        BizRuleValidationResult bizRuleValidationResult = new BizRuleValidationResult();
                        bizRuleValidationResult.RuleName = bizRule.Name;
                        bizRuleValidationResult.Type = ResultType.Success;
                        bizRuleValidationResult.RuleInfo = new List<BizRuleInfo>();

                        int i = 0;
                        bool isRuleSuccessful = true;
                        foreach (SegmentPath segmentPath in bizRule.SegmentPaths)
                        {
                            if (string.IsNullOrWhiteSpace(segmentPath.OriginalPath))
                            {
                                i++;
                                continue;
                            }
                            BizRuleInfo bizRuleInfo = new BizRuleInfo()
                                {
                                    SegmentPath = segmentPath.OriginalPath,
                                };

                            if (ruleSegmentsList[i] != null)
                            {
                                bizRuleInfo.FileName = ruleSegmentsList[i].CertName;

                                IDocumentFragment documentFragment = ruleSegmentsList[i].SelectSegment(segmentPath, valueToMatch);
                                if (documentFragment != null)
                                {
                                    segmentValue = documentFragment.GetDataSegmentValue(segmentPath.DataSegmentName);
                                    if (string.IsNullOrWhiteSpace(valueToMatch) || string.IsNullOrWhiteSpace(segmentPath.Value) == false)
                                    {
                                        valueToMatch = segmentValue;
                                    }
                                    else
                                        if (string.Equals(valueToMatch, segmentValue, StringComparison.OrdinalIgnoreCase) == false)
                                        {
                                            isRuleSuccessful = false;
                                        }
                                    bizRuleInfo.Value = segmentValue;
                                }
                                else if (loopOccurance != 0 && i == 0)
                                {
                                    loopHasValues = false;
                                    break;
                                }
                            }
                            else
                            {
                                bizRuleInfo.FileName = "Not provided";
                            }

                            if (string.IsNullOrWhiteSpace(bizRuleInfo.Value))
                            {
                                bizRuleInfo.Value = "<Not Valued>";
                                isRuleSuccessful = false;
                            }

                            bizRuleValidationResult.RuleInfo.Add(bizRuleInfo);

                            i++;
                        }

                        if (loopHasValues)
                        {
                            // TODO: Conditional mandatory will impact setting type to error.
                            if (isRuleSuccessful == false)
                                bizRuleValidationResult.Type = bizRule.Type == BizRuleType.Optional ? ResultType.Warning : ResultType.Error;


                            if (string.IsNullOrWhiteSpace(bizRule.ConditionalGroupName) == false)
                            {
                                List<BizRuleValidationResult> bizRuleResults;
                                if (conditionalRulesResult.TryGetValue(bizRule.ConditionalGroupName, out bizRuleResults) == false)
                                {
                                    bizRuleResults = new List<BizRuleValidationResult>();
                                    conditionalRulesResult.Add(bizRule.ConditionalGroupName, bizRuleResults);
                                }
                                bizRuleResults.Add(bizRuleValidationResult);
                            }

                            bizRuleValidationResults.Add(bizRuleValidationResult);
                        }
                    }

                    foreach (RuleSegments ruleSegments in ruleSegmentsList)
                    {
                        ruleSegments.MoveCurrentToUsed();
                    }

                    loopOccurance++;
                }
            }

            if (conditionalRulesResult.Count > 0)
            {
                foreach (List<BizRuleValidationResult> bizRuleResults in conditionalRulesResult.Values)
                {
                    if (bizRuleResults.Any(bizRuleResult => bizRuleResult.Type == ResultType.Success))
                    {
                        foreach (BizRuleValidationResult bizRuleResult in bizRuleResults)
                            bizRuleResult.Type = ResultType.Success;
                    }
                }
            }

            return bizRuleValidationResults;
        }

        private static List<EDIValidationResult> ValidateEdi(List<DocumentPlugValidationInfo> documentPlugValidationInfoList)
        {
            if (documentPlugValidationInfoList == null || documentPlugValidationInfoList.Count <= 1)
                throw new ArgumentNullException("documentPlugValidationInfoList", "documentPlugValidationInfoList cannot be null and should have > 1 entries");

            List<EDIValidationResult> ediValidationResults = new List<EDIValidationResult>();
            EDIValidationResult ediValidationResult;
            foreach (DocumentPlugValidationInfo documentPlugValidationInfo in documentPlugValidationInfoList)
            {
                if (documentPlugValidationInfo.DocumentPlug == null)
                {
                    TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata = new TradingPartnerSpecCertMetadata();
                    tradingPartnerSpecCertMetadata.Initialize(documentPlugValidationInfo.SpecCertName);

                    ediValidationResult = new EDIValidationResult();
                    ediValidationResult.BeautifiedOriginalPayload = EdiValidator.FormatEDIData(documentPlugValidationInfo.FileContents);
                    ediValidationResult.FileName = tradingPartnerSpecCertMetadata.SchemaFileName;
                    ediValidationResult.SchemaName = tradingPartnerSpecCertMetadata.SchemaFileName;
                    ediValidationResult.DisplayName = tradingPartnerSpecCertMetadata.GetCertFileDisplayName();
                    ediValidationResult.Type = tradingPartnerSpecCertMetadata.Type;
                    ediValidationResult.SegmentValidationResults = new List<SegmentValidationResult>();
                    ediValidationResult.SegmentValidationResults.Add(new SegmentValidationResult()
                    {
                        Description = tradingPartnerSpecCertMetadata.SchemaFileName + " Spec cert does not exist",
                        EndIndex = -1,
                        Name = "N/A",
                        SequenceNumber = -1,
                        StartIndex = -1,
                        Type = ResultType.Error
                    });
                }
                else
                {
                    IFatpipeDocument fatpipeDocument;

                    ediValidationResult = EdiValidator.ValidateEdi(documentPlugValidationInfo.FileContents, documentPlugValidationInfo.FileName,
                        documentPlugValidationInfo.SpecCertName, documentPlugValidationInfo.DocumentPlug, out fatpipeDocument);

                    documentPlugValidationInfo.FatpipeDocument = fatpipeDocument;
                }

                ediValidationResults.Add(ediValidationResult);
            }

            return ediValidationResults;
        }

        private static DocumentPlugValidationInfo GetDocumentPlugValidationInfo(ZipFileEntry fileEntry, IDalManager dalManager)
        {
            string specCertName = Path.ChangeExtension(fileEntry.FileName, "xlsx");

            DocumentPlugValidationInfo documentPlugValidationInfo = new DocumentPlugValidationInfo()
                {
                    SpecCertName = specCertName,
                    FileContents = fileEntry.Content,
                    FileName = fileEntry.FileName,
                };

            TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata = new TradingPartnerSpecCertMetadata();
            List<string> errors = tradingPartnerSpecCertMetadata.Initialize(specCertName);

            if (errors == null || errors.Count == 0)
            {
                try
                {
                    documentPlugValidationInfo.DocumentPlug = SchemaCache.GetDocumentPlug(tradingPartnerSpecCertMetadata, dalManager);
                }
                catch(Exception)
                {
                    // Ignore error here as we want to add EdiValidationResult error during ValidateEdi call.
                }
            }

            return documentPlugValidationInfo;
        }
    }
}
