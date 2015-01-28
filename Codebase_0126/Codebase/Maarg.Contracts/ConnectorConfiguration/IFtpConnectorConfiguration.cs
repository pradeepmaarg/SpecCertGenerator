
namespace Maarg.Contracts
{
    public interface IFtpConnectorConfiguration
    {
        string HostName { get; set; }
        string Password { get; set; }
        int PollingIntervalInSec { get; set; }
        string UserName { get; set; }
    }
}
