using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    [Serializable]
    public class NewTenant : UniqueIdentifier
    {
        private ContactInfo contactInfo;
        private List<Service> subscribedServices;
        private List<string> partnerIdentifiers;

        public ContactInfo ContactInfo 
        {
            get { return this.contactInfo; }
            set { this.contactInfo = value; } 
        }
        public List<Service> SubscribedServices 
        {
            get { return this.subscribedServices; }
            set { this.subscribedServices = value; } 
        }
        public List<string> PartnerIdentifiers
        {
            get { return this.partnerIdentifiers; }
            set { this.partnerIdentifiers = value; }
        }

        [NonSerialized]
        private List<NewPartner> partners;
        public List<NewPartner> Partners
        {
            get { return this.partners; }
            set { this.partners = value; }
        }
    }
}
