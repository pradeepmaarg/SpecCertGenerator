using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    // ServiceConfiguration or PlugConfiguration?
    [Serializable]
    public abstract class ServiceConfiguration : UniqueIdentifier
    {
        private string name;
        private string description;

        public string Name 
        {
            get { return this.name; }
            set { this.name = value; }
        }
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }
    }
}
