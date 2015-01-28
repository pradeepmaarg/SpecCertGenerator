using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Maarg.AllAboard;

namespace Maarg.Contracts
{
    public class NotificationMessageFactory
    {
        const string NotificationMessageType = "BizMessageType";
        const string TemplateId = "TemplateId";
        const string ParameterMap = "ParameterMap";
        const string RoutingInfo = "RoutingInfo";
        const string PartnerId = "partnerId";
        const string TransportType = "TransportType";
        
        
        const string IndexSeperator = "#";

        public static INotificationMessage CreateNotificationMessage()
        {
            INotificationMessage message = new NotificationMessage();
            return message;
        }

        public static INotificationMessage CreateNotificationMessageFromStream(Stream stream)
        {
            INotificationMessage message = CreateNotificationMessage();
            IList<KeyValuePair<string, string>> headerList;
            MemoryStream bodyStream = StreamHelper.DeserializeStreamToHeadersAndBody(stream, out headerList);
            message.Body = bodyStream;
            
            foreach (KeyValuePair<string, string> keyValuePair in headerList)
            {
                string key = keyValuePair.Key;

                if (string.Compare(key, TemplateId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    message.TemplateId = keyValuePair.Value;
                }

                //now you are reading the ParameterMap dictionary field
                else 
                {
                    string value = keyValuePair.Value;
                    message.ParameterMap.Add(key, value);
                }
            }

            return message;
        }

        public static Stream WriteNotificationMessageToStream(INotificationMessage message)
        {
            IList<KeyValuePair<string, string>> headerList = new List<KeyValuePair<string, string>>();

            headerList.Add(new KeyValuePair<string, string>(TemplateId, message.TemplateId));

            string key;
            string value;
            foreach (KeyValuePair<string, string> info in message.ParameterMap)
            {
                key = info.Key;
                value = info.Value;
                headerList.Add(new KeyValuePair<string, string>(key, value));
            }

            Stream headerStream = StreamHelper.SerializeHeadersToStream(headerList, true);
            Stream bodyStream = message.Body;
            return new ConcatenatingStream(headerStream, bodyStream, null);
        }
    }

    public class NotificationMessage : INotificationMessage
    {
        
        /// <summary>
        /// Email Template Id
        /// </summary>
        public string TemplateId { get; set; }

        public IDictionary<string, string> ParameterMap { get; set; }

        public RoutingInfo RoutingInfo { get; set; }

        public Stream Body { get; set; }

        public NotificationMessage()
        {
            ParameterMap = new Dictionary<string, string>();
            TemplateId = string.Empty;
        }
    }

}
