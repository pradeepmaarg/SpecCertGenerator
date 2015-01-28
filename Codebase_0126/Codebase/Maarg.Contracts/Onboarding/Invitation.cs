using System;

namespace Maarg.Contracts
{
    [Serializable]
    public class Invitation
    {
        private Guid id; // Should this be string instead?
        private int tenantId;
        private string sendTo; // EMail id
        private DateTime sendDate;
        private DateTime expirationDate;
        private DateTime acceptedDate;

        public Guid Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

        public int TenantId
        {
            get { return this.tenantId; }
            set { this.tenantId = value; }
        }
        
        public string SendTo
        {
            get { return this.sendTo; }
            set { this.sendTo = value; }
        }
        
        public DateTime SendDate
        {
            get { return this.sendDate; }
            set { this.sendDate = value; }
        }
        
        public DateTime ExpirationDate
        {
            get { return this.expirationDate; }
            set { this.expirationDate = value; }
        }

        public DateTime AcceptedDate
        {
            get { return this.acceptedDate; }
            set { this.acceptedDate = value; }
        }
    }
}
