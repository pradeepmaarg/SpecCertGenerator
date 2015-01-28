using System;
using System.Collections.Generic;
using System.Text;

namespace Maarg.Fatpipe.LoggingService
{
    public class LoggerFactory
    {
        private static readonly ILogger _Logger = new Logger();

        public static ILogger Logger
        {
            get { return LoggerFactory._Logger; }
        }
    }
}
