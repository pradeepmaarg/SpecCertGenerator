using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using System.Runtime.Serialization;

namespace Maarg.Fatpipe.EDIPlug
{
    public class EDIReaderException : Exception
    {
        public EDIReaderException()    { }
        public EDIReaderException(string message) : base(message) { }
        public EDIReaderException(string message, Exception inner) : base(message, inner) { }

        protected EDIReaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
