using System;
using Maarg.Contracts;
using System.Globalization;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class InboxConnectorConfiguration : IInboxConnectorConfiguration
    {
        private string azureStorageAccountConnectionString;

        public string GetContainerName(string partnerIdentifier)
        {
            return string.Format(CultureInfo.InvariantCulture, "inbox-{0}", partnerIdentifier.ToLowerInvariant());
        }

        public string AzureStorageAccountConnectionString
        {
            get { return this.azureStorageAccountConnectionString; }
            set { this.azureStorageAccountConnectionString = value; }
        }
    }
}
