using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public interface IOutboundAs2PropertyBag : IBasePropertyBag
    {
        bool ApplySignature { get; set; }
        bool ApplyEncryption { get; set; }
    }
}
