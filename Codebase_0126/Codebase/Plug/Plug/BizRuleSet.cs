using System.Collections.Generic;
using System.Xml.Linq;

namespace Maarg.Fatpipe.Plug.DataModel
{
    /// <summary>
    /// BizRules key => first message domain segment path (without data segment). 
    /// E.g. if first segment path is HLS->REF->REF02[PK] then key will be HLS->REF
    /// </summary>
    public class BizRuleSet
    {
        public List<int> MessageDomainIds { get; private set; }
        public Dictionary<string, List<BizRule>> BizRules { get; private set; }

        public BizRuleSet()
        {
            MessageDomainIds = new List<int>();
            BizRules = new Dictionary<string, List<BizRule>>();
        }

        public XElement ToXml()
        {
            XElement bizRuleSet = new XElement("BizRuleSet"
                , new XAttribute("MessageDomainIds", string.Join("-", MessageDomainIds))
                );

            foreach(string segmentPathKey in BizRules.Keys)
            {
                XElement segmentPath = new XElement("SegmentPath"
                    , new XAttribute("Key", segmentPathKey)
                    );

                foreach(BizRule bizRule in BizRules[segmentPathKey])
                    segmentPath.Add(bizRule.ToXml());

                bizRuleSet.Add(segmentPath);
            }

            return bizRuleSet;
        }
    }
}
