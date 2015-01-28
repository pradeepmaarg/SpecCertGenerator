using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.LoggingService
{
    public interface ILogger
    {
        void SetActivityId(Guid id);
        void Debug(string location, string format, params object[] args);
        void Info(string location, string format, params object[] args);
        void Warning(string location, int eventId, string format, params object[] args);
        void Error(string location, int eventId, string format, params object[] args);
    }
}
