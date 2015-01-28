using System;
using System.Collections.Generic;
using System.IO;
using Maarg.Contracts;

namespace Maarg.FatpipeAPI
{
    #region Interfaces

    /// <summary>
    /// Fun begins now :)
    /// </summary>
    public interface IFatpipeManager
    {
        IFatpipeMessage CreateNewMessage();
        IOutboundFatpipeMessage CreateNewOutboundMessage();

        /// <summary>
        /// Retrieve map from underlying storage.
        /// </summary>
        /// <param name="mapId"></param>
        /// <returns>Stream to the content</returns>
        Stream RetrieveMap(string mapId);
        
        /// <summary>
        /// Retrieve schema from underlying storage.
        /// </summary>
        /// <param name="schemaId"></param>
        /// <returns>Stream to the content</returns>
        Stream RetrieveSchema(string schemaId);

        /// <summary>
        /// Retrieve notification template from underlying storage.
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns>string</returns>
        string RetrieveNotificationTemplate(string templateId);


        /// <summary>
        /// Used by Inbound Connectors, to enQ reference to message in one of many incomingQueues and save as Blob
        /// If Message is in Error, it should be put in SuspendedQueue instead
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="agent"></param>
        /// <returns></returns>
        bool EnQToIncomingMessageQ(List<IFatpipeMessage> messages, IMessageHandlerAgent agent);

        
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
        List<IFatpipeMessage> DeQFromIncomingQ(int count);

        /// <summary>
        /// Remove a message from a Q after it has been processed, with success or failure
        /// message contains the owning Q to go to
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool RemoveFromQ(IFatpipeMessage message);

        /// <summary>
        /// This method will be used by B2BProtocolEngine and OutboundTransportManager to retry
        /// message in the wake of transient failures.
        /// 
        /// They would specify a timeSpan, which is the minimum time before retry should be
        /// attempted. The implementation needs to accomodate that requirement
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        bool MoveToEndForRetry(IFatpipeMessage message, TimeSpan ts);

        bool RetryOrSuspendMessage(IFatpipeMessage message);

        //Write message reference to permanent store, not Queue
        bool SuspendMessage(IFatpipeMessage message);

        /// <summary>
        /// B2B Protocol Engine will take batch of messages from Incoming queue, process each of them
        /// and Write them to OutboundQ with routing info or Suspend them
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="agent"></param>
        /// <returns></returns>
        bool EnQToOutboundMessageQ(List<IOutboundFatpipeMessage> messages, IMessageHandlerAgent agent);

        /// <summary>
        /// Used by OutboundTransportManager
        /// 
        /// 1. DeQ bunch of messages from OutboundQueue
        /// 2. Transport each of them
        /// 3. RemoveFromQ each of them or suspend them as appropriate
        /// </summary>
        /// <param name="count">Number of messages to DeQ</param>
        /// <returns></returns>
        List<IOutboundFatpipeMessage> DeQFromOutgoingQ(int count);

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
        bool WriteBusinessTransactionToStore(IFatpipeMessage message, IMessageHandlerAgent agent); 
    }


    /// <summary>
    /// There are 3 agents in the system that work on messages
    /// 1. Inbound Connectors
    /// 2. B2BProtocolEngine
    /// 3. Outbound Connectors
    /// 
    /// This interface is for modeling that. These are general purpose agents, they are
    /// not affinitized to a tenant, unlike messages
    /// </summary>
    public interface IMessageHandlerAgent
    {
        //Who Am I
        AgentType AgentType { get; set; }

        //What did I do to messages. In all of these operations, messages can succeed or fail
        //That info is captured at message level
        OperationType OperationType { get; set; }

        //My identity
        string RoleName { get; set; } //name of actual Azure role
        string RoleInstanceId { get; set; }
        string ServerName { get; set; }

        // My service address. Like https://maarg.com for HTTP connector, b2b://protocolhandler for B2B
        string ServiceAddress { get; set; }
    }

    public enum AgentType : short
    {
        InboundConnector,
        B2BProtocolEngine,
        OutboundConnector
    }

    public enum OperationType : short
    {
        Receive,
        B2BProcessing,
        Send
    }

    public enum MetadataType : short
    {
        Schema,
        Map
    }

    #endregion

}
