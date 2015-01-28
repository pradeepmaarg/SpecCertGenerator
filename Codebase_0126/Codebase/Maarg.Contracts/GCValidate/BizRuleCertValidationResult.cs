using System.Collections.Generic;
using System.Linq;

namespace Maarg.Contracts.GCValidate
{
    public class RuleDefinitionValidationResult
    {
        public ResultType Type { get; set; }
        public string Description { get; set; }
        public long RowIndex { get; set; }
        public string ColumnIndex { get; set; }
    }

    public class BizRuleCertValidationResult
    {
        public BizRuleCertValidationResult()
        {
            RuleDefinitionValidationResults = new List<RuleDefinitionValidationResult>();
        }

        public List<RuleDefinitionValidationResult> RuleDefinitionValidationResults { get; set; }

        public bool IsValid
        {
            get { return RuleDefinitionValidationResults == null || RuleDefinitionValidationResults.Count == 0 || RuleDefinitionValidationResults.All(r => r.Type != ResultType.Error); }
        }
    }
}
