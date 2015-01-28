using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    [Serializable]
    public class ClaimAckServiceConfiguration : ServiceConfiguration
    {
        private string ediId;
        private List<string> emailIds;

        public string EDIId 
        {
            get { return this.ediId; }
            set { this.ediId = value; }
        }

        public List<string> EmailIds 
        {
            get { return this.emailIds; }
            set { this.emailIds = value; }
        }
    }
}
