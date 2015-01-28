
namespace Maarg.Contracts
{
    public interface IBlobConnectorConfiguration
    {
        string AzureStorageAccountConnectionString { get; set; }
        string ContainerName { get; set; }
    }
}
