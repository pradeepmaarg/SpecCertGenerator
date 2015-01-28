using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class Tenant : ITenant
    {
        private string identifier;
        private string tenantPartnerIdentifier;
        private List<string> tradingPartnersIdentifiers;

        [NonSerialized]
        private Partner tenantPartner;
        [NonSerialized]
        private Collection<IPartner> tradingPartners;

        public Tenant()
        {
            this.tradingPartnersIdentifiers = new List<string>();
            this.tradingPartners = new Collection<IPartner>();
        }

        public string Identifier
        {
            get { return this.identifier; }
            set { this.identifier = value; }
        }

        internal string TenantPartnerIdentifier
        {
            get { return this.tenantPartnerIdentifier; }
            set { this.tenantPartnerIdentifier = value; }
        }

        public IPartner TenantPartner
        {
            get { return this.tenantPartner; }
            set { this.tenantPartner = value as Partner; }
        }

        internal List<string> TradingPartnersIdentifiers
        {
            get { return this.tradingPartnersIdentifiers; }
        }

        public Collection<IPartner> TradingPartners
        {
            get { return this.tradingPartners; }
        }

        public List<IFtpConnectorConfiguration> ListTenantFtpConnectorConfigurations()
        {
            List<IFtpConnectorConfiguration> result = new List<IFtpConnectorConfiguration>();

            if (this.TenantPartner != null
                && this.TenantPartner.ConnectorConfiguration != null
                && this.TenantPartner.ConnectorConfiguration.FtpConnectorConfiguration != null)
            {
                result.Add(this.TenantPartner.ConnectorConfiguration.FtpConnectorConfiguration);
            }

            if (this.TradingPartners != null)
            {
                foreach (IPartner partner in this.TradingPartners)
                {
                    if (partner.ConnectorConfiguration != null
                        && partner.ConnectorConfiguration.FtpConnectorConfiguration != null)
                    {
                        result.Add(partner.ConnectorConfiguration.FtpConnectorConfiguration);
                    }
                }
            }

            return result;
        }

        public List<IInboxConnectorConfiguration> ListTenantInboxConnectorConfigurations()
        {
            List<IInboxConnectorConfiguration> result = new List<IInboxConnectorConfiguration>();

            if (this.TenantPartner != null
                && this.TenantPartner.ConnectorConfiguration != null
                && this.TenantPartner.ConnectorConfiguration.InboxConnectorConfiguration != null)
            {
                result.Add(this.TenantPartner.ConnectorConfiguration.InboxConnectorConfiguration);
            }

            if (this.TradingPartners != null)
            {
                foreach (IPartner partner in this.TradingPartners)
                {
                    if (partner.ConnectorConfiguration != null
                        && partner.ConnectorConfiguration.InboxConnectorConfiguration != null)
                    {
                        result.Add(partner.ConnectorConfiguration.InboxConnectorConfiguration);
                    }
                }
            }

            return result;
        }

        [OnSerializing()]
        internal void OnSerializingMethod(StreamingContext context)
        {
            this.TenantPartnerIdentifier = null;
            if (this.TenantPartner != null)
            {
                this.TenantPartnerIdentifier = this.TenantPartner.Identifier;
            }

            this.TradingPartnersIdentifiers.Clear();
            if (this.TradingPartners.Any())
            {
                this.TradingPartnersIdentifiers.AddRange(from partner in this.TradingPartners select partner.Identifier);
            }
        }

        [OnDeserializing()]
        internal void OnDeserializingMethod(StreamingContext context)
        {
            this.tradingPartners = new Collection<IPartner>();
        }
    }
}
