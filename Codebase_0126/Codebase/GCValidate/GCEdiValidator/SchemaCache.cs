using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.AllAboard;
using System.IO;
using Maarg.Contracts.GCValidate;
using Maarg.Fatpipe.Plug.Authoring;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    public static class SchemaCache
    {
        private static Cache gcSpecCertCache = CacheManager.GetCache("GCSpecCertCache", 60, 1000);
        private static Cache gcBizRuleCertCache = CacheManager.GetCache("GCBizRuleCertCache", 60, 1000);

        public static IDocumentPlug GetDocumentPlug(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata, IDalManager dalManager)
        {
            IDocumentPlug documentPlug = gcSpecCertCache.GetObject(tradingPartnerSpecCertMetadata.SchemaFileName) as IDocumentPlug;

            if (documentPlug == null)
            {
                Stream schemaStream = dalManager.GetTradingPartnerSpecCert(tradingPartnerSpecCertMetadata);

                if (schemaStream == null)
                    throw new GCEdiValidatorException(string.Format("{0} Schema not found.", tradingPartnerSpecCertMetadata.SchemaFileName));

                GCExcelToDocumentPlug gcExcelToDocumentPlug = GCExcelToDocumentPlug.CreateInstance((SpecCertFileType)tradingPartnerSpecCertMetadata.FileType);

                documentPlug = gcExcelToDocumentPlug.GenerateDocumentPlug(schemaStream, tradingPartnerSpecCertMetadata.TradingPartnerName,
                    tradingPartnerSpecCertMetadata.DocumentType, tradingPartnerSpecCertMetadata.Direction, (SpecCertFileType)tradingPartnerSpecCertMetadata.FileType);

                gcSpecCertCache.AddObject(tradingPartnerSpecCertMetadata.SchemaFileName, documentPlug);
            }

            return documentPlug;
        }

        public static BizRuleSet GetBizRuleSet(BizRuleCertMetadata bizRuleCertMetadata, IDalManager dalManager)
        {
            BizRuleSet bizRuleSet = gcSpecCertCache.GetObject(bizRuleCertMetadata.RuleCertFileName) as BizRuleSet;

            if (bizRuleSet == null)
            {
                Stream bizRuleStream = dalManager.GetBizRuleCert(bizRuleCertMetadata);

                if (bizRuleStream == null)
                    throw new GCEdiValidatorException(string.Format("{0} BizRule not found.", bizRuleCertMetadata.RuleCertFileName));

                GCExcelToBizRuleSet gcExcelToBizRuleSet = new GCExcelToBizRuleSet();

                bizRuleSet = gcExcelToBizRuleSet.GenerateBizRuleSet(bizRuleStream);

                gcBizRuleCertCache.AddObject(bizRuleCertMetadata.RuleCertFileName, bizRuleSet);
            }

            return bizRuleSet;
        }

        public static void RemoveDocumentPlug(string schemaFileName)
        {
            gcSpecCertCache.RemoveNodeAndExpiredElements(schemaFileName);
        }

        public static void RemoveBizRuleCert(string schemaFileName)
        {
            gcBizRuleCertCache.RemoveNodeAndExpiredElements(schemaFileName);
        }
    }
}
