using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public enum TransportProtocolType
    {
        FTP,
    }

    [Serializable]
    public abstract class TransportProtocol
    {
        private TransportProtocolType protocolType;

        public TransportProtocolType ProtocolType
        {
            get { return this.protocolType; }
            set { this.protocolType = value; }
        }
    }
}
