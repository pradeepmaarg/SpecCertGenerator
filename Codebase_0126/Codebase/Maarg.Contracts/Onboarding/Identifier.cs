using System;

namespace Maarg.Contracts
{
    /// <summary>
    /// This class serve as a common identity for other entities such as Tenant, Partner,
    /// ServiceConfiguratione etc.
    /// </summary>
    [Serializable]
    public abstract class UniqueIdentifier : IIdentifier
    {
        private string identifier;
    
        public string Identifier
        {
            get { return this.identifier; }
            set { this.identifier = value; }
        }
    }
}
