using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    /// <summary>
    /// Business message type
    /// </summary>
    public enum BizMessageType
    {
        /// <summary>
        /// Claim Acknowledgment 277
        /// </summary>
        CA277,

        /// <summary>
        /// Purchase Order 850
        /// </summary>
        PO850,

        /// <summary>
        /// Purchase Order Acknowledgment 855
        /// </summary>
        POA855,

        /// <summary>
        /// Advanced Shipping Notice 856
        /// </summary>
        ASN856,

        /// <summary>
        /// Invoice 810
        /// </summary>
        INV810,
    }

    /// <summary>
    /// Interface for business task
    /// </summary>
    public interface IBizMessage
    {
        /// <summary>
        /// Unique id used for identifying and updating the message in storage
        /// </summary>
        string StoreId { get; set; }

        /// <summary>
        /// Unique id
        /// </summary>
        string UniqueId { get; set; }

        /// <summary>
        /// Created time
        /// </summary>
        DateTime CreatedTime { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        BizMessageType Type { get; set; }

        /// <summary>
        /// Payload string
        /// </summary>
        string Payload { get; set; }

        /// <summary>
        /// List of actions
        /// </summary>
        List<BizMessageAction> Actions { get; }
    }
}
