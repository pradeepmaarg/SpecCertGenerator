using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class PartnerAccount : IPartnerAccount
    {
        private string identifier;
        private string email;
        private string partnerIdentifier;

        /// <summary>
        /// Gets or sets the partner account identifier.
        /// The identifier is token received by user.
        /// </summary>
        public string Identifier
        {
            get { return this.identifier; }
            set { this.identifier = value; ; }
        }

        public string Email
        {
            get { return this.email; }
            set { this.email = value; }
        }

        public string PartnerIdentifier
        {
            get { return this.partnerIdentifier; }
            set { this.partnerIdentifier = value; }
        }

        public string Role
        {
            get;
            set;
        }

        public bool IsFirstTimeLogin
        {
            get;
            set;
        }
    }
}
