using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class InboundEdiPropertyBag : BasePropertyBag, IInboundEdiPropertyBag
    {
        private bool checkInterchangeLevelDuplicate;
        private bool generateTA1;
        private bool generateFunctionalAck;

        public bool CheckInterchangeLevelDuplicate { get { return this.checkInterchangeLevelDuplicate; } set { this.checkInterchangeLevelDuplicate = value; } }

        public bool GenerateTA1 { get { return this.generateTA1; } set { this.generateTA1 = value; } }

        public bool GenerateFunctionalAck { get { return this.generateFunctionalAck; } set { this.generateFunctionalAck = value; } }
    }
}
