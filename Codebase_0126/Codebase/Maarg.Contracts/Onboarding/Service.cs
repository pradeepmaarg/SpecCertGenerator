using System;

namespace Maarg.Contracts
{
    public enum ServiceStatus
    {
        Beta,
        Live,
        Discontinued,
    }

    public enum ConfigurationType
    {
        ClaimAck,
        DocumentProcessor,
    }

    [Serializable]
    public class Service
    {
        private string name;
        private string description;
        private ServiceStatus status;
        private DateTime availableDate; // Should we use this date or status?
        private ConfigurationType configurationType;

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

        public ServiceStatus Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

        public DateTime AvailableDate
        {
            get { return this.availableDate; }
            set { this.availableDate = value; }
        }

        public ConfigurationType ConfigurationType
        {
            get { return this.configurationType; }
            set { this.configurationType = value; }
        }
    }
}
