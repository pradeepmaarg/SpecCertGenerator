using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.LoggingService
{
    public static class EventId
    {
        public const int DALGetPartnerByLoginName = 10001;
        public const int DALListItems = 10002;
        public const int DALGetEntity = 10003;
        public const int DALSaveEntity = 10004;
        public const int DALDeleteEntity = 10005;
        public const int DALEntity = 10006;
        public const int DALGetManager = 10007;

        public const int BizpipeMissingConfigValue = 10101;
        public const int BizpipeCtor = 10102;
        public const int BizpipeEnqueueMessage = 10103;
        public const int BizpipeDequeueMessage = 10104;
        public const int BizpipeRemoveMessage = 10105;
        public const int BizpipeStrayMessage = 10106;
        public const int BizpipeSaveSuspendMessage = 10107;
        public const int BizpipeGetSuspendMessage = 10108;
        public const int BizpipeDeleteSuspendMessage = 10109;
        public const int BizpipeListSuspendMessages = 10110;
        public const int BizpipeRemoveFromQueue = 10111;
        public const int BizpipeSuspendMessage = 10112;
        public const int BizpipeNoCorrelationIdExist = 10113;

        public const int EDIReaderInvalidDocument = 10201;
        public const int EDIReaderInvalidSegment = 10202;
        public const int EDIReaderUnknownSegment = 10203;
        public const int EDIReaderDocFragmentCreation = 10204;
        public const int EDIReaderMissingMandatorySegment = 10205;
        public const int EDIReaderInvalidTransactionSetType = 10206;

        public const int DocTransformerNoMapping = 10301;

        public const int HttpConnectorSaveMessage = 10401;
        public const int HttpConnectorProcessRequest = 10402;

        public const int HttpSenderSendMessage = 10501;

        public const int OutboundTransportManagerProcessMessage = 10601;

        public const int XmlReaderUnhandledException = 10701;
    }
}
