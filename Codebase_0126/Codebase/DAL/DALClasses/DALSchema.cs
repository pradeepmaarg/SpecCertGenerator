using System;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Globalization;

namespace Maarg.AllAboard.DALClasses
{
    public class DALSchema: DalEntityBase<DocumentPlug>
    {
        private string partnerIdentifier;

        public DALSchema(CloudStorageAccount storageAccount, CloudBlobContainer container, string partnerIdentifier)
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
            get { return string.Format(CultureInfo.InvariantCulture, "partnerAccount/{0}", this.partnerIdentifier); }
        }
    }
}
