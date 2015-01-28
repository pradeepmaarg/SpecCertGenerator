using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using System.Runtime.Serialization;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class PlugDataModelException : Exception
    {
        public PlugDataModelException()    { }
        public PlugDataModelException(string message) : base(message) { }
        public PlugDataModelException(string message, Exception inner) : base(message, inner) { }

        protected PlugDataModelException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
