using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Maarg.AllAboard;
using Maarg.Contracts.GCValidate;
using System.Data.Services.Client;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;
using System.Globalization;

namespace Maarg.Dal.GCValidate
{
    public class DALMapFiles
    {
        /// <summary>
        /// Windows Azure Storage Account
        /// </summary>
        protected CloudStorageAccount storageAccount = null;

        /// <summary>
        /// Windows Azure Storage Blob Container name used to store Xml files 
        /// for all partners
        /// </summary>
        protected CloudBlobContainer container = null;

        /// <summary>
        /// Creates a new instance of the <see cref="DALMapFiles"/> type.
        /// </summary>
        /// <param name="storageAccount">The <see cref="CloudStorageAccount"/>.</param>
        /// <param name="container">The <see cref="CloudBlobContainer"/>.</param>
        public DALMapFiles(CloudStorageAccount storageAccount, CloudBlobContainer container)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.storageAccount = storageAccount;
            this.container = container;
        }

        public void SaveBtsAssemblyFiles(Stream mapFilesStream, BtsAssemblyFilesMetadata mapFilesMetadata)
        {
            CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
            CloudBlob blob = client.GetBlobReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", 
                this.container.Name, mapFilesMetadata.FileName));
            blob.DeleteIfExists();
            blob.UploadFromStream(mapFilesStream);
        }

        public void DeleteBtsAssemblyFiles(BtsAssemblyFilesMetadata mapFilesMetadata)
        {
            CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
            CloudBlob blob = client.GetBlobReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}",
                this.container.Name, mapFilesMetadata.FileName));
            blob.DeleteIfExists();
        }

        public Stream GetBtsAssemblyFiles(BtsAssemblyFilesMetadata mapFilesMetadata)
        {
            CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
            CloudBlob blob = client.GetBlobReference(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", this.container.Name, mapFilesMetadata.FileName));

            MemoryStream ms = new MemoryStream();
            blob.DownloadToStream(ms);
            ms.Position = 0;

            return ms;
        }
    }
}
