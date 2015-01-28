using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    [Serializable]
    public class MessageHeader
    {
        private string identifier;
        private string tenantIdentifier;
        private Dictionary<string, string> context;
        private string contentType;
        private string queueName;
        private string queueMessageId;
        private string queueMessagePopReceipt;
        private string correlationId;

        /// <summary>
        /// Gets or sets the message header identifier.
        /// </summary>
        /// <remarks>
        /// System generated unique identifier of a message. Can be used for lookup.
        /// It can internally contain partition, account, blob name etc information. Opaque to clients though.
        /// </remarks>
        public string Identifier { get { return this.identifier; } set { this.identifier = value; } }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        /// <remarks>
        /// Tenant to which this message belongs to - REQUIRED field.
        /// Note that parnter may not be identified yet. It depends on which agent is processing the message.
        /// Eg. InboundTransport may not know partner since it does not rip open the message always.
        /// </remarks>
        public string TenantIdentifier { get { return this.tenantIdentifier; } set { this.tenantIdentifier = value; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Need to be able to set the context.")]
        public Dictionary<string, string> Context { get { return this.context; } set { this.context = value; } }

        public string ContentType { get { return this.contentType; } set { this.contentType = value; } }
        
        /// <summary>
        /// Gets or sets the queue that contains this message. Can be blank if not enqueued.
        /// </summary>
        public string QueueName { get { return this.queueName; } set { this.queueName = value; } }

        public string QueueMessageId { get { return this.queueMessageId; } set { this.queueMessageId = value; } }
        public string QueueMessagePopReceipt { get { return this.queueMessagePopReceipt; } set { this.queueMessagePopReceipt = value; } }

        /// <summary>
        /// Gets or sets the Id of Message that it correlates to, can be used to generate a tree of messages.
        /// </summary>
        public string CorrelationId { get { return this.correlationId; } set { this.correlationId = value; } }

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHeader"/> type.
        /// </summary>
        /// <param name="tenantIdentifier">The tenant identifier.</param>
        /// <param name="identifier">The message header identifier.</param>
        public MessageHeader(string tenantIdentifier, string identifier)
        {
            this.TenantIdentifier = tenantIdentifier;
            this.Identifier = identifier;

            this.Context = new Dictionary<string, string>();
        }
    }
}