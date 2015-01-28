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
    /// Interface for managing biz rule cert files.
    /// </summary>
    public static class ManageBizRuleCerts  
    {
        public static List<BizRuleCertMetadata> GetBizRulesList(IDalManager dalManager)
        {
            if (GCValidatorHelper.AddDummyDataForCertValidation)
            {
                List<BizRuleCertMetadata> bizRuleCerts = new List<BizRuleCertMetadata>();
                bizRuleCerts.Add(new BizRuleCertMetadata() { TradingPartnerName = "3M", RuleCertFileName = "3M - Biz Rule - X12 810-856-850.xlsx" });
                bizRuleCerts.Add(new BizRuleCertMetadata() { TradingPartnerName = "Global", RuleCertFileName = "Global - Biz Rule - X12 810-856-850.xlsx" });

                return bizRuleCerts;
            }

            return dalManager.GetBizRuleCertFileList();
        }

        public static Stream DownloadTradingPartnerSpecCert(BizRuleCertMetadata bizRuleCertMetadata, IDalManager dalManager)
        {
            if (GCValidatorHelper.AddDummyDataForCertValidation)
            {
                throw new NotImplementedException();
            }

            return dalManager.GetBizRuleCert(bizRuleCertMetadata);
        }

        public static void DeleteTradingPartnerSpecCertWithMetadata(BizRuleCertMetadata bizRuleCertMetadata, IDalManager dalManager)
        {
            if (GCValidatorHelper.AddDummyDataForCertValidation)
            {
                return;
            }

            // TODO: Ideally we should keep audit trail of delete 
            // Following function will remove the table entry altogether.
            dalManager.DeleteBizRuleCertMetadata(bizRuleCertMetadata);

            dalManager.DeleteBizRuleCert(bizRuleCertMetadata);

            SchemaCache.RemoveBizRuleCert(bizRuleCertMetadata.RuleCertFileName);
        }
    }
}
