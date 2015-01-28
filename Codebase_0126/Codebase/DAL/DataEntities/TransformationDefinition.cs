using System;
using Maarg.Contracts;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class TransformationDefinition : ITransformationDefinition
    {
        private string sourceDocumentType;
        private string destinationDocumentType;
        private Uri transformationContentUri;

        public string SourceDocumentType { get { return this.sourceDocumentType; } set { this.sourceDocumentType = value; } }

        public string DestinationDocumentType { get { return this.destinationDocumentType; } set { this.destinationDocumentType = value; } }

        public Uri TransformationContentUri { get { return this.transformationContentUri; } set { this.transformationContentUri = value; } }
    }
}
