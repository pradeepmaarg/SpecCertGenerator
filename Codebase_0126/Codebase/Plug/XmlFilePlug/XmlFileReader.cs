using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Maarg.Fatpipe.LoggingService;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml;

namespace Maarg.Fatpipe.XmlFilePlug
{
    public class XmlFileReader : IXmlFileReader
    {
        private static readonly ILogger Logger = LoggerFactory.Logger;
        private InterchangeErrors errors;

        public InterchangeErrors Errors { get {return errors; } }
        
        private FatpipeDocument FatpipeDocumentInst { get; set; }

        private int CurrentElementNumber;
        private int TotalPayloadLength;
        private int CurrentLinePayloadStart;
        private int CurrentLinePayloadEnd;
        private int CurrentLevel;
        private string BeautifiedOriginalPayload;

        /// <summary>
        /// Read xml file and construct IFatpipeDocument.
        /// Xml file reader will traverse Xml files and for each element
        /// match it with current pluglet. If match fails then it tries to 
        /// find matching pluglet (similar to X12).
        /// </summary>
        /// <returns></returns>
        public IFatpipeDocument ReadFile(Stream xmlFileStream, IDocumentPlug documentPlug)
        {
            if (xmlFileStream == null)
                throw new ArgumentNullException("xmlFileStream", "Xml file stream cannot be null");

            if (documentPlug == null)
                throw new ArgumentNullException("documentPlug", "Document plug cannot be null");

            string location = "XmlFileReader.ReadFile";
            Logger.Debug(location, "Start");

            BeautifiedOriginalPayload = string.Empty;
            CurrentElementNumber = 0;
            TotalPayloadLength = 0;
            CurrentLinePayloadStart = 0;
            CurrentLinePayloadEnd = 0;
            CurrentLevel = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            errors = new InterchangeErrors();

            // Since xml file doesn't have concept of ST/SE, ans we want to use InterchangeErrors for reporting purpose
            // create dummy transaction set details
            Errors.AddTransactionSetDetails(1, "", "", true);

            IPluglet currentPluglet = documentPlug.RootPluglet;
            currentPluglet.ResetCurrentOccurances();
            currentPluglet.InitializeStartSegmentList();

            FatpipeDocumentInst = new FatpipeDocument();
            FatpipeDocumentInst.DocumentPlug = documentPlug;
            FatpipeDocumentInst.RootFragment = currentPluglet.ConstructDocumentFragment(null, null);

            IDocumentFragment currentDocumentFragment = FatpipeDocumentInst.RootFragment;
            IDocumentFragment newDocumentFragment = null;

            bool isLeafNode = false;

            try
            {
                XmlTextReader xmlReader = new XmlTextReader(xmlFileStream);

                // If some element doesn't match document plutlet then stop
                // TODO: Should we try to match other elements? If yes, which pluglet to start with? 
                // Also we need to ignore this entire element
                bool stopProcessing = false;

                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            isLeafNode = false;
                            AddStartElementToPayload(xmlReader.Name, xmlReader.IsEmptyElement);

                            if (xmlReader.IsEmptyElement)
                                break;

                            if (stopProcessing == false)
                            {
                                currentDocumentFragment = ConstructNewDocumentFragment(xmlReader.Name, currentPluglet, currentDocumentFragment);
                            }

                            // If some element doesn't match document plutlet then stop
                            // TODO: Should we try to match other elements? If yes, which pluglet to start with? 
                            // Also we need to ignore this entire element
                            if (currentDocumentFragment == null)
                            {
                                stopProcessing = true;
                            }
                            else
                                currentPluglet = currentDocumentFragment.Pluglet;

                            CurrentLevel++;
                            CurrentElementNumber++;
                            break;

                        case XmlNodeType.Text:
                            isLeafNode = true;
                            AddValueToPayload(xmlReader.Value);
                            if (stopProcessing == false)
                                currentDocumentFragment.Value = xmlReader.Value;
                            // Assumption: Leaf nodes are on same line and non-leaf nodes are 1-per line (separate line for start and end element)
                            // If this assumption is wrong then we can construct the xml in string format to match fulfill above assumption
                            // and then Ux can use this string version of xml to highlight the errors.
                            CurrentElementNumber--; // Decrement since leaf level elements are one line, so we need to increment element on endElement only.
                            break;

                        case XmlNodeType.EndElement:
                            CurrentLevel--;
                            AddEndElementToPayload(xmlReader.Name, isLeafNode);
                            if (stopProcessing == false)
                            {
                                // Check if all mandatory segments were present
                                CheckMissingMandatoryElements(currentPluglet, currentDocumentFragment);
                                currentDocumentFragment = currentDocumentFragment.Parent;
                                currentPluglet = currentPluglet.Parent;
                            }

                            CurrentElementNumber++;
                            isLeafNode = false;
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (XmlException xmlException)
            {
                // TODO: Pass start and end postition
                Errors.AddGenericError(currentPluglet == null ? "N/A" : currentPluglet.Name, X12ErrorCode.UnexpectedSegmentCode, 
                        string.Format("Error parsing XML document: {0}", xmlException.Message),
                        CurrentElementNumber/2, CurrentLinePayloadStart+TotalPayloadLength, CurrentLinePayloadEnd+TotalPayloadLength);
            }
            catch (Exception exception)
            {
                // TODO: Pass start and end postition (for all Errors.Add* calls) in this file.
                Errors.AddGenericError(currentPluglet == null ? "N/A" : currentPluglet.Name, X12ErrorCode.UnexpectedSegmentCode,
                        "Internal error occurred, please contact Maarg",
                        CurrentElementNumber / 2, CurrentLinePayloadStart + TotalPayloadLength, CurrentLinePayloadEnd + TotalPayloadLength);
                Logger.Error(location, EventId.XmlReaderUnhandledException, "Error occured during xml file processing: {0}", exception.ToString());
            }

            FatpipeDocumentInst.BeautifiedOriginalPayloadBody = BeautifiedOriginalPayload;

            sw.Stop();
            Logger.Debug(location, "Stop - Elapsed time {0} ms", sw.ElapsedMilliseconds);

            return FatpipeDocumentInst;
        }

        private void AddStartElementToPayload(string elementName, bool isEmptyElement)
        {
            AddElementToPayload(elementName, isEmptyElement, false);
        }

        private void AddValueToPayload(string elementValue)
        {
            BeautifiedOriginalPayload = string.Format("{0}{1}", BeautifiedOriginalPayload, elementValue);
            CurrentLinePayloadStart = CurrentLinePayloadEnd;
            CurrentLinePayloadEnd = CurrentLinePayloadStart + elementValue == null ? 0 : elementValue.Length;
        }

        private void AddEndElementToPayload(string elementName, bool isLeafNode)
        {
            if (isLeafNode)
            {
                BeautifiedOriginalPayload = string.Format("{0}</{1}>", BeautifiedOriginalPayload, elementName);
                CurrentLinePayloadStart = CurrentLinePayloadEnd;
                CurrentLinePayloadEnd = CurrentLinePayloadStart + 3 + elementName.Length - 1;
            }
            else
            {
                AddElementToPayload(elementName, false, true);
            }
        }

        private void AddElementToPayload(string elementName, bool isEmptyElement, bool isEndElement)
        {
            TotalPayloadLength = BeautifiedOriginalPayload.Length + Environment.NewLine.Length;

            string elementPayload;
            if (isEmptyElement)
                elementPayload = string.Format("<{0}/ >", elementName);
            else if (isEndElement)
                elementPayload = string.Format("</{0}>", elementName);
            else
                elementPayload = string.Format("<{0}>", elementName);

            BeautifiedOriginalPayload = string.Format("{0}{1}{2}{3}", BeautifiedOriginalPayload, Environment.NewLine, new String(' ', CurrentLevel * 4), elementPayload);
            CurrentLinePayloadStart = CurrentLevel * 4;
            CurrentLinePayloadEnd = CurrentLinePayloadStart + elementPayload.Length - 1;
        }

        private void CheckMissingMandatoryElements(IPluglet currentPluglet, IDocumentFragment currentDocumentFragment)
        {
            foreach (IPluglet childPluglet in currentPluglet.Children)
            {
                if (childPluglet.IsMandatory == false)
                    continue;

                bool childExist = false;
                foreach (IDocumentFragment childDocumentFragment in currentDocumentFragment.Children)
                {
                    if (childDocumentFragment.Pluglet.Tag == childPluglet.Tag)
                    {
                        childExist = true;
                        break;
                    }
                }

                EdiErrorType errorType = EdiErrorType.Error;
                if (childPluglet.IsIgnore)
                    errorType = EdiErrorType.Warning;

                if (childExist == false)
                {
                    Errors.AddSegmentError(childPluglet.Tag, X12ErrorCode.MandatorySegmentMissingCode
                        , string.Format("{0} : {1}", X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.MandatorySegmentMissingCode), childPluglet.Tag)
                        , CurrentElementNumber, CurrentLinePayloadStart+TotalPayloadLength, CurrentLinePayloadEnd+TotalPayloadLength, errorType);
                }
            }
        }

        private IDocumentFragment ConstructNewDocumentFragment(string elementName, IPluglet currentPluglet, IDocumentFragment currentDocumentFragment)
        {
            IDocumentFragment newDocumentPluglet = null;

            IPluglet nextPluglet = null;

            // Special case for root pluglet
            if (CurrentElementNumber == 0)
            {
                nextPluglet = currentPluglet;

                string rootNodeName = elementName;
                int pos = elementName.IndexOf(":");
                if(pos != -1)
                    rootNodeName = elementName.Substring(pos+1);

                if (string.Equals(nextPluglet.Tag, rootNodeName, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    Errors.AddSegmentError(elementName, -1,
                        string.Format("Invalid root node name. Expected: {0}, Actual {1}", nextPluglet.Tag, rootNodeName), CurrentElementNumber,
                        CurrentLinePayloadStart+TotalPayloadLength, CurrentLinePayloadEnd+TotalPayloadLength, EdiErrorType.Error);
                }
            }
            else
            {

                foreach (IPluglet childPluglet in currentPluglet.Children)
                {
                    if (string.Equals(childPluglet.Tag, elementName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        nextPluglet = childPluglet;
                        break;
                    }
                }
            }

            if (nextPluglet == null)
            {
                Errors.AddSegmentError(elementName, X12ErrorCode.UnrecognizedSegmentIDCode,
                    X12ErrorCode.GetStandardSegmentErrorDescription(X12ErrorCode.UnrecognizedSegmentIDCode), CurrentElementNumber,
                    CurrentLinePayloadStart+TotalPayloadLength, CurrentLinePayloadEnd+TotalPayloadLength, EdiErrorType.Error);
                // TODO: Should we add 'Unrecognized segment' pluglet here?
            }
            else
            {
                newDocumentPluglet = new DocumentFragment()
                    {
                        Parent = currentDocumentFragment,
                        Pluglet = nextPluglet,
                        Value = elementName,
                    };

                if (currentDocumentFragment.Children == null)
                    ((DocumentFragment)currentDocumentFragment).Children = new List<IDocumentFragment>();

                currentDocumentFragment.Children.Add(newDocumentPluglet);
            }

            return newDocumentPluglet;
        }
    }
}