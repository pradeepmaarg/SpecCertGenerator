using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Maarg.Fatpipe.LoggingService;

namespace Maarg.Fatpipe.Plug.DataModel
{
    #region Interfaces
    public interface IDocumentFragment
    {
        IPluglet Pluglet { get; }
        string Value { get; set; }
        string Name { get; }

        IList<IDocumentFragment> Children { get; }
        Dictionary<string, string> Attributes { get; }
        IDocumentFragment Parent { get; }

        // Below 3 properties mainly used for X12 type DocumentFragment
        // This was added during contingency handling logic implementation
        int SequenceNumber { get; }
        long StartOffset { get; }
        long EndOffset { get; }

        /// <summary>
        /// Construct xml representation of this fragment
        /// </summary>
        /// <returns></returns>
        XElement ToXml();

        /// <summary>
        /// Construct EDI representation of this fragment
        /// </summary>
        /// <param name="ediDelimiters"></param>
        /// <param name="internalSegment"></param>
        /// <returns></returns>
        string ToEDI(Delimiters ediDelimiters, bool internalSegment);
    }

    public interface IFatpipeDocument
    {
        IDocumentPlug DocumentPlug { get; }
        IDocumentFragment RootFragment { get; }
        IList<string> Errors { get; }
        //TODO: Is string ok? or we need Base64?, do we need PayloadType (Xml, EDI) or we can infer from something?
        string OriginalPayload { get; }
        string BeautifiedOriginalPayloadBody { get; }
        string BeautifiedOriginalPayloadStartHeader { get; }
        string BeautifiedOriginalPayloadEndHeader { get; }

        // Assumption: FatpipeDocument has only 1 transaction set
        int TransactionSetType { get; }

        // ST02
        string TransactionNumber { get; }

        // TODO: Below mentioned form of ToXml and ToString may not be suitable
        // if we want to create one xml/string for entire document

        /// <summary>
        /// Construct xml representation of document (current transaction set
        /// along with header segments)
        /// </summary>
        /// <returns></returns>
        XElement ToXml();

        /// <summary>
        /// Construct EDI representation of document (current transaction set
        /// along with header segments)
        /// </summary>
        /// <param name="ediDelimiters"></param>
        /// <returns></returns>
        string ToEDI(Delimiters ediDelimiters);
    } 
    #endregion

    #region Classes
    /// <summary>
    /// This class represents a set of delimiters. There are 3 types, all of which are required
    /// 1. Field seperator
    /// 2. Component seperator
    /// 3. Segement delimiter
    /// 
    /// Segment delimiter comes in interesting patterns.
    /// It can be one of following forms
    /// 
    /// 1. Single character like '
    /// 2. Single character + suffix - where suffix is CR for readibility
    /// 3. Single character + CR + LF 
    /// 4. CR LF
    /// 5. CR
    /// 
    /// Thus the data structure allows a max of 3 chars for segment delimiter. the first one is required
    /// suffix1 and suffix2 are optional. 
    /// </summary>
    public class Delimiters
    {
        public const int CarriageReturn = 13;
        public const int LineFeed = 10;

        //suffix1 and suffix2 could be absent, meaning value = -1
        public Delimiters(int fieldSeperator, int componentSeperator, int segmentDelimiter,
            int segmentDelimiterSuffix1, int segmentDelimiterSuffix2)
        {
            this.FieldSeperator = fieldSeperator;
            this.ComponentSeperator = componentSeperator;
            this.SegmentDelimiter = segmentDelimiter;
            this.SegmentDelimiterSuffix1 = segmentDelimiterSuffix1;
            this.SegmentDelimiterSuffix2 = segmentDelimiterSuffix2;
        }

        #region Properties
        public int FieldSeperator { get; set; }
        public int ComponentSeperator { get; set; }
        public int SegmentDelimiter { get; set; }
        public int SegmentDelimiterSuffix1 { get; set; }
        public int SegmentDelimiterSuffix2 { get; set; }

        public int SegmentDelimiterLength
        {
            get
            {
                int suffix1 = SegmentDelimiterSuffix1 > 0 ? 1 : 0;
                int suffix2 = SegmentDelimiterSuffix2 > 0 ? 1 : 0;
                return 1 + suffix1 + suffix2;
            }
        }
        #endregion
    }

    public class DocumentFragment : IDocumentFragment
    {
        public IPluglet Pluglet { get; set; }
        public string Value { get; set; }
        public string Name
        {
            get
            {
                return Pluglet == null ? string.Empty : Pluglet.Name;
            }
        }

        public IList<IDocumentFragment> Children { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public IDocumentFragment Parent { get; set; }

        // Below 3 properties mainly used for X12 type DocumentFragment
        // This was added during contingency handling logic implementation
        public int SequenceNumber { get; set; }
        public long StartOffset { get; set; }
        public long EndOffset { get; set; }

        /// <summary>
        /// Construct xml representation of this fragment
        /// </summary>
        /// <returns></returns>
        public XElement ToXml()
        {
            XElement xmlFragment = new XElement(Pluglet.Name);

            if (Pluglet.PlugletType == PlugletType.Data)
                xmlFragment.Value = Value;

            if(Children != null)
                foreach (IDocumentFragment child in Children)
                {
                    // Construct xml element for data segments only if value is present
                    if(child.Pluglet.PlugletType != PlugletType.Data || string.IsNullOrEmpty(child.Value) == false)
                        xmlFragment.Add(child.ToXml());
                }

            if(Attributes != null)
                foreach (string key in Attributes.Keys)
                {
                    xmlFragment.SetAttributeValue(key, Attributes[key]);
                }

            return xmlFragment;
        }

        /// <summary>
        /// Construct EDI representation of this fragment
        /// </summary>
        /// <param name="ediDelimiters"></param>
        /// <param name="internalSegment"></param>
        /// <returns></returns>
        public string ToEDI(Delimiters ediDelimiters, bool internalSegment)
        {
            if (Children == null)
                return string.Empty;

            StringBuilder segment = new StringBuilder();

            string componentDelimiter = ((char)ediDelimiters.ComponentSeperator).ToString();
            string fieldDelimiter = ((char)ediDelimiters.FieldSeperator).ToString();
            string delimiter = internalSegment == true ? componentDelimiter : fieldDelimiter;

            // construct segment only for parent with at least 1 leaf node
            if (Children.Any(c => c.Pluglet.PlugletType == PlugletType.Data))
            {
                if(internalSegment == false)
                    segment.AppendFormat("{0}", Pluglet.Tag);

                foreach (IDocumentFragment child in Children)
                {
                    if (child.Pluglet.PlugletType == PlugletType.Data)
                    {
                        if(segment.Length != 0 || internalSegment == false)
                            segment.AppendFormat("{0}{1}", delimiter, child.Value);
                        else
                            segment.AppendFormat("{0}", child.Value);
                    }
                    else
                        segment.AppendFormat("{0}{1}", fieldDelimiter, child.ToEDI(ediDelimiters, true));
                }
            }
            else
            {
                string childSegment;

                StringBuilder segmentB = new StringBuilder(3);
                segmentB.Append((char)ediDelimiters.SegmentDelimiter);
                if (ediDelimiters.SegmentDelimiterSuffix1 != -1) segmentB.Append((char)ediDelimiters.SegmentDelimiterSuffix1);
                if (ediDelimiters.SegmentDelimiterSuffix2 != -1) segmentB.Append((char)ediDelimiters.SegmentDelimiterSuffix2);
                string segmentDelimiter = segmentB.ToString();


                foreach(IDocumentFragment child in Children)
                {
                    childSegment = child.ToEDI(ediDelimiters, false);
                    segment.AppendFormat("{0}{1}"
                            , string.IsNullOrEmpty(segment.ToString()) ? string.Empty : segmentDelimiter
                            , childSegment);
                }

                IDocumentFragment firstChild = Children[0];
                // Special case for ading SE/GE/IEA segments
                switch (firstChild.Pluglet.Tag)
                {
                    case "ST":
                        string txnSetNumber = "";
                        int segmentCount = 0;
                        if (firstChild.Children != null && firstChild.Children.Count > 1)
                            txnSetNumber = firstChild.Children[1].Value;
                        // Set segmentCount to child count of X12_00401_850
                        segmentCount = this.CountAllChilds() + 1; // +1 for SE segment
                        //segment.AppendFormat("{0}SE{1}{2}{3}{4}", segmentDelimiter, fieldDelimiter, segmentCount, fieldDelimiter, txnSetNumber);

                        //segment.AppendFormat("{0}GE{1}1{2}1", segmentDelimiter, fieldDelimiter, fieldDelimiter);
                        //segment.AppendFormat("{0}IEA{1}1{2}{3}", segmentDelimiter, fieldDelimiter, fieldDelimiter, "NNNN");

                        break;
                }
            }

            return segment.ToString();
        }
    }

    public class FatpipeDocument : IFatpipeDocument
    {
        public IDocumentPlug DocumentPlug { get; set; }
        public IDocumentFragment RootFragment { get; set; }
        public IList<string> Errors { get; set; }
        public string OriginalPayload { get; set; }
        public string BeautifiedOriginalPayloadBody { get; set; }
        public string BeautifiedOriginalPayloadStartHeader { get; set; }
        public string BeautifiedOriginalPayloadEndHeader { get; set; }
        public int TransactionSetType { get; set; }
        public string TransactionNumber { get; set; }

        // TODO: Below mentioned form of ToXml and ToString may not be suitable
        // if we want to create one xml/string for entire document

        /// <summary>
        /// Construct xml representation of document (current transaction set
        /// along with header segments)
        /// </summary>
        /// <returns></returns>
        public XElement ToXml()
        {
            ILogger logger = LoggerFactory.Logger;

            XElement ediXml = RootFragment.ToXml();

            return ediXml;
        }

        /// <summary>
        /// Construct EDI representation of document (current transaction set
        /// along with header segments)
        /// </summary>
        /// <param name="ediDelimiters"></param>
        /// <returns></returns>
        public string ToEDI(Delimiters ediDelimiters)
        {
            ILogger logger = LoggerFactory.Logger;

            string ediDocument = RootFragment.ToEDI(ediDelimiters, false);

            return ediDocument;
        } 
    } 
    #endregion
}
