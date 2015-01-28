
namespace Maarg.Contracts
{
    /// <summary>
    /// Holds configurations for connectors (FTP etc.)
    /// </summary>
    public interface IConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets the FTP connector configuration.
        /// </summary>
        IFtpConnectorConfiguration FtpConnectorConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the Maarg Inbox configuration.
        /// </summary>
        IInboxConnectorConfiguration InboxConnectorConfiguration { get; set; }
    
        /// <summary>
        /// Gets or sets the Partner Inbox configuration.
        /// </summary>
        IBlobConnectorConfiguration BlobConnectorConfiguration { get; set; }
    }
}
