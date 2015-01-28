using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public interface IBasePropertyBag
    {
        B2BProtocolType ProtocolType { get; set; }
        PropertyBagType BagType { get; set; }
    }
}
