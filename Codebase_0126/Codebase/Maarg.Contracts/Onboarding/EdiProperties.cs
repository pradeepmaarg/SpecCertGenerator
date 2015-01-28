using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    [Serializable]
    public class EdiProperties
    {
        private string name;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
    }
}
