using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Maarg.AllAboard;
using Maarg.Contracts.GCValidate;
using System.Data.Services.Client;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Threading;

namespace Maarg.Dal.GCValidate
{
    public class DALTradingPartnerSpecCertMetadata
    {
        private string TradingPartnerSpecCertTableName;

        public const int Http200 = 200;
        public const int Http201 = 201;
        public const int Http202 = 202;
        private TableServiceContext tableContext;
        private CloudTableClient tableClient;
        private CloudStorageAccount account;

        private static string CacheName = "TradingPartnerSpecCertsCache";

        private static Cache tradingPartnerSpecCertsCache = CacheManager.GetCache(CacheName, 15 * 60, 1000);

        private List<TradingPartnerSpecCertMetadata> TradingPartnerSpecCerts;
        
        public DALTradingPartnerSpecCertMetadata(CloudStorageAccount account)
        {
            this.account = account;

            TradingPartnerSpecCertTableName = RoleEnvironment.GetConfigurationSettingValue("Tenant.TradingPartnerSpecCertTableName");

            TradingPartnerSpecCerts = tradingPartnerSpecCertsCache.GetObject(CacheName) as List<TradingPartnerSpecCertMetadata>;

            if (TradingPartnerSpecCerts == null)
            {
                TradingPartnerSpecCerts = GetAllTradingPartnerSpecCert();
                tradingPartnerSpecCertsCache.AddObject(CacheName, TradingPartnerSpecCerts);
            }
        }

        public bool SaveTradingPartnerSpecCertMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCert)
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(TradingPartnerSpecCertTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            tradingPartnerSpecCert.PartitionKey = tradingPartnerSpecCert.TradingPartnerName;
            tradingPartnerSpecCert.RowKey = string.Format("{0}_{1}", tradingPartnerSpecCert.DocumentType, tradingPartnerSpecCert.Direction);
            tradingPartnerSpecCert.Timestamp = DateTime.UtcNow;

            if(TradingPartnerSpecCerts.FirstOrDefault(t => t.PartitionKey == tradingPartnerSpecCert.PartitionKey && t.RowKey == tradingPartnerSpecCert.RowKey) == null)
                TradingPartnerSpecCerts.Add(tradingPartnerSpecCert);

            // We need upsert functionality here, hence removing AddObject call and adding UpdateObject
            // this.tableContext.AddObject(TradingPartnerSpecCertTableName, tradingPartnerSpecCert);
            // http://social.msdn.microsoft.com/Forums/windowsazure/en-US/892340f1-bfe1-4433-9246-b617abe6078c/upsert-operation-in-the-table
            // http://msdn.microsoft.com/en-us/library/windowsazure/hh452242.aspx
            // http://www.windowsazure.com/en-us/develop/net/how-to-guides/table-services/#replace-entity
            tableContext.AttachTo(TradingPartnerSpecCertTableName, tradingPartnerSpecCert);
            tableContext.UpdateObject(tradingPartnerSpecCert);

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);

            return response.BatchStatusCode == Http200 || response.BatchStatusCode == Http201 || response.BatchStatusCode == Http202;
        }

        public bool DeleteTradingPartnerSpecCertMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCert)
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(TradingPartnerSpecCertTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            tradingPartnerSpecCert.PartitionKey = tradingPartnerSpecCert.TradingPartnerName;
            tradingPartnerSpecCert.RowKey = string.Format("{0}_{1}", tradingPartnerSpecCert.DocumentType, tradingPartnerSpecCert.Direction);
            tradingPartnerSpecCert.Timestamp = DateTime.UtcNow;

            TradingPartnerSpecCerts.Remove(tradingPartnerSpecCert);

            tableContext.AttachTo(TradingPartnerSpecCertTableName, tradingPartnerSpecCert, "*");
            tableContext.DeleteObject(tradingPartnerSpecCert);

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);

            return response.BatchStatusCode == Http200 || response.BatchStatusCode == Http201 || response.BatchStatusCode == Http202;
        }

        public List<string> GetHomeOrgList()
        {
            List<string> tradingPartnerNames = new List<string>();
            TradingPartnerSpecCerts.ForEach(tp => tradingPartnerNames.Add(tp.TradingPartnerName));

            tradingPartnerNames = tradingPartnerNames.Distinct().ToList();

            return tradingPartnerNames;
        }

        public List<TradingPartnerSpecCertMetadata> GetCertFileList()
        {
            return TradingPartnerSpecCerts;
        }

        public List<TradingPartnerSpecCertMetadata> GetCertFileList(string tradingPartnerName)
        {
            return TradingPartnerSpecCerts.Where(tp => tp.TradingPartnerName == tradingPartnerName).ToList();
        }

        public List<TradingPartnerSpecCertMetadata> GetTradingPartnerList(int documentType, string excludeTradingPartner)
        {
            return TradingPartnerSpecCerts.Where(tp => tp.DocumentType == documentType && tp.TradingPartnerName != excludeTradingPartner).ToList();
        }

        private List<TradingPartnerSpecCertMetadata> GetAllTradingPartnerSpecCert()
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(TradingPartnerSpecCertTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            List<TradingPartnerSpecCertMetadata> tradingPartnerSpecCerts = 
                            (from tradingPartnerSpecCertMetadata in
                                 tableContext.CreateQuery<TradingPartnerSpecCertMetadata>(TradingPartnerSpecCertTableName)
                                 select tradingPartnerSpecCertMetadata).ToList();

            return tradingPartnerSpecCerts;
        }
    }
}
