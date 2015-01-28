using System;
using System.IO;

namespace Maarg.Contracts
{
    [Serializable]
    public class MessageBody
    {
        private Stream body;

        public Stream Body { get { return this.body; } set { this.body = value; } }
    }
}
