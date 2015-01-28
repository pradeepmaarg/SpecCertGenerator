using System;
using System.Collections.Generic;
using System.Text;
using Maarg.Contracts.Commerce;
using Maarg.AllAboard;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;

namespace Maarg.Dal.Commerce
{
    public abstract class BaseCommerceDal
    {
        public const int Http200 = 200;
        protected IDalManager manager;
        protected TableServiceContext tableContext;
        protected CloudTableClient tableClient;

        public BaseCommerceDal(IDalManager manager, CloudStorageAccount account, string tableName)
        {
            this.manager = manager;

            tableClient = account.CreateCloudTableClient();
            tableClient.CreateTableIfNotExist(tableName);
            this.tableContext = tableClient.GetDataServiceContext();
        }
    }
}
