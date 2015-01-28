using System;
using System.Collections.Generic;
using System.IO;
using Maarg.Contracts;

namespace Maarg.AllAboard
{
    public class BizpipeMesssageStreaming
    {
        public const string MsgId = "Id";
        public const string TenantId = "TenantId";
        public const string QueueName = "QueueName";
        public const string CorrelationId = "CorrelationID";
        public const string ContentType = "ContentType";
        public const string ErrorDescription = "ErrorDescription";
        public const string ProcessingResult = "ProcessingResult";


        /// <summary>
        /// Has the logic to convert a FatpipeMessage into a Stream object that represents the storage logic
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static Stream WriteBizpipeMessageToStream(IFatpipeMessage message)
        {
            IList<KeyValuePair<string, string>> headerList = new List<KeyValuePair<string, string>>();
            headerList.Add(new KeyValuePair<string, string>(MsgId, message.Header.Identifier));
            headerList.Add(new KeyValuePair<string, string>(TenantId, message.Header.TenantIdentifier));
            headerList.Add(new KeyValuePair<string, string>(CorrelationId, message.Header.CorrelationId));

            if (message.Header.Context != null)
            {
                foreach (KeyValuePair<string, string> kvpair in message.Header.Context)
                {
                    headerList.Add(new KeyValuePair<string, string>(kvpair.Key, kvpair.Value));

                }
            }
            
            Stream headerStream = StreamHelper.SerializeHeadersToStream(headerList, true);
            Stream bodyStream = message.Body.Body;
            return new ConcatenatingStream(headerStream, bodyStream, null);
        }

        public static IFatpipeMessage CreateBizpipeMessageFromStream(Stream stream, IFatpipeManager manager)
        {
            IFatpipeMessage message = manager.CreateNewMessage();
            IList<KeyValuePair<string, string>> headerList;
            message.Body.Body = StreamHelper.DeserializeStreamToHeadersAndBody(stream, out headerList);
            MessageHeader header = new MessageHeader(string.Empty, string.Empty);
            message.Header = header;
            IDictionary<string, string> context = header.Context;

            foreach (KeyValuePair<string, string> keyValuePair in headerList)
            {
                string key = keyValuePair.Key;

                if (string.Compare(key, MsgId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                   header.Identifier = keyValuePair.Value;
                }

                else if (string.Compare(key, TenantId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    header.TenantIdentifier = keyValuePair.Value;
                }

                else if (string.Compare(key, CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    header.CorrelationId = keyValuePair.Value;
                }

                else
                {
                    context[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return message;
        }
         
    }
}
