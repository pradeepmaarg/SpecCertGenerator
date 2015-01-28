using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class Partner : IPartner
    {
        private string identifier;
        private string name;
        private PartnerStatus status;
        private List<string> protocolIdentifiers;
        private string postalAddress;
        private string phone;
        private string emailAddress;
        private List<string> documents;
        private Uri defaultDocumentEndpoint;
        private ConnectorConfiguration connectorConfiguration;
        private ConnectorConfiguration targetConnectorConfiguration;

        public Partner()
        {
            this.ProtocolIdentifiers = new List<string>();
            this.Documents = new List<string>();
        }

        public string Identifier 
        { 
            get { return this.identifier; } 
            set { this.identifier = value; } 
        }

        public string Name 
        {
            get { return this.name; } 
            set { this.name = value; } 
        }

        public PartnerStatus Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

        public List<string> ProtocolIdentifiers 
        { 
            get { return this.protocolIdentifiers; } 
            set { this.protocolIdentifiers = value; } 
        }

        public string PostalAddress
        {
            get { return this.postalAddress; }
            set { this.postalAddress = value; }
        }

        public string Phone
        {
            get { return this.phone; }
            set { this.phone = value; }
        }

        public string EmailAddress 
        {
            get { return this.emailAddress; } 
            set { this.emailAddress = value; } 
        }

        public List<string> Documents 
        {
            get { return this.documents; } 
            set { this.documents = value; } 
        }

        public Uri DefaultDocumentEndpoint 
        {
            get { return this.defaultDocumentEndpoint; } 
            set { this.defaultDocumentEndpoint = value; } 
        }

        public IConnectorConfiguration ConnectorConfiguration
        {
            get { return this.connectorConfiguration; }
            set { this.connectorConfiguration = value as ConnectorConfiguration; }
        }

        public IConnectorConfiguration TargetConnectorConfiguration
        {
            get { return this.targetConnectorConfiguration; }
            set { this.targetConnectorConfiguration = value as ConnectorConfiguration; }
        }

        /// <summary>
        /// Total license users
        /// </summary>
        public int TotalLicenseUsers { get; set; }

        /// <summary>
        /// Total users (already used the license)
        /// </summary>
        public int TotalUsers { get; set; }
    }
}
