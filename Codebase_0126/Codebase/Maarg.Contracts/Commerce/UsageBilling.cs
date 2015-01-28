using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.Contracts.Commerce
{
    /// <summary>
    /// This class represent a unit of usage. Bills are generated based on all the usage events within a billing period
    /// </summary>
    public class UsageEvent : UsageAggregatePerUserByBillingCycle
    {
        public DateTime WhenConsumed { get; set; }
        public string Notes1 { get; set; } //free form notes that can be useful to display to user
        public string Notes2 { get; set; } //free form notes that can be useful to display to user

        // Added for GCom Edi Validation usage
        public string HomeOrgName { get; set; }
        public string PartnerName { get; set; }
        public string SpecCertName { get; set; }
        public string InstanceFileName { get; set; }
        public string ValidationStatus { get; set; }
        public int TimeOfValidationInMs { get; set; }

        public string Service { get; set; }
    }

    public class UsageAggregatePerUserByBillingCycle : UsageAggregateByBillingCycle
    {
        public string TenantId { get; set; }
        public string UserId { get; set; }
    }

    public class UsageAggregateByBillingCycle : TableServiceEntity
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid BillingCycleId { get; set; }
        public string ResourceId 
        {
            get { return resourceId; }
            set { resourceId = value.ToLowerInvariant(); } 
        } //resource being consumed, eg. PageView
        public double AmountConsumed { get; set; }

        string resourceId;
    }
}
