using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    /// <summary>
    /// Message action status enum
    /// </summary>
    public enum MessageActionStatus
    {
        Open,
        Closed,
        Archived,
        Received,
        Shipped,
        Invoiced,
    }

    /// <summary>
    /// Business message action
    /// </summary>
    public class BizMessageAction
    {
        /// <summary>
        /// Payload section (in XPath format)
        /// </summary>
        public string PayloadSection { get; set; }

        /// <summary>
        /// Message description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Message action status
        /// </summary>
        public MessageActionStatus Status { get; set; }

        /// <summary>
        /// Created time
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }
}
