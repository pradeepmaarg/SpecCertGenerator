using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class InboundAs2PropertyBag : BasePropertyBag, IInboundAs2PropertyBag
    {
        private bool generateMDN;

        public bool GenerateMDN { get { return this.generateMDN; } set { this.generateMDN = value; } }
    }
}
