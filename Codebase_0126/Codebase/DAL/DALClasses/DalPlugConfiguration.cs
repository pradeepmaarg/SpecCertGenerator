using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Maarg.Contracts;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard.DALClasses
{
    class DalPlugConfiguration : DalEntityBase<PlugConfiguration>
    {
        private string partnerIdentifier;

        public DalPlugConfiguration(CloudStorageAccount storageAccount, CloudBlobContainer container, string partnerIdentifier)
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
            get { return string.Format(CultureInfo.InvariantCulture, "plugconfiguration/{0}", this.partnerIdentifier); }
        }
    }
}
