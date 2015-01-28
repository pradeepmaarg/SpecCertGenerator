using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts.GCValidate
{
    public class BizRuleInfo
    {
        public string FileName { get; set; }
        public string SegmentPath { get; set; }
        public string Value { get; set; }
    }

    public class BizRuleValidationResult
    {
        public string RuleName { get; set; }
        public ResultType Type { get; set; }
        public List<BizRuleInfo> RuleInfo {get; set; }
    }

    public class BizRulesValidationResult
    {
        public string BizRuleCertName { get; set; }
        public TimeSpan BizRuleValidationExecutionTime { get; set; }

        public List<EDIValidationResult> EdiValidationResults { get; set; }
        public List<BizRuleValidationResult> BizRuleValidationResults { get; set; }

        public BizRulesValidationResult()
        {
            BizRuleValidationExecutionTime = TimeSpan.FromSeconds(0);
        }

        public ResultType GetAggregatedResult()
        {
            ResultType resultType = ResultType.Success;
            if (BizRuleValidationResults != null)
            {
                foreach (BizRuleValidationResult result in BizRuleValidationResults)
                {
                    if (result.Type == ResultType.Error)
                    {
                        resultType = ResultType.Error;
                        break;
                    }
                    else
                        if (result.Type == ResultType.Warning)
                        {
                            resultType = ResultType.Warning;
                        }
                }
            }

            return resultType;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("BizRuleCertName: " + BizRuleCertName);

            result.AppendLine("EDIValidationResult: ");
            foreach (EDIValidationResult ediValidationResult in EdiValidationResults)
            {
                result.AppendFormat("\t{0} - IsValid: {1}, ResultCount:{2}{3}",
                    ediValidationResult.DisplayName, ediValidationResult.IsValid, ediValidationResult.SegmentValidationResults.Count, Environment.NewLine);
            }

            result.AppendLine("BizRuleValidationResult: ");
            foreach (BizRuleValidationResult bizRuleValidationResult in BizRuleValidationResults)
            {
                result.AppendFormat("\t{0} - IsValid: {1}{2}", bizRuleValidationResult.RuleName, bizRuleValidationResult.Type, Environment.NewLine);
                foreach (BizRuleInfo ruleInfo in bizRuleValidationResult.RuleInfo)
                    result.AppendFormat("\t\t{0} - {1} - {2}{3}", ruleInfo.FileName, ruleInfo.SegmentPath, ruleInfo.Value, Environment.NewLine);
            }

            return result.ToString();
        }
    }
}
