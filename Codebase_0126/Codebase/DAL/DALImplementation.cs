using Maarg.Contracts;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard
{
    /// <summary>
    /// The layer that will be used by FPM layer to store messages in Azure queue and Azure Blobs.
    /// There may be a set of queues, several blobs and within each blob more containers.
    /// </summary>
    class FatPipeDAL
    {
        public CloudQueueClient queueClient;
        public CloudBlobClient blobClient;
        public CloudStorageAccount cloudAcct;

        public CloudQueue incomingQ;
        public CloudBlobContainer container;

        public FatPipeDAL(string storage, string key)
        {
            // The constructor below is hardcoded for now.. it will use the method params
            cloudAcct = new CloudStorageAccount(new StorageCredentialsAccountAndKey("maargsoft", "njPcqdHZuYUNbp32GS1tpSAeoUSp1hZ1EJsqRdtnTJe5BZtEmVd61UHonvva6o3WZ1COeAPtTm4ofbMqFaqj7Q=="), false);
            queueClient = cloudAcct.CreateCloudQueueClient();
            blobClient = cloudAcct.CreateCloudBlobClient();

            // Retrieve a reference to a queue. Assume there is only 1 queue and 1 container within it for now.
            incomingQ = queueClient.GetQueueReference("incomingQ");

            // Create the queue if it doesn't already exist
            incomingQ.CreateIfNotExist();

            // Retrieve a reference to a container 
            container = blobClient.GetContainerReference("incomingBlobsContainer");

            // Create the container if it doesn't already exist
            container.CreateIfNotExist();

            // does it need public access?
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        public bool Enqueue(IFatpipeMessage msg)
        {
            // store the entire message in Blob
            string blobName = msg.Header.Identifier;

            CloudBlob blob = container.GetBlobReference(blobName);

            // Only the body is being uploaded for now..change it to upload the entire msg
            blob.UploadFromStream(msg.Body.Body);


            CloudQueueMessage entry = new CloudQueueMessage(blobName);

            incomingQ.AddMessage(entry);

            return true;

        }

        public bool Dequeue(ref IFatpipeMessage msg)
        {
            CloudQueueMessage entry = incomingQ.GetMessage();

            string blobName = entry.AsString;

            CloudBlob blob = container.GetBlobReference(blobName);

            //blob.DownloadToStream(

            return true;
        }

    }
}
