using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Maarg.Fatpipe.Plug.Authoring
{
    [Serializable()]
    public class SchemaReaderException : Exception, ISerializable
    {
        public SchemaReaderException() : base() { }
        public SchemaReaderException(string message) : base(message) { }
        public SchemaReaderException(string message, System.Exception inner) : base(message, inner) { }
        public SchemaReaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
