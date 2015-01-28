using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class OutboundEdiPropertyBag : BasePropertyBag, IOutboundEdiPropertyBag
    {
        private int sequenceNumber;
        private string isa1;
        private string isa2;
        private string isa16;

        public int SequenceNumber { get { return this.sequenceNumber; } set { this.sequenceNumber = value; } }

        public string Isa1 { get { return this.isa1; } set { this.isa1 = value; } }

        public string Isa2 { get { return this.isa2; } set { this.isa2 = value; } }

        public string Isa16 { get { return this.isa16; } set { this.isa16 = value; } }
    }
}
