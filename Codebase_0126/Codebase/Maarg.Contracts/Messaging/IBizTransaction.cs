using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    /// <summary>
    /// Type of business transaction
    /// </summary>
    public enum BizTransactionType
    {
        Message,
        PurchaseOrder,
    }

    /// <summary>
    /// Interface for business transaction
    /// </summary>
    public interface IBizTransaction
    {
        BizTransactionType Type { get; }
    }
}
