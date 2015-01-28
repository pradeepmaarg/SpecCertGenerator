using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    [Serializable]
    public abstract class FtpTransportProtocol : TransportProtocol
    {
        private string ftpLocation;
        private string userName;
        private string password; // encrypted?
        private int pollingIntervalInMinutes;

        public string FtpLocation
        {
            get { return this.ftpLocation; }
            set { this.ftpLocation = value; }
        }

        public string UserName
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

        public string Password
        {
            get { return this.password; }
            set { this.password = value; }
        }

        public int PollingIntervalInMinutes
        {
            get { return this.pollingIntervalInMinutes; }
            set { this.pollingIntervalInMinutes = value; }
        }
    }
}
