using System;
using System.Runtime.Serialization;

namespace SpecCertGenerator
{
    class SpecCertGeneratorException : Exception
    {
        public SpecCertGeneratorException() { }
        public SpecCertGeneratorException(string message) : base(message) { }
        public SpecCertGeneratorException(string message, Exception inner) : base(message, inner) { }

        protected SpecCertGeneratorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
