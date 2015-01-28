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
    public class DalTimeoffset : BaseCommerceDal
    {
        public const string TimeOffsetTableName = "TimeOffsetTable";
        public const string PartitionKey = "ConstantPartitionKey";
        public const string RowKey = "ConstantRowKey";

        public DalTimeoffset(IDalManager manager, CloudStorageAccount account)
            : base(manager, account, TimeOffsetTableName)
        {
        }

        public bool SaveTimeoffset(TimeOffset timeOffset)
        {
            TimeOffset existing = GetTimeOffset();
            
            if (existing == null)
            {
                timeOffset.PartitionKey = PartitionKey;
                timeOffset.RowKey = RowKey;
                timeOffset.Timestamp = DateTime.UtcNow;
                this.tableContext.AddObject(TimeOffsetTableName, timeOffset);
            }

            else
            {
                existing.Days = timeOffset.Days;
                existing.Hours = timeOffset.Hours;
                existing.Minutes = timeOffset.Minutes;
                this.tableContext.UpdateObject(existing);
            }

            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
            return response.BatchStatusCode == Http200;
        }

        public TimeOffset GetTimeOffset()
        {
            TimeOffset timeOffset = null;
            try
            {
                timeOffset = (from e in this.tableContext.CreateQuery<TimeOffset>(TimeOffsetTableName)
                              where e.PartitionKey == PartitionKey && e.RowKey == RowKey
                                         select e).FirstOrDefault();
            }

            catch
            {
            }

            return timeOffset;
        }
    }
}
