using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public enum DocumentType
    {
        EDI_850,
        EDI_937,
    }

    [Serializable]
    public class DocumentProcessorServiceConfiguration : ServiceConfiguration
    {
        private TransportProtocol transport;
        private DocumentType type;
        private EdiProperties ediProperties;
        private AS2Properties as2Properties;

        private List<string> ediInstanceUrls; // Url to Blob
        private string latestPlugUrl; // Url to Blob
        private List<string> transformationUrls; // Url to Blob

        public TransportProtocol Transport
        {
            get { return this.transport; }
            set { this.transport = value; }
        }
        
        public DocumentType Type 
        {
            get { return this.type; }
            set { this.type = value; }
        }

        public EdiProperties EdiProperties
        {
            get { return this.ediProperties; }
            set { this.ediProperties = value; }
        }

        public AS2Properties AS2Properties
        {
            get { return this.as2Properties; }
            set { this.as2Properties = value; }
        }

        public List<string> EDIInstanceUrls
        {
            get { return this.ediInstanceUrls; }
            set { this.ediInstanceUrls = value; }
        }

        public string LatestPlugUrl
        {
            get { return this.latestPlugUrl; }
            set { this.latestPlugUrl = value; }
        }

        public List<string> TransformationUrls
        {
            get { return this.transformationUrls; }
            set { this.transformationUrls = value; }
        }

        [NonSerialized]
        // TODO: List<string> or List<Stream>
        private List<string> ediInstances;
        [NonSerialized]
        private List<TransformationInfo> transformations;

        public List<string> EDIInstances
        {
            get { return this.ediInstances; }
            set { this.ediInstances = value; }
        }

        // TODO: Uncomment the following once project dependency sorted out
        /*
        [NonSerialized]
        private IDocumentPlug latestPlug;
        public IDocumentPlug LatestPlug 
        {
            get { return this.latestPlug; }
            set { this.latestPlug = value; }
        }
*/
        public List<TransformationInfo> Transformations
        {
            get { return this.transformations; }
            set { this.transformations = value; }
        }
    }
}
