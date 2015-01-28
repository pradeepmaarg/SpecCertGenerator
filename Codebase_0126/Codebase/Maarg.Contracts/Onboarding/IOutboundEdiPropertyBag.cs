using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public interface IOutboundEdiPropertyBag : IBasePropertyBag
    {
        int SequenceNumber { get; set; }
        string Isa1 { get; set; }
        string Isa2 { get; set; }
        //...
        //...
        string Isa16 { get; set; }
    }
}
