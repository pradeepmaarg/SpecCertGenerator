using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts.GCValidate
{
    public class SegmentDefinitionValidationResult
    {
        public ResultType Type { get; set; }
        //public string SegmentName { get; set; }
        public string Description { get; set; }
        public long RowIndex { get; set; }
        public string ColumnIndex { get; set; }
    }

    public class SpecCertValidationResult
    {
        public SpecCertValidationResult()
        {
            SegmentDefinitionValidationResults = new List<SegmentDefinitionValidationResult>();
        }

        public List<SegmentDefinitionValidationResult> SegmentDefinitionValidationResults { get; set; }

        public bool IsValid
        {
            get { return SegmentDefinitionValidationResults == null || SegmentDefinitionValidationResults.Count == 0 || SegmentDefinitionValidationResults.All(r => r.Type != ResultType.Error); }
        }
    }
}
