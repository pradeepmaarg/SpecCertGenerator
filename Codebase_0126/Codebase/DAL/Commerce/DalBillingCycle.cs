using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Contracts.Commerce;
using Maarg.AllAboard;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;

namespace Maarg.Dal.Commerce
{
    public class DalBillingCycle : BaseCommerceDal
    {
        public const string BillingCycleTableName = "BillingCycleTable";
        public const string PartitionKey = "ConstantBillingCyclePartitionKey";

        public DalBillingCycle(IDalManager manager, CloudStorageAccount account) 
            : base(manager, account, BillingCycleTableName)
        {
        }

        public bool SaveBillingCycle(BillingCycle cycle)
        {
            //cycle.PartitionKey = cycle.RowKey = Guid.NewGuid().ToString();
            cycle.PartitionKey = PartitionKey;
            cycle.RowKey = RowKeyGenerator(cycle.StartDate.Year, cycle.StartDate.Month);
            cycle.Timestamp = this.manager.GetCurrentTime();

            this.tableContext.AddObject(BillingCycleTableName, cycle);
            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
            return response.BatchStatusCode == Http200;
        }

        public BillingCycle GetLatestBillingCycle()
        {
            return GetBillingCycle(this.manager.GetCurrentTime().Year, this.manager.GetCurrentTime().Month);
        }

        public bool UpdateBillingCycleStatus(int year, int month, int status)
        {
            BillingCycle cycle = GetBillingCycle(year, month);
            cycle.Status = status;

            this.tableContext.UpdateObject(cycle);
            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
            return response.BatchStatusCode == Http200;
        }


        public BillingCycle GetBillingCycle(int year, int month)
        {
            string rowKey = RowKeyGenerator(year, month);

            try
            {
                BillingCycle cycle = (from e in this.tableContext.CreateQuery<BillingCycle>(BillingCycleTableName)
                                      where e.PartitionKey == PartitionKey && e.RowKey == rowKey
                                      select e).FirstOrDefault();
                return cycle;
            }

            catch
            {
                return null; //TODO cleanse the code
            }
        }

        public static string RowKeyGenerator(int year, int month)
        {
            string rowKey = string.Format("{0}-{1}", year, month);
            return rowKey;
        }
    }
}
