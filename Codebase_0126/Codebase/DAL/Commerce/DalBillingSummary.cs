using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Contracts.Commerce;
using Maarg.AllAboard;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using System.Xml.Serialization;
using System.IO;

namespace Maarg.Dal.Commerce
{
    public class DalBillingCycleSummary : BaseCommerceDal
    {
        public const string BillingCycleSummaryTableName = "BillingCycleSummaryTable";

        public DalBillingCycleSummary(IDalManager manager, CloudStorageAccount account)
            : base(manager, account, BillingCycleSummaryTableName)
        {
        }


        public bool SaveOrUpdateBillingCycleSummary(BillingCycleSummary summary)
        {
            
            int statusCode = 0;
            summary.Id = summary.OrderId;

            BillingCycleSummaryEntity entity = new BillingCycleSummaryEntity(summary);
            entity.PartitionKey = DalBillingCycle.RowKeyGenerator(summary.BillingCycleYear, summary.BillingCycleMonth);
            entity.RowKey = summary.OrderId.ToString();
            

            

            try
            {
                this.tableContext.AddObject(BillingCycleSummaryTableName, entity);
                DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
                statusCode = response.BatchStatusCode;
            }

            catch //assuming this is the case where summary already exists
            {
                //TODO - error case when bill generation failed in the middle
                /*
                BillingCycleSummary existingSummary = GetBillingCycleSummary(summary.OrderId, summary.BillingCycleYear, summary.BillingCycleMonth);
                if (existingSummary != null)
                {
                    //copy over existing properties
                    existingSummary.Total = summary.Total;
                    existingSummary.Paid = summary.Paid;
                    existingSummary.BillingLineItems.AddRange(summary.BillingLineItems);
                    this.tableContext.UpdateObject(existingSummary);
                    DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
                    statusCode = response.BatchStatusCode;
                }
                 */
            }

            return statusCode == Http200;
        }

        public BillingCycleSummary GetBillingCycleSummary(Guid orderId, int year, int month)
        {
            string partitionKey = DalBillingCycle.RowKeyGenerator(year, month);
            string rowKey = orderId.ToString();

            try
            {
                BillingCycleSummaryEntity summary = (from e in this.tableContext.CreateQuery<BillingCycleSummaryEntity>(BillingCycleSummaryTableName)
                                               where e.PartitionKey == partitionKey && e.RowKey == rowKey
                                               select e).FirstOrDefault();
                if (summary != null)
                {
                    BillingCycleSummary ret = BillingCycleSummaryEntity.RetreiveOBillingCycleSummaryFromString(summary.BillingCycleSummaryAsString);
                    return ret;
                }

                else
                {
                    return null;
                }
            }

            catch
            {
                return null; //TODO cleanse the code
            }
        }
    }

    class BillingCycleSummaryEntity : TableServiceEntity
    {
        public string BillingCycleSummaryAsString { get; set; }

        public BillingCycleSummaryEntity()
        {
        }

        public BillingCycleSummaryEntity(BillingCycleSummary summary)
        {
            TextWriter writer = new StringWriter();
            XmlSerializer ser = new XmlSerializer(typeof(BillingCycleSummary));
            ser.Serialize(writer, summary);
            BillingCycleSummaryAsString = writer.ToString();

        }

        public static BillingCycleSummary RetreiveOBillingCycleSummaryFromString(string data)
        {
            BillingCycleSummary summary = null;
            if (!string.IsNullOrEmpty(data))
            {
                XmlSerializer ser = new XmlSerializer(typeof(BillingCycleSummary));
                summary = ser.Deserialize(new StringReader(data)) as BillingCycleSummary;
            }

            return summary;
        }
    }
}
