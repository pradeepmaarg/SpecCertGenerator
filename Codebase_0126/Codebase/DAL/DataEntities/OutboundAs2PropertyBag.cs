using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class OutboundAs2PropertyBag : BasePropertyBag, IOutboundAs2PropertyBag
    {
        private bool applySignature;
        private bool applyEncryption;

        public bool ApplySignature { get { return this.applySignature; } set { this.applySignature = value; } }

        public bool ApplyEncryption { get { return this.applyEncryption; } set { this.applyEncryption = value; } }
    }
}
