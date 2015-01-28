using System;

namespace Maarg.Contracts
{
    public interface ITransformationDefinition
    {
        string SourceDocumentType { get; set; }
        string DestinationDocumentType { get; set; }

        //MessagingEngine will contain APIs to fetch the actual content which will be used at runtime. May need refactoring later
        Uri TransformationContentUri { get; set; }
    }
}
