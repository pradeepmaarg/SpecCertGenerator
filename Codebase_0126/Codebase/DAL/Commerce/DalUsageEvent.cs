using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Maarg.Contracts.Commerce;
using Maarg.AllAboard;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Data.Services.Client;

namespace Maarg.Dal.Commerce
{
    public class DalUsageEvent : BaseCommerceDal
    {
        public static string RawUsageTableName = RoleEnvironment.GetConfigurationSettingValue("Tenant.RawUsageTableName");
        public static string AggregateUsageTableName = RoleEnvironment.GetConfigurationSettingValue("Tenant.AggregateUsageTableName");
        public static string AggregatePerUserUsageTableName = RoleEnvironment.GetConfigurationSettingValue("Tenant.AggregatePerUserUsageTableName");

        private static object lockObject = new object();
        TableServiceContext aggregateTableServiceContext;
        TableServiceContext aggregatePerUserTableServiceContext;

        public DalUsageEvent(IDalManager manager, CloudStorageAccount account)
            : base(manager, account, RawUsageTableName)
        {

            this.tableClient.CreateTableIfNotExist(AggregateUsageTableName);
            this.aggregateTableServiceContext = tableClient.GetDataServiceContext();

            this.tableClient.CreateTableIfNotExist(AggregatePerUserUsageTableName);
            this.aggregatePerUserTableServiceContext = tableClient.GetDataServiceContext();
        }


        public bool TrackUsage(UsageEvent usage)
        {
            int statusCode = -1;
            BillingCycle currentBillingCyle = this.manager.FetchOrCreateCurrentBillingCycle();
            usage.BillingCycleId = currentBillingCyle.Id;

            //PartitionKey = orderId + billingCycleId
            //RowKey = NewGuid
            usage.PartitionKey = string.Format("{0}-{1}", usage.OrderId.ToString(), usage.BillingCycleId.ToString());
            usage.RowKey = Guid.NewGuid().ToString();

            //locking to reduce contention for aggregate update if multiple events for same order are coming at the same time
            //this is not foolproff though. TODO - when updates happen from different servers. check what error Azure
            //returns when server1 read, server2 updated, server1 updated
            //based on that tweak the business logic
            lock (lockObject)
            {
                //step1 - fetchORcreate and increment aggregate usage
                UsageAggregateByBillingCycle aggregate = GetUsageAggregateByBillingCycle(usage.OrderId, usage.BillingCycleId, usage.ResourceId);
                bool isNew = false;
                if (aggregate == null)
                {
                    aggregate = new UsageAggregateByBillingCycle { OrderId = usage.OrderId, BillingCycleId = usage.BillingCycleId, ResourceId = usage.ResourceId };
                    aggregate.PartitionKey = string.Format("{0}-{1}", usage.OrderId.ToString(), usage.BillingCycleId.ToString());
                    aggregate.RowKey = usage.ResourceId;
                    isNew = true;
                }

                aggregate.AmountConsumed += usage.AmountConsumed;

                //step2 - save raw usage
                this.tableContext.AddObject(RawUsageTableName, usage);

                if (isNew)
                {
                    this.aggregateTableServiceContext.AddObject(AggregateUsageTableName, aggregate);
                }

                else
                {
                    this.aggregateTableServiceContext.UpdateObject(aggregate);
                }


                //Now add logic for per user aggregate
                UsageAggregatePerUserByBillingCycle aggregatePerUser = GetUsageAggregatePerUserByBillingCycle(usage.OrderId, usage.BillingCycleId, usage.ResourceId, usage.UserId);
                isNew = false;
                if (aggregatePerUser == null)
                {
                    aggregatePerUser = new UsageAggregatePerUserByBillingCycle { OrderId = usage.OrderId, BillingCycleId = usage.BillingCycleId, ResourceId = usage.ResourceId,
                      UserId = usage.UserId };
                    aggregatePerUser.PartitionKey = string.Format("{0}-{1}", usage.OrderId.ToString(), usage.BillingCycleId.ToString());
                    aggregatePerUser.RowKey = string.Format("{0}-{1}", usage.ResourceId, usage.UserId);
                    isNew = true;
                }

                aggregatePerUser.AmountConsumed += usage.AmountConsumed;

                
                if (isNew)
                {
                    this.aggregatePerUserTableServiceContext.AddObject(AggregatePerUserUsageTableName, aggregatePerUser);
                }

                else
                {
                    this.aggregatePerUserTableServiceContext.UpdateObject(aggregatePerUser);
                }

                //end

                DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
                response = this.aggregateTableServiceContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
                statusCode = response.BatchStatusCode;
            }

            return statusCode == Http200 || statusCode == 202;
        }

        public UsageAggregateByBillingCycle GetUsageAggregateByBillingCycle(Guid orderId, Guid billingCycleId, string resourceId)
        {
            UsageAggregateByBillingCycle aggregate = null;

            string partitionKey = string.Format("{0}-{1}", orderId.ToString(), billingCycleId.ToString());
            string rowKey = resourceId;

            try
            {
                aggregate = (from e in this.aggregateTableServiceContext.CreateQuery<UsageAggregateByBillingCycle>(AggregateUsageTableName)
                             where e.PartitionKey == partitionKey && e.RowKey == rowKey
                             select e).FirstOrDefault();
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            return aggregate;
        }

        //Caps number of results to 10. TODO for future to enhance
        //10 should suffice for now, since number of records = number of resource types which is a small number
        public IList<UsageAggregateByBillingCycle> GetUsageAggregateByBillingCycle(Guid orderId, Guid billingCycleId)
        {
            string partitionKey = string.Format("{0}-{1}", orderId.ToString(), billingCycleId.ToString());
            IEnumerator aggregate = null;

            try
            {
                aggregate = (from e in this.aggregateTableServiceContext.CreateQuery<UsageAggregateByBillingCycle>(AggregateUsageTableName)
                             where e.PartitionKey == partitionKey
                             select e).Take(10).GetEnumerator();
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            IList<UsageAggregateByBillingCycle> list = new List<UsageAggregateByBillingCycle>();
            if (aggregate != null)
            {
                while (aggregate.MoveNext())
                {
                    list.Add((UsageAggregateByBillingCycle)aggregate.Current);
                }
            }

            return list;
        }

        public IList<UsageAggregateByBillingCycle> GetUsageAggregateByBillingCycle(Guid orderId, int cycleNumber)
        {
            IList<UsageAggregateByBillingCycle> list = null;
            BillingCycle cycle = this.manager.GetBillingCycle(cycleNumber);
            if (cycle != null)
            {
                list = GetUsageAggregateByBillingCycle(orderId, cycle.Id);
            }

            return list;
        }
         

        public IList<UsageEvent> GetRawUsage(Guid orderId, int cycleNumber)
        {
            BillingCycle cycle = this.manager.GetBillingCycle(cycleNumber);

            IEnumerator enumerator = null;
            if (cycle != null)
            {
                string partitionKey = string.Format("{0}-{1}", orderId.ToString(), cycle.Id.ToString());

                try
                {
                    enumerator = (from e in this.tableContext.CreateQuery<UsageAggregateByBillingCycle>(AggregateUsageTableName)
                                  where e.PartitionKey == partitionKey
                                  select e).Take(1000).GetEnumerator();
                }

                catch
                {
                    //do nothing, this covers the case when the entity does not exist
                    //TODO handle other cases
                }
            }

            IList<UsageEvent> list = new List<UsageEvent>();
            if (enumerator != null)
            {
                while (enumerator.MoveNext())
                {
                    list.Add((UsageEvent)enumerator.Current);
                }
            }

            return list;
        }

        public IList<UsageEvent> GetRawUsage(DateTime dateFrom, DateTime dateTo, string homeOrg)
        {
            IEnumerator enumerator = null;
            try
            {
                // unfortunately we can't use orderby, it'll throw exception "The requested operation is not implemented on the specified resource"
                if (string.IsNullOrEmpty(homeOrg))
                {
                    enumerator = (from e in this.tableContext.CreateQuery<UsageEvent>(RawUsageTableName)
                                  where e.Timestamp >= dateFrom && e.Timestamp <= dateTo
                                  select e).Take(1000).GetEnumerator();
                }
                else
                {
                    homeOrg = homeOrg.Trim();
                    enumerator = (from e in this.tableContext.CreateQuery<UsageEvent>(RawUsageTableName)
                                  where e.Timestamp >= dateFrom && e.Timestamp <= dateTo && string.Compare(e.HomeOrgName, homeOrg, true) == 0
                                  select e).Take(1000).GetEnumerator();
                }
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            IList<UsageEvent> list = new List<UsageEvent>();
            if (enumerator != null)
            {
                while (enumerator.MoveNext())
                {
                    list.Add((UsageEvent)enumerator.Current);
                }
            }

            // instead we order by the timestamp (desc) using this approach
            return list.OrderByDescending(item => item.Timestamp).ToList<UsageEvent>();
        }


        public UsageAggregatePerUserByBillingCycle GetUsageAggregatePerUserByBillingCycle(Guid orderId, Guid billingCycleId, string resourceId, string userId)
        {
            UsageAggregatePerUserByBillingCycle aggregate = null;

            string partitionKey = string.Format("{0}-{1}", orderId.ToString(), billingCycleId.ToString());
            string rowKey = string.Format("{0}-{1}", resourceId, userId);

            try
            {
                aggregate = (from e in this.aggregateTableServiceContext.CreateQuery<UsageAggregatePerUserByBillingCycle>(AggregatePerUserUsageTableName)
                             where e.PartitionKey == partitionKey && e.RowKey == rowKey
                             select e).FirstOrDefault();
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            return aggregate;
        }

        //Caps number of results to 10. TODO for future to enhance
        //10 should suffice for now, since number of records = number of resource types which is a small number
        public IList<UsageAggregatePerUserByBillingCycle> GetUsageAggregatePerUserByBillingCycle(Guid orderId, Guid billingCycleId)
        {
            string partitionKey = string.Format("{0}-{1}", orderId.ToString(), billingCycleId.ToString());
            IEnumerator aggregate = null;

            try
            {
                aggregate = (from e in this.aggregateTableServiceContext.CreateQuery<UsageAggregatePerUserByBillingCycle>(AggregatePerUserUsageTableName)
                             where e.PartitionKey == partitionKey
                             select e).Take(100).GetEnumerator();
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            IList<UsageAggregatePerUserByBillingCycle> list = new List<UsageAggregatePerUserByBillingCycle>();
            if (aggregate != null)
            {
                while (aggregate.MoveNext())
                {
                    list.Add((UsageAggregatePerUserByBillingCycle)aggregate.Current);
                }
            }

            return list;
        }

        public IList<UsageAggregatePerUserByBillingCycle> GetUsageAggregatePerUserByBillingCycle(Guid orderId, int cycleNumber)
        {
            IList<UsageAggregatePerUserByBillingCycle> list = null;
            BillingCycle cycle = this.manager.GetBillingCycle(cycleNumber);
            if (cycle != null)
            {
                list = GetUsageAggregatePerUserByBillingCycle(orderId, cycle.Id);
            }

            return list;
        }
    }
}
