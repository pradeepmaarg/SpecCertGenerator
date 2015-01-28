using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    [Serializable]
    public class ContactInfo
    {
        private string name;
        private Address contactAddress;
        private string phoneNumber;
        private string eMail;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public Address ContactAddress
        {
            get { return this.contactAddress; }
            set { this.contactAddress = value; }
        }

        public string PhoneNumber
        {
            get { return this.phoneNumber; }
            set { this.phoneNumber = value; }
        }

        public string EMail
        {
            get { return this.eMail; }
            set { this.eMail = value; }
        }
    }
}
