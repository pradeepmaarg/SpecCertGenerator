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
    public class DALBtsAssemblyFilesMetadata
    {
        private string MapFilesTableName;

        public const int Http200 = 200;
        public const int Http201 = 201;
        public const int Http202 = 202;
        private TableServiceContext tableContext;
        private CloudTableClient tableClient;
        private CloudStorageAccount account;

        private static string CacheName = "BtsAssemblyFilesCache";

        private static Cache mapFilesCache = CacheManager.GetCache(CacheName, 15 * 60, 1000);

        private List<BtsAssemblyFilesMetadata> MapFiles;
        
        public DALBtsAssemblyFilesMetadata(CloudStorageAccount account)
        {
            this.account = account;

            MapFilesTableName = RoleEnvironment.GetConfigurationSettingValue("Tenant.BtsAssemblyFilesTableName");

            MapFiles = mapFilesCache.GetObject(CacheName) as List<BtsAssemblyFilesMetadata>;

            if (MapFiles == null)
            {
                MapFiles = GetAllBtsAssemblyFiles();
                mapFilesCache.AddObject(CacheName, MapFiles);
            }
        }

        public List<BtsAssemblyFilesMetadata> GetBtsAssemblyFilesList()
        {
            return MapFiles;
        }

        public List<BtsAssemblyFilesMetadata> GetBtsAssemblyFilesList(string fileName)
        {
            return MapFiles.Where(tp => tp.FileName == fileName).ToList();
        }

        public bool SaveBtsAssemblyFilesMetadata(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(MapFilesTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            btsAssemblyFilesMetadata.PartitionKey = btsAssemblyFilesMetadata.FileName;
            btsAssemblyFilesMetadata.RowKey = btsAssemblyFilesMetadata.FileName;
            btsAssemblyFilesMetadata.Timestamp = DateTime.UtcNow;

            if(MapFiles.FirstOrDefault(t => t.PartitionKey == btsAssemblyFilesMetadata.PartitionKey && t.RowKey == btsAssemblyFilesMetadata.RowKey) == null)
                MapFiles.Add(btsAssemblyFilesMetadata);

            // We need upsert functionality here, hence removing AddObject call and adding UpdateObject
            // this.tableContext.AddObject(MapFilesTableName, tradingPartnerSpecCert);
            // http://social.msdn.microsoft.com/Forums/windowsazure/en-US/892340f1-bfe1-4433-9246-b617abe6078c/upsert-operation-in-the-table
            // http://msdn.microsoft.com/en-us/library/windowsazure/hh452242.aspx
            // http://www.windowsazure.com/en-us/develop/net/how-to-guides/table-services/#replace-entity
            tableContext.AttachTo(MapFilesTableName, btsAssemblyFilesMetadata);
            tableContext.UpdateObject(btsAssemblyFilesMetadata);

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);

            return response.BatchStatusCode == Http200 || response.BatchStatusCode == Http201 || response.BatchStatusCode == Http202;
        }

        public bool DeleteBtsAssemblyFilesMetadata(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(MapFilesTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            btsAssemblyFilesMetadata.PartitionKey = btsAssemblyFilesMetadata.FileName;
            btsAssemblyFilesMetadata.RowKey = btsAssemblyFilesMetadata.FileName;
            btsAssemblyFilesMetadata.Timestamp = DateTime.UtcNow;

            MapFiles.Remove(btsAssemblyFilesMetadata);

            tableContext.AttachTo(MapFilesTableName, btsAssemblyFilesMetadata, "*");
            tableContext.DeleteObject(btsAssemblyFilesMetadata);

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);

            return response.BatchStatusCode == Http200 || response.BatchStatusCode == Http201 || response.BatchStatusCode == Http202;
        }

        private List<BtsAssemblyFilesMetadata> GetAllBtsAssemblyFiles()
        {
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.CreateTableIfNotExist(MapFilesTableName);
            this.tableContext = tableClient.GetDataServiceContext();

            List<BtsAssemblyFilesMetadata> btsAssemblyFiles = 
                            (from mapFilesMetadata in
                                 tableContext.CreateQuery<BtsAssemblyFilesMetadata>(MapFilesTableName)
                                 select mapFilesMetadata).ToList();

            return btsAssemblyFiles;
        }
    }
}
