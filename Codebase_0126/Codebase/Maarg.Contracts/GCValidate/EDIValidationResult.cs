using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts.GCValidate
{
    public enum ResultType
    {
        Warning,
        Error,
        Success
    }

    public class SegmentValidationResult
    {
        public ResultType Type { get; set; }
        public int SequenceNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long StartIndex { get; set; }
        public long EndIndex { get; set; }
    }

    public class EDIValidationResult
    {
        public string FileName { get; set; }
        public string SchemaName { get; set; }

        public string DisplayName { get; set; }
        public string Type { get; set; }

        public string BeautifiedOriginalPayload { get; set; }

        public TimeSpan ExecutionTime { get; set; }

        public List<SegmentValidationResult> SegmentValidationResults { get; set; }
        public List<string> TransactionNumbers { get; set; }

        public EDIValidationResult()
        {
            ExecutionTime = TimeSpan.FromSeconds(0);
        }

        public bool IsValid
        {
            get { return SegmentValidationResults == null || SegmentValidationResults.Count == 0 || SegmentValidationResults.All(r => r.Type != ResultType.Error); }
        }
    }
}
