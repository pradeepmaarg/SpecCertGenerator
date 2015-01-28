using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Fatpipe.Plug.DataModel;
using System.Xml.Linq;
using System.Runtime.Serialization;
using Maarg.Fatpipe.LoggingService;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public static class PlugletExtensions
    {
        /// <summary>
        /// Traverse pluglet (schema) tree and construct start segment tree
        /// for each intermediate node
        /// Start segment can be any segment till first mandatory segment in childrens.
        /// If child itself has children the start segment list will contain childrens start 
        /// segment list too.
        /// </summary>
        /// <param name="pluglet"></param>
        public static void InitializeStartSegmentList(this IPluglet pluglet)
        {
            if (pluglet == null)
                throw new ArgumentNullException("pluglet");

            if (pluglet.PlugletType != PlugletType.Loop || pluglet.Children == null)
                return;

            bool mandatorySegmentFound = false;

            pluglet.StartSegmentList = new List<IPluglet>();

            foreach (IPluglet child in pluglet.Children)
            {
                if (child.PlugletType == PlugletType.Loop)
                {
                    child.InitializeStartSegmentList();

                    if (mandatorySegmentFound == false)
                        pluglet.StartSegmentList.AddRange(child.StartSegmentList);
                }
                else if (child.PlugletType == PlugletType.Segment && mandatorySegmentFound == false)
                {
                    pluglet.StartSegmentList.Add(child);
                }

                mandatorySegmentFound = mandatorySegmentFound || child.IsMandatory;
            }

            StringBuilder segmentList = new StringBuilder();
            pluglet.StartSegmentList.ForEach(s => segmentList.Append(s.Tag+" "));
            LoggerFactory.Logger.Debug("PlugletExtensions.InitializeStartSegmentList", "Start segment list ({0}): {1}"
                          , pluglet.Name, segmentList.ToString());
        }

        /// <summary>
        /// Get the sibling of current pluglet
        /// </summary>
        /// <param name="currentPluglet"></param>
        public static IPluglet GetSibling(this IPluglet currentPluglet)
        {
            if (currentPluglet == null)
                throw new ArgumentNullException("currentPluglet");

            IPluglet nextPluglet = currentPluglet.Parent;

            if (nextPluglet != null)
            {
                // Get next child of the parent of currentPluglet
                int i = 0;
                for (i = 0; i < nextPluglet.Children.Count; i++)
                {
                    if (nextPluglet.Children[i] == currentPluglet)
                        break;
                }

                // if currentPluglet is not the last child of it's parent, return next child
                if (i < nextPluglet.Children.Count - 1)
                {
                    nextPluglet = nextPluglet.Children[i + 1];
                }
                else
                {
                    nextPluglet = null;
                }
            }

            return nextPluglet;
        }

        /// <summary>
        /// Get the next pluglet in pre-order traversal from current pluglet
        /// </summary>
        /// <param name="currentPluglet"></param>
        public static IPluglet GetNextPluglet(this IPluglet currentPluglet)
        {
            if (currentPluglet == null)
                throw new ArgumentNullException("currentPluglet");

            bool foundNextNode = false;
            IPluglet nextPluglet = currentPluglet.Parent;

            while (nextPluglet != null && foundNextNode == false)
            {
                // Get next child of the parent of currentPluglet
                int i = 0;
                for (i = 0; i < nextPluglet.Children.Count; i++)
                {
                    if (nextPluglet.Children[i] == currentPluglet)
                        break;
                }

                // if currentPluglet is not the last child of it's parent, return next child
                if (i < nextPluglet.Children.Count - 1)
                {
                    nextPluglet = nextPluglet.Children[i + 1];
                    foundNextNode = true;
                }
                else
                {
                    currentPluglet = nextPluglet;
                    nextPluglet = nextPluglet.Parent;
                    //foundNextNode = true;
                }
            }

            return nextPluglet;
        }

        /// <summary>
        /// Search for a given segment in subtree of currentPluglet
        /// </summary>
        /// <param name="currentPluglet"></param>
        /// <param name="segmentName"></param>
        /// <param name="missingMandatorySegments"></param>
        /// <returns></returns>
        public static IPluglet FindInSubTree(this IPluglet currentPluglet, string segmentName, string[] segmentDetails, bool ignoreOccurrenceCheck, out string missingMandatorySegments)
        {
            missingMandatorySegments = string.Empty;

            if (currentPluglet == null)
                throw new ArgumentNullException("currentPluglet");

            if (currentPluglet.IsSameSegment(segmentName, segmentDetails, ignoreOccurrenceCheck))
            {
                return currentPluglet;
            }

            if (currentPluglet.IsMandatory && currentPluglet.IsIgnore == false && currentPluglet.PlugletType == PlugletType.Segment)
            {
                missingMandatorySegments = currentPluglet.Name;
            }

            //if all child nodes are data element then return null
            if (currentPluglet.Children == null
                || currentPluglet.Children.All(n => n.PlugletType == PlugletType.Data))
                return null;

            string tmpMissingMandatorySegments;

            foreach (IPluglet child in currentPluglet.Children)
            {
                IPluglet node = child.FindInSubTree(segmentName, segmentDetails, ignoreOccurrenceCheck, out tmpMissingMandatorySegments);

                if ((child.IsMandatory && child.IsIgnore == false) || node != null)
                    missingMandatorySegments = JoinCSVList(missingMandatorySegments, tmpMissingMandatorySegments);

                if (node != null)
                    return node;
            }

            return null;
        }

        /// <summary>
        /// Find out the node in tree for a given xPath
        /// </summary>
        /// <param name="currentPluget"></param>
        /// <param name="xPath"></param>
        /// <returns></returns>
        public static IPluglet MoveTo(this IPluglet rootPluget, string xPath)
        {
            if (rootPluget == null)
                return null;

            IPluglet pluglet = null;

            string[] pathElements = xPath.Split(new string[] { rootPluget.PathSeperator }, StringSplitOptions.None);

            // Check if root element and 1st element in path match
            if (string.Equals(pathElements[0], rootPluget.Name, StringComparison.OrdinalIgnoreCase) == false)
                throw new PlugDataModelException(
                    string.Format("Error while traversing path {0}. First element in path {1} does not match with root element {2} of document",
                            xPath, pathElements[0], rootPluget.Name));

            IPluglet prevPluglet = rootPluget;

            //code to load attributes in a dictionary
            Dictionary<string, string> attributes = new Dictionary<string,string>();
            foreach (IPluglet attr in prevPluglet.Attributes)
            {
                string name = attr.Name;
                name = name.Remove(0, 1);
                name = name.Remove(name.Length-1, 1);
                attributes.Add(name, name);
            }

            for (int i = 1; i < pathElements.Length; i++)
            {
                pluglet = prevPluglet.Children.FirstOrDefault(
                            f => string.Equals(f.Name, pathElements[i], StringComparison.OrdinalIgnoreCase));

                if (pluglet == null)
                {
                    // Check if this is last pathElement and if it is then check if 
                    // it's attributes 

                    if (i == pathElements.Length - 1 && prevPluglet.Attributes != null
                        && attributes.ContainsKey(pathElements[i]))
                    {
                        pluglet = prevPluglet;
                    }
                    else
                        throw new PlugDataModelException(
                            string.Format("Error while traversing path {0}. {1} node not found", xPath, pathElements[i]));
                }

                prevPluglet = pluglet;
            }

            return pluglet;
        }

        /// <summary>
        /// Construct DocumentFragment from IPluglet for non-leaf elements
        /// </summary>
        /// <param name="pluglet"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static DocumentFragment ConstructDocumentFragment(this IPluglet pluglet, IDocumentFragment parent, string value)
        {
            if (pluglet == null)
                throw new ArgumentNullException("pluglet");

            ++pluglet.CurrentOccurrences;

            DocumentFragment documentFragment = new DocumentFragment()
                                                {
                                                    Pluglet = pluglet,
                                                    Parent = parent,
                                                    Value = value,
                                                };

            return documentFragment;
        }

        /// <summary>
        /// Construct DocumentFragment for leaf node parent and then create and attach leaf nodes to it.
        /// </summary>
        /// <param name="pluglet"></param>
        /// <param name="segmentDetails"></param>
        /// <param name="internalSegment"></param>
        /// <param name="ediDelimiters"></param>
        /// <param name="segmentStartIndex"></param>
        /// <param name="segmentEndIndex"></param>
        /// <param name="ediErrors"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static DocumentFragment ConstructDocumentFragment(this IPluglet pluglet, string[] segmentDetails, bool internalSegment, Delimiters ediDelimiters, 
            int segmentSeqNumber, long segmentStartIndex, long segmentEndIndex, ref InterchangeErrors ediErrors, out string error)
        {
            if (pluglet == null)
                throw new ArgumentNullException("pluglet");

            if (segmentDetails == null)
                throw new ArgumentNullException("segmentDetails");

            LoggerFactory.Logger.Info("PlugletExtensions.ConstructDocumentFragment", "Constructing {0} instance", pluglet.Tag);

            if (pluglet.Children == null)
            {
                error = string.Format("Pluglet {0} does not have childrens", pluglet.Name);
                return null;
            }

            // -1 is required since SegmentDetails[0] is the SegmentName
            if (pluglet.Children.Count < segmentDetails.Length - 1)
            {
                error = string.Format("Document has {1} data elements, it is less than segment children count {2} as described in Spec",
                    pluglet.Name, pluglet.Children.Count, segmentDetails.Length);
                return null;
            }

            error = string.Empty;
            ++pluglet.CurrentOccurrences;
            DocumentFragment documentFragment = new DocumentFragment()
                                                {
                                                    Pluglet = pluglet,
                                                    Children = new List<IDocumentFragment>(),
                                                    SequenceNumber = segmentSeqNumber,
                                                    StartOffset = segmentStartIndex,
                                                    EndOffset = segmentEndIndex,
                                                };

            int plugletNum = 0;
            DocumentFragment dataFragment;
            IPluglet plugletChild;
            bool validateValue;

            long currentSegmentFieldStartIndex = segmentStartIndex;

            //SegmentNum start from 1 since SegmentDetails[0] is Segment name
            int segmentNum = 1;
            // if this is internal segment (composite segment - segment handing data and segment as children) then 
            // start segmentDetails from 0
            if (internalSegment == true)
                segmentNum = 0;

            EdiErrorType errorType = pluglet.IsIgnore ? EdiErrorType.Warning : EdiErrorType.Error;

            for (; segmentNum < segmentDetails.Length; segmentNum++, plugletNum++ )
            {
                if(segmentNum != 0)
                    currentSegmentFieldStartIndex += segmentDetails[segmentNum-1].Length + 1;

                validateValue = false;
                plugletChild = pluglet.Children[plugletNum];

                if(plugletChild.PlugletType == PlugletType.Data)
                {
                    dataFragment = new DocumentFragment()
                                    {
                                        Parent = documentFragment,
                                        Pluglet = plugletChild,
                                        Value = segmentDetails[segmentNum],
                                        /*SequenceNumber = segmentSeqNumber,
                                        StartOffset = currentSegmentFieldStartIndex,
                                        EndOffset = currentSegmentFieldStartIndex + segmentDetails[segmentNum].Length,*/
                                    };

                    validateValue = true;
                }
                else if (plugletChild.PlugletType == PlugletType.Segment)
                {
                    if(string.IsNullOrEmpty(segmentDetails[segmentNum]) == false)
                    {
                        string segmentError = string.Empty;

                        string []internalSegmentDetails = segmentDetails[segmentNum].Split((char)ediDelimiters.ComponentSeperator);

                        dataFragment = plugletChild.ConstructDocumentFragment(internalSegmentDetails, true, ediDelimiters, segmentSeqNumber, currentSegmentFieldStartIndex, 
                            currentSegmentFieldStartIndex + segmentDetails[segmentNum].Length, ref ediErrors, out segmentError);

                        if(string.IsNullOrEmpty(segmentError) == false)
                        {
                            if (string.IsNullOrEmpty(error) == true)
                                error = string.Format("ConstructDocumentSegmentInstance: Pluglet {0}: ", pluglet.Name);

                            error = string.Format("{0} Error constructing {1}: {2}", error, plugletChild.Name, segmentError);
                        }
                    }
                    else
                    {
                        dataFragment = new DocumentFragment()
                                    {
                                        Parent = documentFragment,
                                        Pluglet = plugletChild,
                                        Value = segmentDetails[segmentNum],
                                    };
                    }
                }
                else
                {
                    error = string.Format("ConstructDocumentSegmentInstance: Error constructing SegmentInstance. Pluglet {0} is of type {1}",
                        pluglet.Name, plugletChild.Name);
                    return null;
                }

                if (validateValue == true && plugletChild.DataType != null && string.IsNullOrEmpty(segmentDetails[segmentNum]) == false)
                {
                    FieldError fieldError = plugletChild.DataType.ValidateValue(new StringBuilder(segmentDetails[segmentNum]));
                    if (fieldError != null)
                    {
                        if (string.IsNullOrEmpty(error) == true)
                            error = string.Format("ConstructDocumentSegmentInstance: Pluglet {0}: ", pluglet.Name);

                        error = string.Format("{0} Error data validation failed {1}: {2} ({3})", error, plugletChild.Name, fieldError.Description, fieldError.DataValue);

                        ediErrors.AddFieldError(segmentDetails[0], plugletChild.Tag, fieldError.ErrorCode, fieldError.Description, segmentSeqNumber, segmentNum, segmentDetails[segmentNum], currentSegmentFieldStartIndex,
                            currentSegmentFieldStartIndex + segmentDetails[segmentNum].Length - 1, errorType);
                    }
                }

                //TODO: Add validation (extension method)
                documentFragment.Children.Add(dataFragment);

                if (string.IsNullOrEmpty(segmentDetails[segmentNum]) && plugletChild.IsMandatory == true)
                {
                    if (string.IsNullOrEmpty(error) == true)
                        error = string.Format("ConstructDocumentSegmentInstance: Pluglet {0}: ", pluglet.Name);

                    error = string.Format("{0} Child {1} is mandatory but missing", error, plugletChild.Name);

                    ediErrors.AddFieldError(segmentDetails[0], plugletChild.Tag, X12ErrorCode.DeMandatoryDataElementMissingCode,
                        X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeMandatoryDataElementMissingCode), segmentSeqNumber, segmentNum, string.Empty,
                        currentSegmentFieldStartIndex, currentSegmentFieldStartIndex, errorType);
                }
            }

            for (; segmentNum <= pluglet.Children.Count; segmentNum++, plugletNum++)
            {
                plugletChild = pluglet.Children[plugletNum];
                if (plugletChild.IsMandatory == true)
                {
                    if (string.IsNullOrEmpty(error) == true)
                        error = string.Format("ConstructDocumentSegmentInstance: Pluglet {0}: ", pluglet.Name);

                    error = string.Format("{0} Child {1} is mandatory but missing", error, plugletChild.Name);

                    ediErrors.AddFieldError(segmentDetails[0], plugletChild.Tag, X12ErrorCode.DeMandatoryDataElementMissingCode,
                        X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeMandatoryDataElementMissingCode), segmentSeqNumber, segmentNum, string.Empty,
                        currentSegmentFieldStartIndex, currentSegmentFieldStartIndex, errorType);
                }
            }

            return documentFragment;
        }

        /// <summary>
        /// Clone IPluglet
        /// </summary>
        /// <param name="pluglet"></param>
        /// <param name="withChild">True if childs also need to be cloned</param>
        /// <returns></returns>
        public static Pluglet Clone(this IPluglet pluglet, bool withChild)
        {
            Pluglet newPluglet = new Pluglet(pluglet.Name, pluglet.Definition, pluglet.PlugletType, null,
                                            pluglet.RepetitionInfo.MinOccurs, pluglet.RepetitionInfo.MaxOccurs);

            newPluglet.DataType = pluglet.DataType;
            newPluglet.IsRecursiveMandatory = pluglet.IsRecursiveMandatory;

            if (pluglet.Attributes != null)
            {
                foreach (IPluglet attr in pluglet.Attributes)
                {
                    newPluglet.Attributes.Add( attr.Clone(false) );
                }
            }

            if (withChild && pluglet.Children != null)
            {
                foreach (Pluglet child in pluglet.Children)
                {
                    newPluglet.Children.Add(child.Clone(withChild));
                }
            }

            return newPluglet;
        }

        /// <summary>
        /// Merge pluglet and newPluglet and return merged pluglet.
        /// Assumption: pluglet and newPluglet does not have loops
        /// </summary>
        /// <param name="pluglet"></param>
        /// <param name="newPluglet"></param>
        /// <param name="canonicalPluglet"></param>
        /// <returns></returns>
        public static Pluglet Merge(this IPluglet pluglet, IPluglet newPluglet, IPluglet canonicalPluglet)
        {
            if (pluglet == null)
                throw new ArgumentNullException("pluglet");
            if (newPluglet == null)
                throw new ArgumentNullException("newPluglet");
            if (canonicalPluglet == null)
                throw new ArgumentNullException("canonicalPluglet");

            Pluglet mergedPluglet;

            // Make sure that both pluglet has same name
            if (string.Equals(pluglet.Name, newPluglet.Name, StringComparison.OrdinalIgnoreCase) == false)
                throw new PlugDataModelException(
                    string.Format("Merge operation is invalid: pluglet ({0}) and newpluglet({1}) does not match.", pluglet.Name, newPluglet.Name));

            if (string.Equals(pluglet.Name, canonicalPluglet.Name, StringComparison.OrdinalIgnoreCase) == false)
                throw new PlugDataModelException(string.Format("Merge operation params are invalid: {0} and {1}", pluglet.Name, canonicalPluglet.Name));

            // If one of the pluglet does not have children then assign non-null to tempPluglet, clone it with 
            // childrens and return
            IPluglet tempPluglet = null;
            if (pluglet.Children == null || newPluglet.Children == null)
                tempPluglet = pluglet ?? newPluglet;

            if (tempPluglet != null)
            {
                mergedPluglet = tempPluglet.Clone(true);
            }
            else
            {
                mergedPluglet = pluglet.Clone(false);
                
                int plugletChildIndex, newPlugletChildIndex;
                bool plugletChildExist, newPlugletChildExist;
                plugletChildIndex = newPlugletChildIndex = 0;

                foreach (IPluglet child in canonicalPluglet.Children)
                {
                    plugletChildExist = newPlugletChildExist = false;

                    if (plugletChildIndex < pluglet.Children.Count &&
                        string.Equals(child.Name, pluglet.Children[plugletChildIndex].Name, StringComparison.OrdinalIgnoreCase))
                        plugletChildExist = true;

                    if (newPlugletChildIndex < newPluglet.Children.Count && 
                        string.Equals(child.Name, newPluglet.Children[newPlugletChildIndex].Name, StringComparison.OrdinalIgnoreCase))
                        newPlugletChildExist = true;

                    // If both pluglet has same child then call Merge
                    if (plugletChildExist && newPlugletChildExist)
                    {
                        // TODO: Perf optimization - We don't need to call Merge if child does not have childrens
                        mergedPluglet.Children.Add(pluglet.Children[plugletChildIndex].Merge(newPluglet.Children[newPlugletChildIndex], child));

                        plugletChildIndex++;
                        newPlugletChildIndex++;
                    }
                    // If only one pluglet has child then clone it with childrens
                    else if (plugletChildExist == true)
                    {
                        mergedPluglet.Children.Add(pluglet.Children[plugletChildIndex].Clone(true));

                        plugletChildIndex++;
                    }
                    else if (newPlugletChildExist == true)
                    {
                        mergedPluglet.Children.Add(newPluglet.Children[newPlugletChildIndex].Clone(true));

                        newPlugletChildIndex++;
                    }

                    if (plugletChildIndex == pluglet.Children.Count && newPlugletChildIndex == newPluglet.Children.Count)
                        break;
                }
            }

            return mergedPluglet;
        }

        /// <summary>
        /// Remove loops from pluglet
        /// </summary>
        /// <param name="pluglet"></param>
        /// <param name="canonicalPluglet"></param>
        /// <returns></returns>
        public static Pluglet RemoveLoops(this IPluglet pluglet, IPluglet canonicalPluglet)
        {
            if (pluglet == null)
                throw new ArgumentNullException("pluglet");

            Pluglet resultPluglet = pluglet.Clone(false);

            if (pluglet.Children != null && pluglet.Children.Count > 0)
            {
                int childIndex;
                IPluglet prevChild, mergedChild, canonicalChildPluglet = null;
                prevChild = mergedChild = canonicalChildPluglet = null;

                for(childIndex = 0; childIndex < pluglet.Children.Count; childIndex++)
                {
                    canonicalChildPluglet = canonicalPluglet.Children.First(p => p.Name == pluglet.Children[childIndex].Name);
                    pluglet.Children[childIndex] = pluglet.Children[childIndex].RemoveLoops(canonicalChildPluglet);

                    if (prevChild != null)
                    {
                        if (string.Equals(prevChild.Name, pluglet.Children[childIndex].Name, StringComparison.OrdinalIgnoreCase))
                        {
                            mergedChild = mergedChild.Merge(pluglet.Children[childIndex], canonicalChildPluglet);
                        }
                        else
                        {
                            resultPluglet.Children.Add(mergedChild);
                            mergedChild = null;
                        }
                    }

                    prevChild = pluglet.Children[childIndex];
                    if (mergedChild == null)
                        mergedChild = pluglet.Children[childIndex];
                }

                if(mergedChild == null)
                    mergedChild = pluglet.Children[childIndex - 1];

                resultPluglet.Children.Add(mergedChild);
            }

            return resultPluglet;
        }

        public static XElement SerializeToXml(this IDocumentPlug documentPlug)
        {
            if (documentPlug == null)
                return null;

            string name = string.IsNullOrEmpty(documentPlug.Name) ? "undef" : documentPlug.Name;
            string elementDelimiter = GetDelimiterString(documentPlug.ElementDelimiters);
            string segmentDelimiter = GetDelimiterString(documentPlug.SegmentDelimiters);

            XElement plugletXml = new XElement("DocumentPlug"
                                    , new XAttribute("Name", name)
                                    , new XAttribute("ElementDelimiter", elementDelimiter)
                                    , new XAttribute("SegmentDelimiter", segmentDelimiter)
                                    );

            plugletXml.Add(documentPlug.RootPluglet.SerializeToXml());

            return plugletXml;
        }

        public static XElement SerializeToXml(this IPluglet pluglet)
        {
            if (pluglet == null)
                return null;

            string description = string.IsNullOrEmpty(pluglet.Definition) ? "undef" : pluglet.Definition;
            string standard = string.IsNullOrEmpty(pluglet.DEStandard) ? "undef" : pluglet.DEStandard;
            string denumber = string.IsNullOrEmpty(pluglet.DENumber) ? "undef" : pluglet.DENumber;

            XElement plugletXml = new XElement(pluglet.PlugletType.ToString()
                                    , new XAttribute("Tag", pluglet.Tag)
                                    , new XAttribute("Name", pluglet.Name)
                                    , new XAttribute("Definition", description)
                                    , new XAttribute("DENameStandard", standard)
                                    , new XAttribute("DENumber", denumber)
                                    , new XAttribute("IsMandatory", pluglet.IsMandatory)
                                    , new XAttribute("IsIgnore", pluglet.IsIgnore)
                                    , new XAttribute("MaxOccur", pluglet.RepetitionInfo.MaxOccurs)
                                    , new XAttribute("IsRepeatable", pluglet.IsRepeatable)
                                    , new XAttribute("DataType", GetPlugletDataType(pluglet.DataType))
                                    );

            if (pluglet.PlugletType != PlugletType.Data && pluglet.PlugletType != PlugletType.Unknown)
                plugletXml.Add(new XAttribute("Path", pluglet.Path));

            if(pluglet.Children != null
                //&& pluglet.PlugletType != PlugletType.Segment
                )
                foreach(IPluglet child in pluglet.Children)
                    //if(child.PlugletType != PlugletType.Data && child.PlugletType != PlugletType.Unknown)
                        plugletXml.Add(child.SerializeToXml());

            return plugletXml;
        }

        private static object GetPlugletDataType(X12BaseDataType dataType)
        {
            string dataTypeString = dataType == null ? string.Empty : dataType.ToString();

            if(string.IsNullOrWhiteSpace(dataTypeString) == false)
            {
                X12_IdDataType idDataType = dataType as X12_IdDataType;
                if (idDataType != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(dataTypeString);
                    if (idDataType.AllowedValues != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in idDataType.AllowedValues)
                            if (!string.IsNullOrEmpty(kvp.Value))
                            {
                                Contingency contingency = idDataType.GetContingencies(kvp.Key);
                                if (contingency != null && contingency.ContingencyValues != null && contingency.ContingencyValues.Count > 0)
                                {
                                    sb.AppendFormat(" '{0}:{1}:Contingencies{{", kvp.Key, kvp.Value);
                                    sb.AppendFormat(string.Join(",", contingency.ContingencyValues));
                                    sb.AppendFormat("}}'");
                                }
                                else
                                    sb.AppendFormat(" '{0}:{1}'", kvp.Key, kvp.Value);
                            }
                    }
                    dataTypeString = sb.ToString();
                }
            }

            return dataTypeString;
        }

        public static void ResetCurrentOccurances(this IPluglet pluglet)
        {
            if (pluglet == null)
                return;

            pluglet.CurrentOccurrences = 0;

            if (pluglet.Children != null)
                foreach (IPluglet child in pluglet.Children)
                    child.ResetCurrentOccurances();
        }

        /// <summary>Get first segment (PlugType=Segment/CompositeData) pluglet from currentPluglet.</summary>
        /// <param name="pluglet"></param>
        /// <param name="segment"></param>
        /// <returns>First segment/compositeData pluglet if found, otherwise null</returns>
        public static IPluglet GetFirstSegment(this IPluglet pluglet, string segment)
        {
            if (pluglet == null)
                return null;

            if (string.IsNullOrWhiteSpace(segment))
            {
                if (pluglet.Children != null && pluglet.Children.Count > 0)
                    return pluglet.Children[0];

                return null;
            }

            foreach (IPluglet child in pluglet.Children)
            {
                if ( (child.PlugletType == PlugletType.Segment || child.PlugletType == PlugletType.CompositeData)
                    && string.Equals(segment, child.Tag))
                    return child;
                else
                    if (child.PlugletType == PlugletType.Loop)
                    {
                        IPluglet segmentPluglet = child.GetFirstSegment(segment);
                        if (segmentPluglet != null)
                            return segmentPluglet;
                    }
            }

            return null;
        }

        private static string GetDelimiterString(List<int> delimiters)
        {
            if (delimiters == null)
                return string.Empty;

            StringBuilder strDelimiter = new StringBuilder();

            string separator = string.Empty;

            foreach (int delimiter in delimiters)
            {
                strDelimiter.AppendFormat("{0}{1}", separator, delimiter.ToString());
                separator = " ";
            }

            return strDelimiter.ToString();
        }

        private static string JoinCSVList(string list1, string list2)
        {
            if (string.IsNullOrWhiteSpace(list1))
                return list2;

            if (string.IsNullOrWhiteSpace(list2))
                return list1;

            return list1 + "," + list2;
        }

        public static bool IsSameSegment(this IPluglet pluglet, string segmentName, string[] segmentDetails, bool ignoreOccurrenceCheck)
        {
            bool isSameSegment = false;
            if (pluglet.PlugletType == PlugletType.Segment || pluglet.PlugletType == PlugletType.CompositeData)
            {
                // Set currentOccur based on number of occurances
                // option-1 (not clean design, but quick and dirty) - Maintain count in IPluglet and reset it on EDIReader.Initialize
                // option-2 - Refer to document fragment (result of EDI reader) and get current occurrence count
                int currentOccur = pluglet.CurrentOccurrences;

                if (string.Equals(pluglet.Tag, segmentName, StringComparison.InvariantCultureIgnoreCase)
                    && (ignoreOccurrenceCheck == true
                        ||
                        (currentOccur == 0
                            || (pluglet.IsRepeatable
                                && (pluglet.RepetitionInfo.MaxOccurs == -1 || pluglet.RepetitionInfo.MaxOccurs > currentOccur)))))
                {
                    if (pluglet.ContainsTriggerChildField)
                    {
                        for(int i = 0; i < pluglet.Children.Count; i++)
                        {
                            IPluglet child = pluglet.Children[i];
                            if (child.IsTriggerField)
                            {
                                // For trigger field pluglet should is of type ID (enum) and one of the value should match the dataSegmentValue
                                X12_IdDataType dataType = child.DataType as X12_IdDataType;
                                if (dataType != null
                                    && (i + 1) < segmentDetails.Length
                                    && !string.IsNullOrWhiteSpace(segmentDetails[i + 1]))
                                {
                                    string dataSegmentValue = segmentDetails[i + 1].Trim().ToUpperInvariant();
                                    foreach (string value in dataType.AllowedValues.Keys)
                                    {
                                        if (value.ToUpperInvariant().Equals(dataSegmentValue))
                                        {
                                            isSameSegment = true;
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else
                        isSameSegment = true;
                }
            }

            return isSameSegment;
        }

        /// <summary>Get segment pluglet for given segmentName from currentPluglet. Also report missing mandatory segments.</summary>
        /// <param name="currentPluglet"></param>
        /// <param name="segmentName"></param>
        /// <param name="missingMandatorySegments"></param>
        /// <returns></returns>
        public static IPluglet GetSegmentPluglet(this IPluglet currentPluglet, string segmentName, string[] segmentDetails, string firstSegmentName, out string missingMandatorySegments)
        {
            IPluglet nextPluglet = currentPluglet;
            missingMandatorySegments = string.Empty;

            // Special case for first time - Only first time CurrentPluglet will be Loop, otherwise it will be segment always
            if (nextPluglet.PlugletType == PlugletType.Loop)
            {
                nextPluglet = nextPluglet.GetFirstSegment(firstSegmentName);

                // Since below code assume that nextPluglet points to already consumed segment
                // before moving to that check if it matches given segment name
                if (nextPluglet.IsSameSegment(segmentName, segmentDetails, false))
                    return nextPluglet;
                else if(nextPluglet.IsIgnore == false)
                    missingMandatorySegments = nextPluglet.Name;
            }
            else
                if (nextPluglet.IsSameSegment(segmentName, segmentDetails, false))
                {
                    return nextPluglet;
                }

            IPluglet tmpPluglet;
            string tmpMissingMandatorySegments;
            int currentOccur;

            while (nextPluglet != null)
            {
                tmpPluglet = FindPlugletAfter(nextPluglet, segmentName, segmentDetails, out tmpMissingMandatorySegments);

                if((nextPluglet.IsMandatory && nextPluglet.IsIgnore == false) || tmpPluglet != null)
                    missingMandatorySegments = JoinCSVList(missingMandatorySegments, tmpMissingMandatorySegments);

                if (tmpPluglet != null)
                {
                    return tmpPluglet;
                }

                tmpPluglet = nextPluglet.Parent;

                if (tmpPluglet != null && tmpPluglet.IsRepeatable)
                {
                    // TODO: Set currentOccur based on number of occurances
                    currentOccur = tmpPluglet.CurrentOccurrences;

                    if (tmpPluglet.RepetitionInfo.MaxOccurs == -1 || tmpPluglet.RepetitionInfo.MaxOccurs > currentOccur)
                    {
                        tmpPluglet = FindPlugletBefore(nextPluglet, segmentName, segmentDetails, out tmpMissingMandatorySegments);
                        if (tmpPluglet != null)
                        {
                            // Reset max occurrences for all childs as we are going back to loop
                            nextPluglet.Parent.ResetCurrentOccurances();
                            missingMandatorySegments = JoinCSVList(missingMandatorySegments, tmpMissingMandatorySegments);

                            return tmpPluglet;
                        }
                    }
                }

                nextPluglet = nextPluglet.Parent;
            }

            return nextPluglet;
        }

        /// <summary>
        ///     Search for segment pluglet matching segmentName from given currentPluglet. Search right side of current pluglet. 
        /// </summary>
        /// <param name="currentPluglet"></param>
        /// <param name="segmentName"></param>
        /// <param name="missingMandatorySegments"></param>
        /// <returns></returns>
        public static IPluglet FindPlugletAfter(this IPluglet currentPluglet, string segmentName, string[] segmentDetails, out string missingMandatorySegments)
        {
            bool ignoreOccurrenceCheck = false;
            missingMandatorySegments = string.Empty;

            if (currentPluglet == null || currentPluglet.Parent == null || currentPluglet.Parent.Children.Count < 2)
                return null;

            string tmpMissingMandatorySegments;
            bool processPluglets = false;
            IPluglet tmpPluglet;

            // Skip all pluglets till current pluglet and then start comparing
            foreach (IPluglet child in currentPluglet.Parent.Children)
            {
                if (processPluglets)
                {
                    if (child.PlugletType == PlugletType.Segment || child.PlugletType == PlugletType.CompositeData)
                    {
                        if (child.IsSameSegment(segmentName, segmentDetails, ignoreOccurrenceCheck))
                            return child;

                        // TODO: Mandatory and IsIgnore is confusing in one if statement
                        // Mandatory actually indicate that partner will accept this segment
                        if (child.IsMandatory && child.IsIgnore == false)
                            missingMandatorySegments = JoinCSVList(missingMandatorySegments, child.Tag);
                    }
                    else // for loops
                    {
                        tmpPluglet = child.FindInSubTree(segmentName, segmentDetails, ignoreOccurrenceCheck, out tmpMissingMandatorySegments);

                        // Check if we need to include missing mandatory segments
                        // TODO: Mandatory and IsIgnore is confusing in one if statement
                        // Mandatory actually indicate that partner will accept this segment
                        if ((child.IsMandatory && child.IsIgnore == false) || tmpPluglet != null)
                            missingMandatorySegments = JoinCSVList(missingMandatorySegments, tmpMissingMandatorySegments);

                        if (tmpPluglet != null)
                        {
                            return tmpPluglet;
                        }
                    }
                }
                else
                    // TODO: Check if we need reference equality here
                    if (child == currentPluglet)
                        processPluglets = true;
            }

            return null;
        }

        /// <summary>
        ///     Search for segment pluglet matching segmentName from given currentPluglet. Search left side of current pluglet.
        /// </summary>
        /// <param name="currentPluglet"></param>
        /// <param name="segmentName"></param>
        /// <param name="missingMandatorySegments"></param>
        /// <returns></returns>
        public static IPluglet FindPlugletBefore(this IPluglet currentPluglet, string segmentName, string[] segmentDetails, out string missingMandatorySegments)
        {
            bool ignoreOccurrenceCheck = true;
            missingMandatorySegments = string.Empty;

            if (currentPluglet == null || currentPluglet.Parent == null || currentPluglet.Parent.Children.Count < 2)
                return null;

            string tmpMissingMandatorySegments;
            IPluglet tmpPluglet;

            foreach (IPluglet child in currentPluglet.Parent.Children)
            {
                // Compare pluglets till current pluglet only
                // TODO: Check if we need reference equality here
                if (child == currentPluglet)
                    break;

                if (child.PlugletType == PlugletType.Segment || child.PlugletType == PlugletType.CompositeData)
                {
                    if (child.IsSameSegment(segmentName, segmentDetails, ignoreOccurrenceCheck))
                        return child;

                    // TODO: Mandatory and IsIgnore is confusing in one if statement
                    // Mandatory actually indicate that partner will accept this segment
                    if (child.IsMandatory && child.IsIgnore == false)
                        missingMandatorySegments = JoinCSVList(missingMandatorySegments, child.Tag);
                }
                else // for loops
                {
                    tmpPluglet = child.FindInSubTree(segmentName, segmentDetails, ignoreOccurrenceCheck, out tmpMissingMandatorySegments);

                    // Check if we need to include missing mandatory segments
                    // TODO: Mandatory and IsIgnore is confusing in one if statement
                    // Mandatory actually indicate that partner will accept this segment
                    if ( (child.IsMandatory && child.IsIgnore == false) || tmpPluglet != null)
                        missingMandatorySegments = JoinCSVList(missingMandatorySegments, tmpMissingMandatorySegments);

                    if (tmpPluglet != null)
                    {
                        return tmpPluglet;
                    }
                }
            }

            return null;
        }
    }
}
