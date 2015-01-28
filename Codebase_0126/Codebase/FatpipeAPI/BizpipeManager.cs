using System;
using System.Collections.Generic;
using System.IO;
using Maarg.AllAboard;
using Maarg.Contracts;
using Maarg.Fatpipe.LoggingService;

namespace Maarg.FatpipeAPI
{
    public class FatpipeManager : IFatpipeManager
    {
        static string _FatPipeManagerID;

        TenancyMode tenancyMode;
        static Tenant[] tenants;
        FatPipeDAL fpmDal;
        private int maximumNumberOfRetryAttempts;

        public enum TenancyMode : short
        {
            Single,
            Multi
        }
        /// <summary>
        /// FatPipeManagerID is used when several instances of this class is used, for example hosted as part of another service
        /// When that service is run in different Role instances, the Role Instance ID can be passed on as the FPM ID.
        /// MsgId generation would use the FPM ID as part of it.
        /// </summary>
        /// <param name="FatPipeManagerID"> A number or string used when there are multiple instances of this class.</param>
        /// <param name="mode"> Single or Multi</param>
        /// <param name="tenantName"> First tenant name</param>
        public FatpipeManager(string FatPipeManagerID, TenancyMode mode, string tenantName)
        {
            tenancyMode = mode;

            if (mode == TenancyMode.Multi)
            {
                // Move this hardcoded to a different Utils file
                tenants = new Tenant[100];

            }
            else
            {
                tenants = new Tenant[1];
            }

            //string storageConnectionString = MaargConfiguration.Instance[MaargConfiguration.TenantStorageAccountConnectionString];
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=maargsoft;AccountKey=njPcqdHZuYUNbp32GS1tpSAeoUSp1hZ1EJsqRdtnTJe5BZtEmVd61UHonvva6o3WZ1COeAPtTm4ofbMqFaqj7Q==";

            tenants[0] = new Tenant(1, tenantName, storageConnectionString);
            _FatPipeManagerID = FatPipeManagerID;

            // Might have to set this depending on the Tenant we are operating on
            fpmDal = new FatPipeDAL(tenants[0].AzureStorageConnectionString, this);

            //Message retry settings
            if (!int.TryParse(MaargConfiguration.Instance[MaargConfiguration.NumberOfRetriesBeforeSuspend], out this.maximumNumberOfRetryAttempts))
            {
                this.maximumNumberOfRetryAttempts = 3;
            }
        }


        const string MsgIdFormat = "{0}_{1}_{2}_{3}_{4}";
        static string MessageIDGenerator(Tenant t)
        {

            DateTime today = DateTime.Now;
            string bucket = today.Month.ToString() + today.Year.ToString();

            string messageId = string.Format(MsgIdFormat, _FatPipeManagerID, t.TenantName, bucket, today.Ticks, t.getNextMsgID());
            return messageId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IFatpipeMessage CreateNewMessage()
        {
            // TODO: assume Tenant is tenants[0]; will expect the tenant id from caller later
            Tenant t = tenants[0];
            FatpipeMessage fpm = new FatpipeMessage(t._tenantid.ToString(), MessageIDGenerator(t), this);
            return fpm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IOutboundFatpipeMessage CreateNewOutboundMessage()
        {
            // TODO: assume Tenant is tenants[0]; will expect the tenant id from caller later
            Tenant t = tenants[0];
            FatpipeMessage fpm = new FatpipeMessage(t._tenantid.ToString(), MessageIDGenerator(t), this);

            // Have to fix this, need to lookup the Agreement, etc for this Tenant and partner, etc.
            return (IOutboundFatpipeMessage)fpm;
        }

        /// <summary>
        /// Used by Inbound Connectors, to enQ reference to message in one of many incomingQueues and save as Blob
        /// If Message is in Error, it should be put in SuspendedQueue instead
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="agent"></param>
        /// <returns></returns>
        public bool EnQToIncomingMessageQ(List<IFatpipeMessage> messages, IMessageHandlerAgent agent)
        {
            bool success = false;
            int msgCnt = 0;
            int msgSuccess = 0;

            foreach (IFatpipeMessage msg in messages)
            {
                msgCnt++;
                success = fpmDal.Enqueue(msg, Constants.QueueType.INBOUND);
                if (success)
                {
                    msgSuccess++;
                }
            }

            return true;
        }

        /// <summary>
        /// Used by B2BProtocolEngine, it's a 3 step process. 
        /// 
        /// 1. First acquire lock (this method), so that other workers don't see the message for a period of time
        /// 2. Process message and write to OutboundQ or SuspendQ
        /// 3. Remove the message from Q 
        /// 
        /// </summary>
        /// <param name="count">Number of messages to DeQ</param>
        /// <returns></returns>
        public List<IFatpipeMessage> DeQFromIncomingQ(int count)
        {
            int iter = 0;
            IFatpipeMessage msg = null;
            List<IFatpipeMessage> msgList = new List<IFatpipeMessage>();

            while (iter < count)
            {
                msg = fpmDal.Dequeue(Constants.QueueType.INBOUND);
                if (msg == null) break;
                msgList.Add(msg);

                iter++;
            }

            return msgList;
        }

        /// <summary>
        /// Remove a message from a Q after it has been processed, with success or failure
        /// message contains the owning Q to go to
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool RemoveFromQ(IFatpipeMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            bool result = true;
            try
            {
                this.fpmDal.RemoveMessageFromQueue(message);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error("FatpipeManager.RemoveFromQueue", EventId.BizpipeRemoveFromQueue,
                    "Failed to delete message (Id: {0}, PopReceipt: {1}) from queue {2}. Error: {3}",
                    message.Header.QueueMessageId,
                    message.Header.QueueMessagePopReceipt,
                    message.Header.QueueName,
                    exception.ToString());
                result = false;
            }

            return result;
        }

        public IEnumerable<IFatpipeMessage> ListSuspendedMessages(string tenantIdentifier)
        {
            if (tenantIdentifier == null)
            {
                throw new ArgumentNullException("tenantIdentifier");
            }

            return this.fpmDal.ListSuspendedMessages(tenantIdentifier);
        }

        public bool RetryOrSuspendMessage(IFatpipeMessage message)
        {
            if (message.Status.NumberOfRetryAttempts >= this.maximumNumberOfRetryAttempts)
            {
                return this.SuspendMessage(message);
            }

            //Message will become visible again after the preset period of invisibility; no need to add another visibility setting.
            return true;
        }

        //Write message reference to permanent store, not Queue
        public bool SuspendMessage(IFatpipeMessage message)
        {
            bool result = true;
            string location = "FatpipeManager.SuspendMessage";
            try
            {
                //delete from queue
                this.fpmDal.RemoveMessageFromQueue(message);
                //save to suspended store
                result = this.fpmDal.SaveSuspendedMessage(message);
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Error(location, EventId.BizpipeSuspendMessage,
                    "Failed to suspend message {0}: {1}.", 
                    message.Header.Identifier, 
                    exception.ToString());
                result = false;
            }

            return result;
        }

        /// <summary>
        /// B2B Protocol Engine will take batch of messages from Incoming queue, process each of them
        /// and Write them to OutboundQ with routing info or Suspend them
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="agent"></param>
        /// <returns></returns>
        public bool EnQToOutboundMessageQ(List<IOutboundFatpipeMessage> messages, IMessageHandlerAgent agent)
        {
            bool success = false;
            int msgCnt = 0;
            int msgSuccess = 0;

            foreach (IOutboundFatpipeMessage msg in messages)
            {
                msgCnt++;

                if (msg.RoutingInfo != null && !string.IsNullOrEmpty(msg.RoutingInfo.PartnerId))
                {
                    msg.Header.Context["PartnerId"] = msg.RoutingInfo.PartnerId;
                    msg.Header.Context["TransportType"] = msg.RoutingInfo.TransportType.ToString();
                }

                if (msg.RoutingInfo != null)
                {
                    switch (msg.RoutingInfo.TransportType)
                    {
                        case TransportType.Suspend:
                            success = this.fpmDal.SaveSuspendedMessage(msg);
                            break;
                        default:
                            success = this.fpmDal.Enqueue(msg as IFatpipeMessage, Constants.QueueType.OUTBOUND);
                            break;
                    }
                }
                else
                {
                    //TODO: remove after confirming that all outbound messages have routing info
                    success = fpmDal.Enqueue(msg as IFatpipeMessage, Constants.QueueType.OUTBOUND);
                }

                if (success)
                {
                    msgSuccess++;
                }
            }

            return (msgCnt == msgSuccess);
        }

        /// <summary>
        /// Used by OutboundTransportManager
        /// 
        /// 1. DeQ bunch of messages from OutboundQueue
        /// 2. Transport each of them
        /// 3. RemoveFromQ each of them or suspend them as appropriate
        /// </summary>
        /// <param name="count">Number of messages to DeQ</param>
        /// <returns></returns>
        public List<IOutboundFatpipeMessage> DeQFromOutgoingQ(int count)
        {
            int iter = 0;
            IFatpipeMessage msg = null;
            List<IOutboundFatpipeMessage> msgList = new List<IOutboundFatpipeMessage>();

            while (iter < count)
            {
                msg = fpmDal.Dequeue(Constants.QueueType.OUTBOUND);
                if (msg == null) break;

                IOutboundFatpipeMessage message = msg as IOutboundFatpipeMessage;
                string partnerId;
                bool flag = msg.Header.Context.TryGetValue("PartnerId", out partnerId);
                if (flag)
                {
                    RoutingInfo routingInfo = new RoutingInfo(partnerId, TransportType.None);
                    flag = msg.Header.Context.TryGetValue("TransportType", out partnerId);
                    routingInfo.TransportType = (TransportType)Enum.Parse(typeof(TransportType), partnerId, true);
                    message.RoutingInfo = routingInfo;
                }

                msgList.Add(message);
                iter++;
            }

            return msgList;
        }

        /// <summary>
        /// This is a very interesting method. It provides optics into what's going on in the system.
        /// Logic will be specific to agent.
        /// Refer to CorrelationId in message which stitches multiple messages together
        /// Eg. EDI Message M1, received, by AS2
        ///     M1 (correlationId), split into Ms1, Ms2, Ms3, Ms4, by B2BProtocolEngine
        ///     Ms1, sent, by HTTP
        ///     Ms2, sent, by AS2
        ///     Ms2, suspended, by AS2
        /// Common operations are
        /// messageM1, Received, 4/9/2012, By https://maarg.com, WebRole1, WebRoleId1234, ServerCloudMaarg1
        /// 
        /// This is a very interesting subject and implementation can be very intelligent.
        /// We will start with a simple one though
        /// </summary>
        /// <param name="message"></param>
        /// <param name="agent"></param>
        /// <returns></returns>
        public bool WriteBusinessTransactionToStore(IFatpipeMessage message, IMessageHandlerAgent agent)
        {
            return false;
        }

        /// <summary>
        /// Retrieve map from underlying storage.
        /// </summary>
        /// <param name="mapId"></param>
        /// <returns>Stream to the content</returns>
        public Stream RetrieveMap(string mapId)
        {
            return fpmDal.RetrieveMap(mapId);
        }

        /// <summary>
        /// Retrieve schema from underlying storage.
        /// </summary>
        /// <param name="schemaId"></param>
        /// <returns>Stream to the content</returns>
        public Stream RetrieveSchema(string schemaId)
        {
            return fpmDal.RetrieveSchema(schemaId);
        }

        public string RetrieveNotificationTemplate(string templateId)
        {
            return fpmDal.RetrieveNotificationTemplate(templateId);
        }
    }
}
