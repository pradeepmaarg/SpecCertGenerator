using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Maarg.Contracts;

namespace Maarg.AllAboard
{
    public class BizMessageFactory
    {
        const string BizMessageType = "BizMessageType";
        const string BizUniqueId = "BizUniqueId";
        const string BizCreatedTime = "BizCreatedTime";

        //Action
        const string PayloadSection = "PayloadSection";
        const string Description = "Description";
        const string Status = "Status";
        const string CreatedTime = "CreatedTime";
        const string LastUpdateTime = "LastUpdateTime";
        
        const string IndexSeperator = "#";

        public static IBizMessage CreateBizMessage()
        {
            IBizMessage message = new BizMessage();
            return message;
        }

        public static IBizMessage CreateBizMessage(string uniqueId, BizMessageType type, string payload, MessageActionStatus status)
        {
            IBizMessage message = new BizMessage();
            message.Type = type;
            message.UniqueId = uniqueId;
            message.CreatedTime = DateTime.Now;
            message.Payload = payload;
            message.Actions.Add(new BizMessageAction
            {
                Description = "Document level action",
                PayloadSection = "/",
                CreatedTime = DateTime.Now,
                LastUpdateTime = DateTime.Now,
                Status = status,
            });

            return message;
        }

        public static IBizMessage CreateBizMessageFromStream(Stream stream)
        {
            IList<KeyValuePair<string, string>> headerList;
            MemoryStream bodyStream = null;
            try
            {
                bodyStream = StreamHelper.DeserializeStreamToHeadersAndBody(stream, out headerList);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            IBizMessage message = CreateBizMessage();
            message.Payload = Encoding.UTF8.GetString(bodyStream.GetBuffer(), (int)bodyStream.Position, (int)bodyStream.Length - (int)bodyStream.Position);
            int prevIndex = -1;
            int currentIndex;
            BizMessageAction currentAction = new BizMessageAction();

            foreach (KeyValuePair<string, string> keyValuePair in headerList)
            {
                string key = keyValuePair.Key;

                if (string.Compare(key, BizMessageType, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    message.Type = (BizMessageType)Enum.Parse(typeof(BizMessageType), keyValuePair.Value, true);
                }
                else if (string.Compare(key, BizUniqueId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    message.UniqueId = keyValuePair.Value;
                }
                else if (string.Compare(key, BizCreatedTime, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    DateTime createdTime;
                    DateTime.TryParse(keyValuePair.Value, out createdTime);
                    message.CreatedTime = createdTime;
                }

                //now you are reading a list field
                else if ((currentIndex = key.IndexOf(IndexSeperator)) > 0)
                {
                    //read the list till you have the same index
                    string fieldName = key.Substring(0, currentIndex);
                    currentIndex = int.Parse(key.Substring(currentIndex + 1));

                    if (prevIndex != currentIndex)
                    {
                        //create a new action
                        currentAction = new BizMessageAction();
                        message.Actions.Add(currentAction);
                    }

                    if (string.Compare(fieldName, PayloadSection, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentAction.PayloadSection = keyValuePair.Value;
                    }

                    else if (string.Compare(fieldName, Description, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentAction.Description = keyValuePair.Value;
                    }

                    else if (string.Compare(fieldName, Status, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentAction.Status = (MessageActionStatus)Enum.Parse(typeof(MessageActionStatus), keyValuePair.Value, true);
                    }

                    else if (string.Compare(fieldName, CreatedTime, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentAction.CreatedTime = DateTime.Parse(keyValuePair.Value);
                    }

                    else if (string.Compare(fieldName, LastUpdateTime, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentAction.LastUpdateTime = DateTime.Parse(keyValuePair.Value);
                    }


                    prevIndex = currentIndex;
                }
            }

            // old message may not have unique id and created time, so need to assign them with something
            if (string.IsNullOrEmpty(message.UniqueId))
            {
                message.UniqueId = Guid.NewGuid().ToString();
            }
            if (message.CreatedTime == DateTime.MinValue)
            {
                message.CreatedTime = DateTime.Now;
            }

            return message;
        }

        public static Stream WriteBizMessageToStream(IBizMessage message)
        {
            IList<KeyValuePair<string, string>> headerList = new List<KeyValuePair<string, string>>();
            headerList.Add(new KeyValuePair<string, string>(BizMessageType, message.Type.ToString()));
            headerList.Add(new KeyValuePair<string, string>(BizUniqueId, message.UniqueId));
            headerList.Add(new KeyValuePair<string, string>(BizCreatedTime, message.CreatedTime.ToString()));
            
            int index = 0;
            string key;
            string value;
            foreach (BizMessageAction action in message.Actions)
            {
                key = PayloadSection + IndexSeperator + index;
                value = action.PayloadSection;
                headerList.Add(new KeyValuePair<string, string>(key, value));

                key = Description + IndexSeperator + index;
                value = action.Description;
                headerList.Add(new KeyValuePair<string, string>(key, value));

                key = Status + IndexSeperator + index;
                value = action.Status.ToString();
                headerList.Add(new KeyValuePair<string, string>(key, value));

                key = CreatedTime + IndexSeperator + index;
                value = action.CreatedTime.ToString();
                headerList.Add(new KeyValuePair<string, string>(key, value));

                key = LastUpdateTime + IndexSeperator + index;
                value = action.LastUpdateTime.ToString();
                headerList.Add(new KeyValuePair<string, string>(key, value));
                
                index++;
            }

            Stream headerStream = StreamHelper.SerializeHeadersToStream(headerList, true);
            Stream bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(message.Payload));
            return new ConcatenatingStream(headerStream, bodyStream, null);
        }
    }

    public class BizMessage : IBizMessage
    {
        List<BizMessageAction> actions;

        /// <summary>
        /// Message type
        /// </summary>
        public BizMessageType Type { get; set; }

        /// <summary>
        /// Unique id used for identifying and updating the message in storage
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Unique id
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Created time
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Payload string
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// List of actions
        /// </summary>
        public List<BizMessageAction> Actions 
        {
            get { return this.actions; }
        }

        public BizMessage()
        {
            actions = new List<BizMessageAction>();
            Type = BizMessageType.CA277;
            Payload = string.Empty;
        }
    }

}
