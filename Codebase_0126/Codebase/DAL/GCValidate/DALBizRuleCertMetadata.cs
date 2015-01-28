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
    public class DALBizRuleCertMetadata
    {
        private string BizRuleCertTableName;

        public const int Http200 = 200;
        public const int Http201 = 201;
        public const int Http202 = 202;
        private TableServiceContext tableContext;
        private CloudTableClient tableClient;
        private CloudStorageAccount account;

        private static string CacheName = "BizRuleCertsCache";

        private static Cache bizRuleCertsCache = CacheManager.GetCache(CacheName, 15*60, 1000);

        private List<BizRuleCertMetadata> BizRuleCerts;

        public DALBizRuleCertMetadata(CloudStorageAccount account)
        {
            this.account = account;

            BizRuleCertTableName = RoleEnvironment.GetConfigurationSettingValue("Tenant.BizRuleCertTableName");

            BizRuleCerts = bizRuleCertsCache.GetObject(CacheName) as List<BizRuleCertMetadata>;

            if (BizRuleCerts == null)
            {
                BizRuleCerts = GetAllBizRuleCert();
                bizRuleCertsCache.AddObject(CacheName, BizRuleCerts);
            }
        }

        public bool SaveBizRuleCertMetadata(BizRuleCertMetadata bizRuleCertMetadata)
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(BizRuleCertTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            bizRuleCertMetadata.PartitionKey = bizRuleCertMetadata.TradingPartnerName;
            bizRuleCertMetadata.RowKey = bizRuleCertMetadata.RuleCertFileName;
            bizRuleCertMetadata.Timestamp = DateTime.UtcNow;

            if(BizRuleCerts.FirstOrDefault(t => t.PartitionKey == bizRuleCertMetadata.PartitionKey && t.RowKey == bizRuleCertMetadata.RowKey) == null)
                BizRuleCerts.Add(bizRuleCertMetadata);

            // We need upsert functionality here, hence removing AddObject call and adding UpdateObject
            // this.tableContext.AddObject(TradingPartnerSpecCertTableName, tradingPartnerSpecCert);
            // http://social.msdn.microsoft.com/Forums/windowsazure/en-US/892340f1-bfe1-4433-9246-b617abe6078c/upsert-operation-in-the-table
            // http://msdn.microsoft.com/en-us/library/windowsazure/hh452242.aspx
            // http://www.windowsazure.com/en-us/develop/net/how-to-guides/table-services/#replace-entity
            tableContext.AttachTo(BizRuleCertTableName, bizRuleCertMetadata);
            tableContext.UpdateObject(bizRuleCertMetadata);

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);

            return response.BatchStatusCode == Http200 || response.BatchStatusCode == Http201 || response.BatchStatusCode == Http202;
        }

        public bool DeleteBizRuleCertMetadata(BizRuleCertMetadata bizRuleCertMetadata)
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(BizRuleCertTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            bizRuleCertMetadata.PartitionKey = bizRuleCertMetadata.TradingPartnerName;
            bizRuleCertMetadata.RowKey = bizRuleCertMetadata.RuleCertFileName;
            bizRuleCertMetadata.Timestamp = DateTime.UtcNow;

            BizRuleCerts.Remove(bizRuleCertMetadata);

            tableContext.AttachTo(BizRuleCertTableName, bizRuleCertMetadata, "*");
            tableContext.DeleteObject(bizRuleCertMetadata);

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);

            return response.BatchStatusCode == Http200 || response.BatchStatusCode == Http201 || response.BatchStatusCode == Http202;
        }

        public List<string> GetHomeOrgList()
        {
            List<string> tradingPartnerNames = new List<string>();
            BizRuleCerts.ForEach(tp => tradingPartnerNames.Add(tp.TradingPartnerName));

            tradingPartnerNames = tradingPartnerNames.Distinct().ToList();

            tradingPartnerNames.RemoveAll(tp => string.Equals(tp, "global", StringComparison.OrdinalIgnoreCase));

            return tradingPartnerNames;
        }

        public List<BizRuleCertMetadata> GetBizRuleCertFileList()
        {
            return BizRuleCerts;
        }

        public List<BizRuleCertMetadata> GetBizRuleCertFileList(string tradingPartnerName)
        {
            return BizRuleCerts.Where(tp => tp.TradingPartnerName == tradingPartnerName 
                || string.Equals(tp.TradingPartnerName, "global", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private List<BizRuleCertMetadata> GetAllBizRuleCert()
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(BizRuleCertTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            List<BizRuleCertMetadata> bizRuleCerts = 
                            (from bizRuleCertMetadata in
                                 tableContext.CreateQuery<BizRuleCertMetadata>(BizRuleCertTableName)
                             select bizRuleCertMetadata).ToList();

            return bizRuleCerts;
        }
    }
}
