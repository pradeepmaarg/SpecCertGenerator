using System;
using System.Collections.Generic;

namespace Maarg.Contracts
{
    [Serializable]
    public class AvailableService
    {
        private List<Service> services;

        public List<Service> Services 
        {
            get { return this.services; }
            set { this.services = value; }
        }
    }
}
