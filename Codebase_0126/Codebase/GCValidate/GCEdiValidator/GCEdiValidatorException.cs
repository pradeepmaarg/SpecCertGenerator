using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using System.Runtime.Serialization;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    public class GCEdiValidatorException : Exception
    {
        public GCEdiValidatorException()    { }
        public GCEdiValidatorException(string message) : base(message) { }
        public GCEdiValidatorException(string message, Exception inner) : base(message, inner) { }

        protected GCEdiValidatorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
