using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Maarg.Contracts
{
    /// <summary>
    /// BizTransactionManager that adds a business context on top of
    /// raw FatpipeMessages. This component is called from BizView to show
    /// the transaction history for a particular Partner displaying the
    /// messages with a set of pivots
    /// </summary>
    public interface IBizTransactionManager
    {
        /// <summary>
        /// Get the list of inbound messages coming from a partner
        /// </summary>
        /// <param name="fromPartner">The sender partner</param>
        /// <returns>List of inbound messages</returns>
        List<IFatpipeMessage> GetInboundMessages(IPartner fromPartner);

        /// <summary>
        /// Get the list of outbound messages delivering to a partner
        /// </summary>
        /// <param name="toPartner">The recipient partner</param>
        /// <returns>List of outbound messages</returns>
        List<IFatpipeMessage> GetOutboundMessages(IPartner toPartner);
        
        /// <summary>
        /// Get the list of messages processed by the system and coming from a partner
        /// </summary>
        /// <param name="fromPartner">The sender partner</param>
        /// <returns>List of processed messages</returns>
        List<IFatpipeMessage> GetProcessedMessages(IPartner fromPartner);

        /// <summary>
        /// Get the list of inbound messages coming from a partner within a date range
        /// </summary>
        /// <param name="fromPartner">The sender partner</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of inbound messages</returns>
        List<IFatpipeMessage> GetInboundMessages(IPartner fromPartner, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get the list of outbound messages delivering to a partner within a date range
        /// </summary>
        /// <param name="toPartner">The recipient partner</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of outbound messages</returns>
        List<IFatpipeMessage> GetOutboundMessages(IPartner toPartner, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get the list of messages processed by the system and coming from a partner within a date range
        /// </summary>
        /// <param name="fromPartner">The sender partner</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of processed messages</returns>
        List<IFatpipeMessage> GetProcessedMessages(IPartner fromPartner, DateTime startDate, DateTime endDate);

        /// more queries will be added later
    }
}
