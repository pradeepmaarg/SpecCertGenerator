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
    /// Interface for managing spec cert files.
    /// </summary>
    public static class ManageSpecCerts 
    {
        public static List<TradingPartnerSpecCertMetadata> GetCertFileList(IDalManager dalManager)
        {
            return dalManager.GetCertFileList();
        }

        public static Stream DownloadTradingPartnerSpecCert(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata, IDalManager dalManager)
        {
            return dalManager.GetTradingPartnerSpecCert(tradingPartnerSpecCertMetadata);
        }

        public static void DeleteTradingPartnerSpecCertWithMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata, IDalManager dalManager)
        {
            // TODO: Ideally we should keep audit trail of delete 
            // Following function will remove the table entry altogether.
            dalManager.DeleteTradingPartnerSpecCertMetadata(tradingPartnerSpecCertMetadata);

            dalManager.DeleteTradingPartnerSpecCert(tradingPartnerSpecCertMetadata);

            SchemaCache.RemoveDocumentPlug(tradingPartnerSpecCertMetadata.SchemaFileName);
        }
    }
}
