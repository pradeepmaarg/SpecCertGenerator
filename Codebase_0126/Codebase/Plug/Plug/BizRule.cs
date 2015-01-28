using System.Collections.Generic;
using System.Xml.Linq;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public enum BizRuleType
    {
        Mandatory,
        Optional,
        ConditionalMandatory,
    }

    public class SegmentPath
    {
        public string OriginalPath { get; private set; }

        // Optional value - If present then it should match segment instance
        public string Value { get; private set; }
        public string DataSegmentName { get; private set; }
        public List<string> Segments { get; private set; }
        public string SegmentName { get { return Segments[Segments.Count - 1]; } }
        public string Path { get { return string.Join("->", Segments); } }

        public SegmentPath(string originalPath)
        {
            Segments = new List<string>();

            if (string.IsNullOrWhiteSpace(originalPath))
            {
                DataSegmentName = null;
                return;
            }

            this.OriginalPath = originalPath;

            string[] pathParts = originalPath.Split(new string[]{"->"}, System.StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length < 2)
            {
                throw new PlugDataModelException("SegmentPath should have at least 2 parts (SegmentName -> DataSegmentName)");
            }

            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                Segments.Add(pathParts[i].Trim());
            }

            DataSegmentName = pathParts[pathParts.Length - 1].Trim();

            int valueStartIndex = DataSegmentName.IndexOf('[');
            if (valueStartIndex != -1)
            {
                Value = DataSegmentName.Substring(valueStartIndex + 1, DataSegmentName.Length - valueStartIndex - 2);
                DataSegmentName = DataSegmentName.Substring(0, valueStartIndex);
            }
        }

        public XElement ToXml()
        {
            XElement segmentPath = new XElement("SegmentPath");

            if (OriginalPath != null)
                segmentPath.Add(new XAttribute("OriginalPath", OriginalPath));
            if (DataSegmentName != null)
                segmentPath.Add(new XAttribute("DataSegment", DataSegmentName));
            if (Value != null)
                segmentPath.Add(new XAttribute("Value", Value));
            if (Segments.Count > 0)
                segmentPath.Add(new XAttribute("Segments", Path));

            return segmentPath;
        }
    }

    public class BizRule
    {
        public string Name { get; set; }
        public BizRuleType Type { get; set; }
        public string ConditionalGroupName { get; set; }
        public List<SegmentPath> SegmentPaths { get; private set; }

        public BizRule(List<string> bizRuleRow)
        {
            if (bizRuleRow == null || bizRuleRow.Count < 4)
                throw new PlugDataModelException("BizRule initialization failed: Invalid parameter bizRuleRow");

            this.Name = bizRuleRow[0];
            ExtractBizRuleType(bizRuleRow[1]);

            SegmentPaths = new List<SegmentPath>();
            for (int i = 2; i < bizRuleRow.Count; i++)
            {
                SegmentPaths.Add(new SegmentPath(bizRuleRow[i]));
            }
        }

        private void ExtractBizRuleType(string bizRuleTypeStr)
        {
            if (string.IsNullOrWhiteSpace(bizRuleTypeStr))
            {
                throw new PlugDataModelException("Rule Matching Option cannot be empty");
            }

            BizRuleType bizRuleType = BizRuleType.Optional;
            bizRuleTypeStr = bizRuleTypeStr.ToLower();

            if (string.Equals(bizRuleTypeStr, "mandatory"))
                bizRuleType = BizRuleType.Mandatory;
            else
                if (string.Equals(bizRuleTypeStr, "optional"))
                    bizRuleType = BizRuleType.Optional;
                else
                    if (bizRuleTypeStr.StartsWith("conditional mandatory"))
                    {
                        bizRuleType = BizRuleType.ConditionalMandatory;

                        int groupNameIndex = bizRuleTypeStr.IndexOf(" - ");
                        if (groupNameIndex == -1)
                            throw new PlugDataModelException("Missing conditional mandatory group name.");

                        this.ConditionalGroupName = bizRuleTypeStr.Substring(groupNameIndex + 3);
                    }
                    else
                    {
                        throw new PlugDataModelException("Invalid rule type: " + bizRuleTypeStr);
                    }

            this.Type = bizRuleType;
        }

        public XElement ToXml()
        {
            XElement bizRule = new XElement("BizRule"
                , new XAttribute("Name", Name)
                , new XAttribute("Type", Type)
                );
            if (ConditionalGroupName != null)
                bizRule.Add(new XAttribute("ConditionalGroupName", ConditionalGroupName));

            foreach (SegmentPath segmentPath in SegmentPaths)
                bizRule.Add(segmentPath.ToXml());

            return bizRule;
        }
    }
}
