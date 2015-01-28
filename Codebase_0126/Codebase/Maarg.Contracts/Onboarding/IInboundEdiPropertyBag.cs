using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public interface IInboundEdiPropertyBag : IBasePropertyBag
    {
        bool CheckInterchangeLevelDuplicate { get; set; }
        bool GenerateTA1 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ack", Justification = "Correct spelling is used.")]
        bool GenerateFunctionalAck { get; set; }
    }
}
