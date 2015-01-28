using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class FtpConnectorConfiguration : IFtpConnectorConfiguration
    {
        private string hostName;
        private int pollingIntervalInSec;
        private string userName;
        private string password;

        public FtpConnectorConfiguration()
        {
        }

        public string HostName
        {
            get { return this.hostName; }
            set { this.hostName = value; }
        }

        public int PollingIntervalInSec
        {
            get { return this.pollingIntervalInSec; }
            set { this.pollingIntervalInSec = value; }
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
    }
}
