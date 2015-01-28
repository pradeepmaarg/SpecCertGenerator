using Maarg.AllAboard.DataEntities;
using Maarg.Contracts;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard.DALClasses
{
    public class DalPartner : DalEntityBase<Partner>
    {
        public DalPartner(CloudStorageAccount storageAccount, CloudBlobContainer container)
            : base(storageAccount, container)
        {
        }

        public override string BlobDirectoryName
        {
            get { return "partner"; }
        }

        protected override void DeleteExtended(Partner entity)
        {
            DalPartnerAccount partnerAccountDal = new DalPartnerAccount(this.storageAccount, this.container, entity.Identifier);

            foreach (IPartnerAccount partnerAccount in partnerAccountDal.List())
            {
                partnerAccountDal.Delete(partnerAccount as PartnerAccount);
            }
        }

        protected override void SaveExtended(Partner entity)
        {
            //Create the inbox container if it doesn't exist
            if (entity != null
                && entity.ConnectorConfiguration != null
                && entity.ConnectorConfiguration.InboxConnectorConfiguration != null)
            {
                IInboxConnectorConfiguration inboxConnectorConfiguration = entity.ConnectorConfiguration.InboxConnectorConfiguration;

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(inboxConnectorConfiguration.AzureStorageAccountConnectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(inboxConnectorConfiguration.GetContainerName(entity.Identifier));
                container.CreateIfNotExist();
            }
        }
    }
}
