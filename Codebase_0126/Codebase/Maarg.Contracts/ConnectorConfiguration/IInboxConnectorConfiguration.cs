
namespace Maarg.Contracts
{
    public interface IInboxConnectorConfiguration
    {
        string AzureStorageAccountConnectionString { get; set; }
        string GetContainerName(string partnerIdentifier);
    }
}
