using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    [Serializable]
    public class EDIIdentity
    {
        private string isaId;

        public string IsaId
        {
            get { return this.isaId; }
            set { this.isaId = value; }
        }
    }
}
