using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class BasePropertyBag : IBasePropertyBag
    {
        private B2BProtocolType protocolType;
        private PropertyBagType bagType;

        public B2BProtocolType ProtocolType
        {
            get
            {
                return this.protocolType;
            }
            set
            {
                this.protocolType = value;
            }
        }

        public PropertyBagType BagType
        {
            get
            {
                return this.bagType;
            }
            set
            {
                this.bagType = value;
            }
        }
    }
}
