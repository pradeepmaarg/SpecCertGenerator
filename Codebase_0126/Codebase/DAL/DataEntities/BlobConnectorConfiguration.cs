using System;
using Maarg.Contracts;
using System.Globalization;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class BlobConnectorConfiguration : IBlobConnectorConfiguration
    {
        private string azureStorageAccountConnectionString;
        private string containerName;

        public string AzureStorageAccountConnectionString
        {
            get { return this.azureStorageAccountConnectionString; }
            set { this.azureStorageAccountConnectionString = value; }
        }

        public string ContainerName
        {
            get { return this.containerName; }
            set { this.containerName = value; }
        }
    }
}
