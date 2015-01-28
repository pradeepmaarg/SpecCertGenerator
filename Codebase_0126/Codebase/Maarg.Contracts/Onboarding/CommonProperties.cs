using System;

namespace Maarg.Contracts
{
    /// <summary>
    /// This class serve as a common identity for other entities such as Tenant, Partner,
    /// ServiceConfiguratione etc.
    /// </summary>
    [Serializable]
    public class CommonProperties
    {
        private string createdBy;
        private string lastModifiedBy;
        private DateTime createdDate;
        private DateTime lastModifiedDate;

        public string CreatedBy
        {
            get { return this.createdBy; }
            set { this.createdBy = value; }
        }

        public string LastModifiedBy
        {
            get { return this.lastModifiedBy; }
            set { this.lastModifiedBy = value; }
        }

        public DateTime CreatedDate
        {
            get { return this.createdDate; }
            set { this.createdDate = value; }
        }

        public DateTime LastModifiedDate
        {
            get { return this.lastModifiedDate; }
            set { this.lastModifiedDate = value; }
        }
    }
}
