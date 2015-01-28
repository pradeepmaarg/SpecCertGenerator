using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.AllAboard
{
    public class MaargConfiguration
    {
        static MaargConfiguration configuration = new MaargConfiguration();
        IDictionary<string, string> configDictionary;

        public const string FatpipeManagerIdSetting = "FatpipeManagerId";
        public const string FatpipeManagerTenantNameSetting = "FatpipeManagerTenantName";

        public const string FatpipeManagerIncomingQueueName = "incomingqueue";
        public const string FatpipeManagerOutgoingQueueName = "outgoingqueue";
        public const string FatpipeManagerQueueMessageVisibilityTimeoutInSeconds = "QueueMessageVisibilityTimeoutInSeconds";

        public const string FatpipeManagerIncomingBlobContainerNameSuffix = "incomingblobcontainer";
        public const string FatpipeManagerOutgoingBlobContainerNameSuffix = "outgoingblobcontainer";

        public const string OutboundTransportManagerAddress = "outboundaddress";
        public const string OutboundTransportManagerDeQBatchSize = "OutboundTransportManagerDeQBatchSize";
        public const string OutboundTransportManagerNumberOfThreads = "OutboundTransportManagerNumberOfThreads";
        public const string OutboundTransportManagerSleepTimeInMilliseconds = "OutboundTransportManagerSleepTimeInMilliseconds";

        public const string B2BProtocolEngineDeQBatchSize = "B2BProtocolEngineDeQBatchSize";
        public const string B2BProtocolEngineNumberOfThreads = "B2BProtocolEngineNumberOfThreads";
        public const string B2BProtocolEngineSleepTimeInMilliseconds = "B2BProtocolEngineSleepTimeInMilliseconds";

        public const string HttpConnectorEnabled = "HttpConnectorEnabled";
        public const string FtpConnectorEnabled = "FtpConnectorEnabled";
        public const string SmtpConnectorEnabled = "SmtpConnectorEnabled";
        public const string OutboundTransportManagerEnabled = "OutboundTransportManagerEnabled";
        public const string B2BProtocolEngineEnabled = "B2BProtocolEngineEnabled";
        public const string BillingEngineEnabled = "BillingEngineEnabled";
        public const string BillingEngineSleepIntervalInSeconds = "BillingEngineSleepIntervalInSeconds";
        public const string DalManagerType = "DalManagerType";

        public const string FtpConnectorFileRenamePrefix = "FtpConnectorFileRenamePrefix";

        public const string TenantSmtpRelayAccountName = "TenantSmtpRelayAccountName";
        public const string TenantSmtpRelayAccountPassword = "TenantSmtpRelayAccountPassword";
        public const string TenantSmtpRelayHost = "TenantSmtpRelayHost";

        public const string SuspendedMessageTableName = "SuspendedMessageTableName";
        public const string SuspendedMessageBlobContainerName = "SuspendedMessageBlobContainerName";
        public const string NumberOfRetriesBeforeSuspend = "NumberOfRetriesBeforeSuspend";

        public const string TenantContainerName = "Tenant.ContainerName";
        public const string TenantStorageAccountConnectionString = "Tenant.StorageAccount.ConnectionString";
        public const string MaargTemplatesContainer = "Maarg.TemplatesContainer";
        public const string MaargSchemaContainer = "Maarg.SchemasContainer";


        public static readonly string[] ConfigurationKeys = 
        {
            FatpipeManagerIdSetting, 
            FatpipeManagerTenantNameSetting, 
            OutboundTransportManagerAddress,
            OutboundTransportManagerDeQBatchSize, 
            OutboundTransportManagerNumberOfThreads,
            OutboundTransportManagerSleepTimeInMilliseconds,
            FatpipeManagerIncomingQueueName, 
            FatpipeManagerOutgoingQueueName,
            FatpipeManagerQueueMessageVisibilityTimeoutInSeconds,
            FatpipeManagerIncomingBlobContainerNameSuffix,
            FatpipeManagerOutgoingBlobContainerNameSuffix,
            B2BProtocolEngineDeQBatchSize, 
            B2BProtocolEngineNumberOfThreads,
            B2BProtocolEngineSleepTimeInMilliseconds,
            HttpConnectorEnabled,
            SmtpConnectorEnabled,
            OutboundTransportManagerEnabled,
            B2BProtocolEngineEnabled, FtpConnectorEnabled,
            FtpConnectorFileRenamePrefix, 
            TenantSmtpRelayHost, 
            TenantSmtpRelayAccountName, 
            TenantSmtpRelayAccountPassword,
            SuspendedMessageBlobContainerName,
            SuspendedMessageTableName,
            NumberOfRetriesBeforeSuspend,
            TenantContainerName,
            TenantStorageAccountConnectionString,
            MaargTemplatesContainer,
            MaargSchemaContainer,
            DalManagerType,
            BillingEngineEnabled,
            BillingEngineSleepIntervalInSeconds
        };

        public MaargConfiguration()
        {
            configDictionary = new Dictionary<string, string>();
        }

        public static MaargConfiguration Instance
        {
            get { return MaargConfiguration.configuration; }
        }

        public string this[string key]
        {
            get 
            {
                string value;
                bool present = configDictionary.TryGetValue(key, out value);
                if (!present) value = null;
                return value;
            }

            set 
            {
                configDictionary[key] = value;
            }
        }
    }
}
