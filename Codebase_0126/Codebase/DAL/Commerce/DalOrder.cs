using System;
using System.Collections;
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
    public class DalOrder : BaseCommerceDal
    {
        public const string OrderTableName = "OrderTable";
        
        public DalOrder(IDalManager manager, CloudStorageAccount account) 
            : base(manager, account, OrderTableName)
        {
        }

        public bool SaveOrder(Order order)
        {
            order.PartitionKey = order.Id.ToString();
            order.RowKey = order.Id.ToString();
            order.CreationTime = this.manager.GetCurrentTime();
            order.UpdateTime = this.manager.GetCurrentTime();
            order.Timestamp = this.manager.GetCurrentTime();

            this.tableContext.AddObject(OrderTableName, order);
            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
            return response.BatchStatusCode == Http200;
        }

        public Order GetOrder(Guid id)
        {
            Order order = (from e in this.tableContext.CreateQuery<Order>(OrderTableName)
                           where e.PartitionKey == id.ToString()
                           select e).FirstOrDefault();
            return order;
        }

        public IList<Order> GetOrderByTenantId(string tenantId)
        {
            IEnumerator enumerator = null;

            try
            {
                enumerator = (from e in this.tableContext.CreateQuery<Order>(OrderTableName)
                              where e.TenantId == tenantId
                                select e).Take(100).GetEnumerator();
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            IList<Order> list = new List<Order>();
            if (enumerator != null)
            {
                while (enumerator.MoveNext())
                {
                    list.Add((Order)enumerator.Current);
                }
            }

            return list;
        }


        public IList<Order> GetAllOrders()
        {
            IEnumerator enumerator = null;

            try
            {
                enumerator = (from e in this.tableContext.CreateQuery<Order>(OrderTableName)
                              select e).Take(1000).GetEnumerator();
            }

            catch
            {
                //do nothing, this covers the case when the entity does not exist
                //TODO handle other cases
            }

            IList<Order> list = new List<Order>();
            if (enumerator != null)
            {
                while (enumerator.MoveNext())
                {
                    list.Add((Order)enumerator.Current);
                }
            }

            return list;
        }
    }
}
