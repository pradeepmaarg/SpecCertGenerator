using System;
using System.Globalization;
using Maarg.AllAboard.DataEntities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard.DALClasses
{
    public class DalPartnerAccount : DalEntityBase<PartnerAccount>
    {
        private string partnerIdentifier;

        public DalPartnerAccount(CloudStorageAccount storageAccount, CloudBlobContainer container, string partnerIdentifier)
            : base(storageAccount, container)
        {
            if (string.IsNullOrWhiteSpace(partnerIdentifier))
            {
                throw new ArgumentNullException("partnerIdentifier");
            }

            this.partnerIdentifier = partnerIdentifier;
        }

        public override string BlobDirectoryName
        {
            get { return string.Format(CultureInfo.InvariantCulture, "partneraccount/{0}", this.partnerIdentifier); }
        }
    }
}
