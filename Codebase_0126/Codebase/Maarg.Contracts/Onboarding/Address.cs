using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    [Serializable]
    public class Address
    {
        private string line1;
        private string line2;
        private string city;
        private string state;
        private string country;
        private string zipCode;

        public string Line1 
        {
            get { return this.line1; }
            set { this.line1 = value; }
        }

        public string Line2
        {
            get { return this.line2; }
            set { this.line2 = value; }
        }

        public string City
        {
            get { return this.city; }
            set { this.city = value; }
        }

        public string State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        public string Country
        {
            get { return this.country; }
            set { this.country = value; }
        }

        public string ZipCode
        {
            get { return this.zipCode; }
            set { this.zipCode = value; }
        }
    }
}
