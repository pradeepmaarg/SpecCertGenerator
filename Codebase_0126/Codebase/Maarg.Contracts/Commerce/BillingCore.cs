using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.Contracts.Commerce
{
    /// <summary>
    /// This represent the summary of a bill including time period, amount due, when due etc
    /// It provides access into additional details of how the bill came up to a given amount
    /// BillingLineItem provides the additional level of detail
    /// </summary>
    public class BillingCycleSummary : TableServiceEntity
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public int BillingCycleMonth { get; set; }
        public int BillingCycleYear { get; set; }
        public double Total { get; set; }
		public double Paid { get; set; }
        public DateTime WhenDue { get; set; }
		public DateTime WhenPaid { get; set; }

        public DateTime CreationTime { get; set; }
        public List<BillingLineItem> BillingLineItems 
        {
            get { return billingLineItems; }
        }

        List<BillingLineItem> billingLineItems = new List<BillingLineItem>();
    }

    public class BillingLineItem
    {
        public double Amount { get; set; }
        public string Description { get; set; }
    }

    public enum Currency
    {
        USD
    }

    public class TimeOffset : TableServiceEntity
    {
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
    }

    /// <summary>
    /// Represents a given billing cycle. For simplication, we will align all cycles to being and end
    /// with month boundaries. In future, support will be added for different cycles like 20th-19th of next month
    /// </summary>
    public class BillingCycle : TableServiceEntity
    {
        public Guid Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Status { get; set; }
		
		public const int Open = 1;
		public const int Closing = 2;
		public const int Closed = 3;

        public BillingCycle()
        {
            Status = Open;
        }
    }

    public enum BillingModel
    {
        Trial,
        Paid
    }

    public enum BillingFrequency
    {
        OneTime,
        Monthly,
        Quarterly,
        Yearly
    }
}
