
namespace Maarg.Contracts
{
    public class RoutingInfo
    {
        string partnerId;
        TransportType transportType;

        public RoutingInfo(string partnerId, TransportType transportType)
        {
            this.partnerId = partnerId;
            this.transportType = transportType;
        }

        public RoutingInfo()
        {
        }

        public string PartnerId
        {
            get { return this.partnerId; }
            set { this.partnerId = value; }
        }

        public TransportType TransportType
        {
            get { return this.transportType; }
            set { this.transportType = value; }
        }

    }

    public enum TransportType
    {
        Ftp,
        Http,
        As2,
        Notification,
        Inbox,
        Suspend,
        None
    }
}
