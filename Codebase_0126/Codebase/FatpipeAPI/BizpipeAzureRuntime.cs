using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Maarg.AllAboard;
using Maarg.Contracts;
using Maarg.Fatpipe.LoggingService;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.FatpipeAPI
{
   
    
    /// <summary>
    /// The layer that will be used by FPM layer to store messages in Azure queue and Azure Blobs.
    /// There may be a set of queues, several blobs and within each blob more containers.
    /// </summary>
    public class FatPipeDAL
    {
        const char CarriageReturn = (char)13;
        const char NewLine = (char)10;
        const string SchemaContainerName = "schemarepository";
        const string NotificationTemplateContainerName = "notificationtemplaterepository";
        const string BlobContainerPartitionNameFormat = "{0}-{1}-{2}";
        static UTF8Encoding encoding = new UTF8Encoding();

        FatpipeManager fpm;

        //Clients to connect to Azure storage
        CloudQueueClient queueClient;
        CloudBlobClient blobClient;
        CloudTableClient tableClient;
        CloudStorageAccount cloudAcct;

        //Incoming queue and blob storage
        CloudQueue incomingQ;
        CloudBlobContainer inContainer;

        //Outgoing queue and blob storage
        CloudQueue outgoingQ;
        CloudBlobContainer outContainer;

        //Schema container
        CloudBlobContainer schemaContainer;
        CloudBlobContainer notificationTemplateContainer;

        //suspended messages store
        CloudBlobContainer suspendedContainer;
        TableServiceContext suspendedTableServiceContext;

        //Get the queue message visibility timeout
        TimeSpan visibilityTimeout;

        public FatPipeDAL(string storageConnectionString, FatpipeManager fpm)
        {
            try
            {
                LoggerFactory.Logger.Debug("FatPipeDAL C'tor", "Initializing Azure accounts");
                this.fpm = fpm;

                // The constructor below is hardcoded for now.. it will use the method params
                if (!string.IsNullOrEmpty(storageConnectionString))
                {
                    this.cloudAcct = CloudStorageAccount.Parse(storageConnectionString);
                }

                else //fallback path to Cloud storage
                {
                    cloudAcct = new CloudStorageAccount(new StorageCredentialsAccountAndKey("maargsoft",                          "njPcqdHZuYUNbp32GS1tpSAeoUSp1hZ1EJsqRdtnTJe5BZtEmVd61UHonvva6o3WZ1COeAPtTm4ofbMqFaqj7Q=="), false);
                }

                queueClient = cloudAcct.CreateCloudQueueClient();
                blobClient = cloudAcct.CreateCloudBlobClient();
                this.tableClient = cloudAcct.CreateCloudTableClient();

                bool runtimeEnv = !string.IsNullOrEmpty(MaargConfiguration.Instance[MaargConfiguration.FatpipeManagerIncomingQueueName]);
                if (runtimeEnv)
                {
                    incomingQ = queueClient.GetQueueReference(MaargConfiguration.Instance[MaargConfiguration.FatpipeManagerIncomingQueueName]);
                    outgoingQ = queueClient.GetQueueReference(MaargConfiguration.Instance[MaargConfiguration.FatpipeManagerOutgoingQueueName]);

                    // Create the queue if it doesn't already exist
                    incomingQ.CreateIfNotExist();
                    outgoingQ.CreateIfNotExist();

                    DateTime today = DateTime.Today;
                    string bucket = string.Format(BlobContainerPartitionNameFormat, today.Month, today.Year,
                        MaargConfiguration.Instance[MaargConfiguration.FatpipeManagerIncomingBlobContainerNameSuffix]);

                    string outbucket = string.Format(BlobContainerPartitionNameFormat, today.Month, today.Year,
                        MaargConfiguration.Instance[MaargConfiguration.FatpipeManagerOutgoingBlobContainerNameSuffix]);

                    // Retrieve a reference to a container 
                    inContainer = blobClient.GetContainerReference(bucket);
                    outContainer = blobClient.GetContainerReference(outbucket);

                    // Create the container if it doesn't already exist
                    inContainer.CreateIfNotExist();
                    outContainer.CreateIfNotExist();
                }

                schemaContainer = blobClient.GetContainerReference(SchemaContainerName);
                schemaContainer.CreateIfNotExist();

                notificationTemplateContainer = blobClient.GetContainerReference(NotificationTemplateContainerName);
                notificationTemplateContainer.CreateIfNotExist();

                //suspended store
                if (runtimeEnv)
                {
                    this.suspendedContainer = this.blobClient.GetContainerReference(MaargConfiguration.Instance[MaargConfiguration.SuspendedMessageBlobContainerName]);
                    this.suspendedContainer.CreateIfNotExist();
                    this.tableClient.CreateTableIfNotExist(MaargConfiguration.Instance[MaargConfiguration.SuspendedMessageTableName]);
                    this.suspendedTableServiceContext = this.tableClient.GetDataServiceContext();

                    int visibilityTimeoutInSeconds;
                    if (!int.TryParse(MaargConfiguration.Instance[MaargConfiguration.FatpipeManagerQueueMessageVisibilityTimeoutInSeconds], out visibilityTimeoutInSeconds))
                    {
                        visibilityTimeoutInSeconds = 30;
                        LoggerFactory.Logger.Warning("FatPipeDAL C'tor", EventId.BizpipeMissingConfigValue
                            , "Configuration for {0} is not defined. Using default value of {1} seconds."
                            , MaargConfiguration.FatpipeManagerQueueMessageVisibilityTimeoutInSeconds
                            , visibilityTimeoutInSeconds);
                    }
                    this.visibilityTimeout = new TimeSpan(0, 0, visibilityTimeoutInSeconds);

                    // does it need public access?
                    this.inContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                    this.outContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                    this.suspendedContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                }
                LoggerFactory.Logger.Debug("FatPipeDAL C'tor", "Connections established to Azure accounts");
            }
            catch (Exception excp)
            {
                LoggerFactory.Logger.Error("FatPipeDAL C'tor", EventId.BizpipeCtor
                    , "FatPipeDAL constructor encountered exception: {0} ", excp.ToString());
            }
        }

        public bool Enqueue(IFatpipeMessage msg, Constants.QueueType type)
        {
            string location = "FpmDal->Enqueue";
            Stopwatch watch;

            try
            {
                // store the entire message in Blob
                string blobName = msg.Header.Identifier;
                CloudBlob blob;
                bool inbound = (type == Constants.QueueType.INBOUND);

                Stream blobStream = BizpipeMesssageStreaming.WriteBizpipeMessageToStream(msg);
                // The entry in the Q is the message blob name
                CloudQueueMessage entry = new CloudQueueMessage(blobName);
                //SetQueueBodyData(entry, blobStream);

                if (inbound)
                {
                    //Measure and write to blob storage
                    watch = Stopwatch.StartNew();
                    blob = inContainer.GetBlobReference(blobName);
                    blob.UploadFromStream(blobStream);
                    watch.Stop();
                    LoggerFactory.Logger.Info(location + "->SaveToIncomingBlob", "timeInMs={0} blobName={1}", 
                        watch.ElapsedMilliseconds, blobName);
                    
                    //Measure and write to incomingQ
                    watch = Stopwatch.StartNew();
                    incomingQ.AddMessage(entry);
                    watch.Stop();
                    LoggerFactory.Logger.Info(location + "->WriteToIncomingQ", "timeInMs={0} entry={1}",
                        watch.ElapsedMilliseconds, blobName);
                }
                else
                {
                    //Measure and write to blob storage
                    watch = Stopwatch.StartNew();
                    blob = outContainer.GetBlobReference(blobName);
                    blob.UploadFromStream(blobStream);
                    watch.Stop();
                    LoggerFactory.Logger.Info(location + "->B2BEngineWritingToOutgoingBlob", "timeInMs={0}, blob={1}",
                        watch.ElapsedMilliseconds, blobName);

                    //Measure and write to incomingQ
                    watch = Stopwatch.StartNew();
                    outgoingQ.AddMessage(entry);
                    watch.Stop();
                    LoggerFactory.Logger.Info(location + "->B2BEngineWritingToOutgoingQ", "timeInMs={0}, entry={1}",
                        watch.ElapsedMilliseconds, blobName);
                }

                // set the QueueName and the queue message identifiers
                msg.Header.QueueName = (inbound == true) ? incomingQ.Name : outgoingQ.Name;
                msg.Header.QueueMessageId = entry.Id;
                msg.Header.QueueMessagePopReceipt = entry.PopReceipt;
            }

            catch (Exception ex)
            {
                LoggerFactory.Logger.Warning(location, EventId.BizpipeEnqueueMessage
                    , "Exception encountered during Enqueue operation: {0}", ex.ToString());
                return false;
            }

            return true;

        }

        public IFatpipeMessage Dequeue(Constants.QueueType type)
        {
            string location = "FpmDal->Dequeue";
            IFatpipeMessage fatpipeMessage = null;

            bool inbound = (type == Constants.QueueType.INBOUND);
            CloudQueue queue = inbound ? this.incomingQ : this.outgoingQ;
            CloudBlobContainer container = inbound ? this.inContainer : this.outContainer;
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                CloudQueueMessage entry = queue.GetMessage(this.visibilityTimeout);
                if (entry != null)
                {
                    watch.Stop();
                    LoggerFactory.Logger.Info(location + (inbound ? "->B2BEngineReadingFromIncomingQ" : "OutboundTransportReadingFromOutgoingQ"),
                        "timeInMs={0}, entry={1}",
                        watch.ElapsedMilliseconds, entry.AsString);

                    byte[] buffer = null;
                    bool isInlineContent = inbound ? GetContent(entry, out buffer) : false;
                    MemoryStream blobStream = null;
                    if (isInlineContent)
                    {
                        blobStream = new MemoryStream(buffer);
                    }
                    else
                    {
                        watch = Stopwatch.StartNew();
                        string blobName = entry.AsString;
                        CloudBlob blob = container.GetBlobReference(blobName);
                        BlobRequestOptions options = new BlobRequestOptions();
                        blobStream = new MemoryStream();
                        blob.DownloadToStream(blobStream);
                        watch.Stop();
                        LoggerFactory.Logger.Info(location + (inbound ? "->B2BEngineReadingFromIncomingBlob" : "OutboundTransportReadingFromOutgoingBlob"),
                            "timeInMs={0}, blob={1}",
                            watch.ElapsedMilliseconds, blobName);
                    }
                    blobStream.Position = 0;
                    //msg = DecodeBlobPayload(blobStream, this.fpm);
                    fatpipeMessage = BizpipeMesssageStreaming.CreateBizpipeMessageFromStream(blobStream, this.fpm);
                    // set the QueueName and the queue message identifiers
                    fatpipeMessage.Header.QueueName = (inbound == true) ? incomingQ.Name : outgoingQ.Name;
                    fatpipeMessage.Header.QueueMessageId = entry.Id;
                    fatpipeMessage.Header.QueueMessagePopReceipt = entry.PopReceipt;
                    fatpipeMessage.Status.NumberOfRetryAttempts = entry.DequeueCount;
                }
            }
            catch (Exception ex)
            {
                LoggerFactory.Logger.Warning(location, EventId.BizpipeDequeueMessage
                    , "Exception encountered during Dequeue operation: {0}", ex.ToString());
            }

            return fatpipeMessage;
        }

        public void RemoveMessageFromQueue(IFatpipeMessage message)
        {
            string location = "FpmDal->RemoveMessageFromQueue";

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (!string.IsNullOrEmpty(message.Header.QueueName))
            {
                CloudQueue queue = null;
                if (message.Header.QueueName == this.incomingQ.Name)
                {
                    queue = this.incomingQ;
                }

                if (message.Header.QueueName == this.outgoingQ.Name)
                {
                    queue = this.outgoingQ;
                }

                if (queue != null)
                {
                    try
                    {
                        queue.DeleteMessage(message.Header.QueueMessageId, message.Header.QueueMessagePopReceipt);
                    }
                    catch (Exception exception)
                    {
                        LoggerFactory.Logger.Error(location, EventId.BizpipeRemoveMessage
                            , "Error deleting message (Id = {0}, PopReceipt = {1}) from queue {2}: {3}.",
                            message.Header.QueueMessageId,
                            message.Header.QueueMessagePopReceipt,
                            queue.Name,
                            exception.ToString());
                        throw;
                    }
                }
                else
                {
                    LoggerFactory.Logger.Error(location, EventId.BizpipeStrayMessage
                        , "Queue message is attached to queue {0} which is neither the inbound ({1}) nor the outbound ({2}) queue.",
                        message.Header.QueueName,
                        this.incomingQ.Name,
                        this.outgoingQ.Name);
                }
            }
        }

        /// <summary>
        /// Retrieve schema from underlying storage.
        /// </summary>
        /// <param name="schemaId"></param>
        /// <returns>Stream to the content</returns>
        public Stream RetrieveSchema(string schemaId)
        {
            string ns, name;
            RetrieveNamespaceAndName(schemaId, out ns, out name);
            string blobName = ConstructBlobName(name, ns);

            CloudBlob blob = schemaContainer.GetBlobReference(blobName);
            MemoryStream ms = new MemoryStream();
            blob.DownloadToStream(ms);
            ms.Position = 0;
            return ms;
        }

        public Stream RetrieveMap(string mapId)
        {
            CloudBlob blob = schemaContainer.GetBlobReference(mapId);
            MemoryStream ms = new MemoryStream();
            blob.DownloadToStream(ms);
            ms.Position = 0;
            return ms;
        }

        public string RetrieveNotificationTemplate(string templateId)
        {
            CloudBlob blob = notificationTemplateContainer.GetBlobReference(templateId);
            MemoryStream ms = new MemoryStream();
            blob.DownloadToStream(ms);
            ms.Position = 0;
            string data = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            return data;
        }

        private static string ConstructBlobName(string name, string ns)
        {
            string blobName = string.Format("{0}.xsd", name);
            return blobName;
        }

        public static void RetrieveNamespaceAndName(string docType, out string ns, out string name)
        {
            string[] names = docType.Split(new char[] { '#' });
            if (names == null || names.Length != 2)
            {
                names = new string[2];
                names[0] = string.Empty;
                names[1] = docType;
            }

            ns = names[0];
            name = names[1];
        }

        private static bool GetContent(CloudQueueMessage queueMessage, out byte[] buffer)
        {
            buffer = null;
            bool inlineContent = false;
            return inlineContent;

            try
            {
                if (queueMessage != null)
                {
                    buffer = queueMessage.AsBytes;
                    inlineContent = buffer != null && buffer.Length > 0;
                }
            }
            catch
            {

            }

            return inlineContent;
        }

        public bool SaveSuspendedMessage(IFatpipeMessage message)
        {
            string location = "FpmDal->SaveSuspendedMessage";
            bool result = true;
            SuspendedMessageEntity suspendedMessage = null;

            //TODO remove the return statement
            // don't want any message to be suspended
            return true;

            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                suspendedMessage = new SuspendedMessageEntity(message.Header.TenantIdentifier, message.Header.Identifier);
                suspendedMessage.Timestamp = DateTime.UtcNow;
                suspendedMessage.SuspendedMessageBlobReference = message.Header.Identifier;
                if (message.Status != null)
                {
                    suspendedMessage.ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                        "{0}. Message suspended after {1} retries. Error: {2}",
                        message.Status.ProcessingResult,
                        message.Status.NumberOfRetryAttempts,
                        message.Status.ErrorDescription);
                }
                IOutboundFatpipeMessage outboundMessage = message as IOutboundFatpipeMessage;
                if (outboundMessage != null)
                {
                    if (outboundMessage.RoutingInfo != null)
                    {
                        suspendedMessage.PartnerIdentifier = outboundMessage.RoutingInfo.PartnerId;
                    }
                }

                this.suspendedTableServiceContext.AddObject(MaargConfiguration.Instance[MaargConfiguration.SuspendedMessageTableName], suspendedMessage);

                //save to suspended table and blob
                DataServiceResponse response = this.suspendedTableServiceContext.SaveChangesWithRetries();
                // The entry in the Q is the message blob name
                using (Stream stream = BizpipeMesssageStreaming.WriteBizpipeMessageToStream(message))
                {
                    CloudBlob blob = this.suspendedContainer.GetBlobReference(suspendedMessage.SuspendedMessageBlobReference);
                    blob.UploadFromStream(stream);
                }

                watch.Stop();
                LoggerFactory.Logger.Debug(location,
                    "Message {0} saved to suspended queue in {1} ms.",
                    message.Header.Identifier,
                    watch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error(location, EventId.BizpipeSaveSuspendMessage
                    , "Exception encountered during suspended message operation: {0}."
                    , exception.ToString());
                result = false;
            }

            return result;
        }

        public IFatpipeMessage GetSuspendedMessage(string messageIdentifier)
        {
            string location = "FpmDal->GetSuspendedMessage";
            IFatpipeMessage result = null;
            SuspendedMessageEntity suspendedMessage = null;

            try
            {
                Stopwatch watch = Stopwatch.StartNew();

                suspendedMessage =
                    (from e in this.suspendedTableServiceContext.CreateQuery<SuspendedMessageEntity>(MaargConfiguration.Instance[MaargConfiguration.SuspendedMessageTableName])
                     where e.RowKey == messageIdentifier
                     select e).FirstOrDefault();

                if (suspendedMessage != null)
                {
                    IFatpipeMessage message = null;
                    CloudBlob blob = this.suspendedContainer.GetBlobReference(suspendedMessage.SuspendedMessageBlobReference);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        blob.DownloadToStream(stream);
                        stream.Position = 0;
                        message = BizpipeMesssageStreaming.CreateBizpipeMessageFromStream(stream, this.fpm);
                    }

                    if (!string.IsNullOrEmpty(suspendedMessage.PartnerIdentifier))
                    {
                        IOutboundFatpipeMessage outboundMessage = this.fpm.CreateNewOutboundMessage();
                        outboundMessage.Header = message.Header;
                        outboundMessage.Body = message.Body;
                        outboundMessage.Status = message.Status;
                        outboundMessage.RoutingInfo = new RoutingInfo();
                        outboundMessage.RoutingInfo.PartnerId = suspendedMessage.PartnerIdentifier;
                        outboundMessage.RoutingInfo.TransportType = TransportType.Suspend;

                        result = outboundMessage;
                    }
                    else
                    {
                        result = message;
                    }
                }

                watch.Stop();
                LoggerFactory.Logger.Debug(location,
                    "Message {0} retrieved from suspended queue in {1} ms.",
                    messageIdentifier,
                    watch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error(location, EventId.BizpipeGetSuspendMessage
                    , "Error getting the suspended message {0}: {1}."
                    , messageIdentifier
                    , exception.ToString());
            }

            return result;
        }

        public bool DeleteSuspendedMessage(string tenantIdentifier, string messageIdentifier)
        {
            string location = "FpmDal->DeleteSuspendedMessage";
            bool result = true;
            SuspendedMessageEntity suspendedMessage = null;

            try
            {
                Stopwatch watch = Stopwatch.StartNew();

                suspendedMessage =
                    (from e in this.suspendedTableServiceContext.CreateQuery<SuspendedMessageEntity>(MaargConfiguration.Instance[MaargConfiguration.SuspendedMessageTableName])
                     where e.PartitionKey == tenantIdentifier && e.RowKey == messageIdentifier
                     select e).FirstOrDefault();

                if (suspendedMessage != null)
                {
                    //delete from suspended table
                    this.suspendedTableServiceContext.DeleteObject(suspendedMessage);
                    this.suspendedTableServiceContext.SaveChangesWithRetries();

                    //delete from suspended blob
                    CloudBlob blob = this.suspendedContainer.GetBlobReference(suspendedMessage.SuspendedMessageBlobReference);
                    result = blob.DeleteIfExists();
                }

                watch.Stop();
                LoggerFactory.Logger.Info(location,
                    "Message {0} deleted from suspended queue in {1} ms.",
                    messageIdentifier,
                    watch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                result = false;
                LoggerFactory.Logger.Error(location, EventId.BizpipeDeleteSuspendMessage
                    , "Error deleting the suspended message {0}: {1}."
                    , messageIdentifier
                    , exception.ToString());
            }

            return result;
        }

        public IEnumerable<IFatpipeMessage> ListSuspendedMessages(string tenantIdentifier)
        {
            string location = "FpmDal->ListSuspendedMessage";
            List<IFatpipeMessage> result = new List<IFatpipeMessage>();

            try
            {
                Stopwatch watch = Stopwatch.StartNew();

                string tableName = MaargConfiguration.Instance[MaargConfiguration.SuspendedMessageTableName];
                var suspendedMessages =
                (
                    from e in this.suspendedTableServiceContext.CreateQuery<SuspendedMessageEntity>(tableName)
                    where e.PartitionKey == tenantIdentifier
                    select e
                ).AsTableServiceQuery<SuspendedMessageEntity>();

                foreach (var suspendedMessage in suspendedMessages)
                {
                    IFatpipeMessage message = this.GetSuspendedMessage(suspendedMessage.RowKey);
                    if (message != null)
                    {
                        result.Add(message);
                    }
                }

                watch.Stop();
                LoggerFactory.Logger.Info(location,
                    "Listed suspended messages for tenant {0} in {1} ms.",
                    tenantIdentifier,
                    watch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error(location, EventId.BizpipeListSuspendMessages
                    , "Error listing suspended messages for tenant {0}: {1}."
                    , tenantIdentifier
                    , exception.ToString());
                throw;
            }

            return result;
        } 
    }
    /// <summary>
    /// 
    /// </summary>

    class Tenant
    {
        public int _tenantid;
        public string TenantName;

        public string AzureStorageConnectionString;

        public Tenant(int tenantId, string tenantName, string storageConnectionString)
        {
            _tenantid = tenantId;
            TenantName = tenantName;
            this.AzureStorageConnectionString = storageConnectionString;
        }

        public string getNextMsgID()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 15);
        }
    }

    public static class Constants
    {
        

        public enum QueueType : short
        {
            INBOUND,
            OUTBOUND
        }

    }
}
  