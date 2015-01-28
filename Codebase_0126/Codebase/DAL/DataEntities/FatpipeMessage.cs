using System;
using System.IO;
using Maarg.Contracts;
using System.Runtime.Serialization.Formatters.Binary;

namespace Maarg.AllAboard.DataEntities
{
    [Serializable]
    public class FatpipeMessage : IFatpipeMessage
    {
        private MessageHeader header;
        private MessageBody body;
        private MessageStatus status;

        public MessageHeader Header
        {
            get
            {
                return this.header;
            }
            set
            {
                this.header = value;
            }
        }

        public MessageBody Body
        {
            get
            {
                return this.body;
            }
            set
            {
                this.body = value;
            }
        }

        public MessageStatus Status
        {
            get
            {
                return this.status;
            }
            set
            {
                this.status = value;
            }
        }

        public IFatpipeMessage Clone(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream) as FatpipeMessage;
        }
    }
}
