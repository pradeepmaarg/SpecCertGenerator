using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.FatpipeAPI
{
    public class SuspendedMessageEntity : TableServiceEntity
    {
        public SuspendedMessageEntity()
        {
        }

        public SuspendedMessageEntity(string tenantIdentifier, string messageIdentifier)
            : base(tenantIdentifier, messageIdentifier)
        {
            this.SuspendedMessageBlobReference = messageIdentifier;
        }

        public string PartnerIdentifier { get; set; }
        public string SuspendedMessageBlobReference { get; set; }
        public string ErrorMessage { get; set; }
    }
}