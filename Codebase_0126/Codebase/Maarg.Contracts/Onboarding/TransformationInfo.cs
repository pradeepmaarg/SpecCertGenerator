using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public class TransformationInfo
    {
        private int providerId;
        // TODO: Uncomment the following once project dependency sorted out
        //private ITranformPlug transformPlug;

        public int ProviderId 
        {
            get { return this.providerId; }
            set { this.providerId = value; }
        }

        //public ITranformPlug TransformPlug 
        //{
        //    get { return this.transformPlug; }
        //    set { this.transformPlug = value; }
        //}
    }
}
