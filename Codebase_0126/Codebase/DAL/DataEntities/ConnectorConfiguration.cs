using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class ConnectorConfiguration : IConnectorConfiguration
    {
        private FtpConnectorConfiguration ftpConnectorConfiguration;
        private InboxConnectorConfiguration inboxConfiguration;
        private BlobConnectorConfiguration blobConfiguration;

        public IFtpConnectorConfiguration FtpConnectorConfiguration
        {
            get { return this.ftpConnectorConfiguration; }
            set { this.ftpConnectorConfiguration = value as FtpConnectorConfiguration; }
        }

        public IInboxConnectorConfiguration InboxConnectorConfiguration
        {
            get { return this.inboxConfiguration; }
            set { this.inboxConfiguration = value as InboxConnectorConfiguration; }
        }

        public IBlobConnectorConfiguration BlobConnectorConfiguration
        {
            get { return this.blobConfiguration; }
            set { this.blobConfiguration = value as BlobConnectorConfiguration; }
        }
    }
}
