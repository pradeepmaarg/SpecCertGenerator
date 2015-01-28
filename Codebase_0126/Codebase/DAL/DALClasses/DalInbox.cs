using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Maarg.Contracts;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard.DALClasses
{
    public class DalInbox
    {
        CloudStorageAccount cloudAcct;
        CloudBlobClient blobClient;
        CloudBlobContainer inboxContainer;

        public DalInbox(string storageAccountConnectionString, string inboxName)
        {
            // The constructor below is hardcoded for now.. it will use the method params
            cloudAcct = CloudStorageAccount.Parse(storageAccountConnectionString);
            blobClient = cloudAcct.CreateCloudBlobClient();

            // Retrieve a reference to a container 
            inboxContainer = blobClient.GetContainerReference(inboxName);

            // Create the container if it doesn't already exist
            inboxContainer.CreateIfNotExist();

            // does it need public access?
            inboxContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        public IList<IBizMessage> GetBizMessageList()
        {
            IEnumerable<IListBlobItem> items = inboxContainer.ListBlobs();
            IList<IBizMessage> msgList = new List<IBizMessage>();
            foreach (IListBlobItem item in items)
            {
                string blobId = item.Uri.ToString();
                CloudBlob blob = inboxContainer.GetBlobReference(blobId);
                MemoryStream memoryStream = new MemoryStream();
                blob.DownloadToStream(memoryStream);
                memoryStream.Position = 0;
                IBizMessage message = BizMessageFactory.CreateBizMessageFromStream(memoryStream);
                if (message != null)
                {
                    message.StoreId = blobId;   // TBD: this is temporary solution
                    msgList.Add(message);
                }
            }

            return msgList;
        }

        public void SaveBizMessage(IBizMessage message)
        {
            if (!string.IsNullOrEmpty(message.StoreId))
            {
                CloudBlob blob = inboxContainer.GetBlobReference(message.StoreId);
                if (blob != null)
                {
                    Stream stream = BizMessageFactory.WriteBizMessageToStream(message);
                    blob.UploadFromStream(stream);
                }
            }
        }

        public void WriteToInbox(IBizMessage message)
        {
            string identifier = Guid.NewGuid().ToString("N").Substring(0, 15);

            string name = DateTime.UtcNow.Ticks + "-" + identifier + "inboxMessage";
            CloudBlob blob = inboxContainer.GetBlobReference(name);
            using (Stream stream = BizMessageFactory.WriteBizMessageToStream(message))
            {
                blob.UploadFromStream(stream);
            }
        }
    }
}
