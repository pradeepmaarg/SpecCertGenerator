using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    public enum NewPartnerStatus
    {
        Active,
        Disabled
    }

    [Serializable]
    public class NewPartner : UniqueIdentifier
    {
        private string tenantId;
        private ContactInfo contactInfo;
        private EDIIdentity identity; // Or should we introduce abstract Identity here for extensibility?
        private PartnerStatus status;
        private List<Service> subscribedServices;

        public string TenantId
        {
            get { return this.tenantId; }
            set { this.tenantId = value; }
        }
        public ContactInfo ContactInfo
        {
            get { return this.contactInfo; }
            set { this.contactInfo = value; }
        }
        public EDIIdentity Identity
        {
            get { return this.identity; }
            set { this.identity = value; }
        }
        public PartnerStatus Status
        {
            get { return this.status; }
            set { this.status = value; }
        }
        public List<Service> SubscribedServices
        {
            get { return this.subscribedServices; }
            set { this.subscribedServices = value; }
        }

        [NonSerialized]
        private Dictionary<int, ServiceConfiguration> ServiceConfiguration; // ServiceId => ServiceConfiguration
    }
}
