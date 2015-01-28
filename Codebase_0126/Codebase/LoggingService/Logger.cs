using System;
using System.Diagnostics;

namespace Maarg.Fatpipe.LoggingService
{
    public class Logger : ILogger
    {
        private static TraceSource traceSource = new TraceSource("MaargLogSource");

        public void SetActivityId(Guid id)
        {
            Trace.CorrelationManager.ActivityId = id;
        }

        public void Debug(string location, string format, params object[] args)
        {
            traceSource.TraceEvent(TraceEventType.Verbose, 1, GetLogEntry(location, format, args));
        }

        public void Info(string location, string format, params object[] args)
        {
            traceSource.TraceEvent(TraceEventType.Information, 2, GetLogEntry(location, format, args));
        }

        public void Warning(string location, int eventId, string format, params object[] args)
        {
            traceSource.TraceEvent(TraceEventType.Warning, eventId, GetLogEntry(location, format, args));
        }

        public void Error(string location, int eventId, string format, params object[] args)
        {
            traceSource.TraceEvent(TraceEventType.Warning, eventId, GetLogEntry(location, format, args));
        }

        private string GetLogEntry(string location, string format, params object[] args)
        {
            return string.Format("{0} - {1} - {2}", Trace.CorrelationManager.ActivityId, location, string.Format(format, args));
        }
    }
}
