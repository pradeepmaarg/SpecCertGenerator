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
    public static class DocumentFragmentExtensions
    {
        /// <summary>
        /// Traverse tree from DocumentFragment based on given xPath. If node is not present
        /// then add it. 
        /// </summary>
        /// <param name="currentDocument"></param>
        /// <param name="xPath"></param>
        public static DocumentFragment AddIntermediateDocumentFragment(this DocumentFragment document, string xPath)
        {
            if (document == null)
                throw new ArgumentNullException("document", "AddIntermediateDocumentFragment: Cannot add intermediate document fragments to null DocumentFragment");

            if (string.IsNullOrEmpty(xPath))
                throw new ArgumentNullException("xPath", "AddIntermediateDocumentFragment: xPath cannot be null");

            ILogger logger = LoggerFactory.Logger;

            string[] pathElements = xPath.Split(new string[] { document.Pluglet.PathSeperator }, StringSplitOptions.None);

            // Check if root element and 1st element in path match
            if (string.Equals(pathElements[0], document.Name, StringComparison.OrdinalIgnoreCase) == false)
                throw new PlugDataModelException(
                    string.Format("Error while adding intermediate in {0} path. First element in path {1} does not match with root element {2} of document",
                            xPath, pathElements[0], document.Name));

            DocumentFragment nextFragment = null;
            DocumentFragment currentFragment = (DocumentFragment)document;

            StringBuilder pathTillCurrentNode = new StringBuilder();
            pathTillCurrentNode.AppendFormat("{0}", pathElements[0]);

            for (int i = 1; i < pathElements.Length - 1; i++)
            {
                //TODO: Optimize here - following call is not required once following if condition becomes true
                if (currentFragment.Children != null)
                {
                    // Check if it's last child is pathElements[i], otherwise create new instance
                    nextFragment = (DocumentFragment)currentFragment.Children.Last();

                    if (string.Equals(nextFragment.Name, pathElements[i], StringComparison.OrdinalIgnoreCase) == false)
                        nextFragment = null;

                    // Special case for last but one pathElement - Check if we need to recreate node (loop?)
                    // This is required to generate different loop nodes (e.g. BVT_X12_850.txt)
                    if (nextFragment != null && nextFragment.Children != null && i == pathElements.Length - 2 && nextFragment.Pluglet.IsRepeatable == true)
                    {
                        // Check last child and pathElements[i+1] child order
                        DocumentFragment lastChildFragment = (DocumentFragment)nextFragment.Children.Last();

                        bool restartLoop = false;
                        foreach (IPluglet child in nextFragment.Pluglet.Children)
                        {
                            if (string.Equals(child.Name, pathElements[i + 1], StringComparison.OrdinalIgnoreCase) == true)
                            {
                                restartLoop = true;
                                break;
                            }

                            if (string.Equals(child.Name, lastChildFragment.Name, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                break;
                            }
                        }

                        if (restartLoop == true)
                            nextFragment = null;
                    }
                }

                pathTillCurrentNode.AppendFormat("{0}{1}", document.Pluglet.PathSeperator, pathElements[i]);
                if (nextFragment == null)
                {
                    logger.Debug("DocumentFragmentExtensions.AddDocumentFragment", "Node {0} does not exist, creating one now", currentFragment.Name);

                    IPluglet pluglet = document.Pluglet.MoveTo(pathTillCurrentNode.ToString());
                    nextFragment = pluglet.ConstructDocumentFragment(currentFragment, null);

                    if (currentFragment.Children == null)
                        currentFragment.Children = new List<IDocumentFragment>();
                    currentFragment.Children.Add(nextFragment);
                }

                currentFragment = nextFragment;
                nextFragment = null;
            }

            return currentFragment;
        }

        /// <summary>
        /// Add new DocumentFragment based on it's xPath. If intermediate nodes are not available then this function create those nodes too.
        /// </summary>
        /// <param name="currentDocument"></param>
        /// <param name="newFragment"></param>
        public static void AddDocumentFragment(this DocumentFragment document, IDocumentFragment newFragment)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            if (newFragment == null)
                throw new ArgumentNullException("newFragment");

            ILogger logger = LoggerFactory.Logger;
            logger.Debug("DocumentFragmentExtensions.AddDocumentFragment", "Adding document fragment {0}", newFragment.Name);

            DocumentFragment parentFragment = document.AddIntermediateDocumentFragment(newFragment.Pluglet.Path);

            if (parentFragment.Children == null)
                parentFragment.Children = new List<IDocumentFragment>();
            parentFragment.Children.Add(newFragment);
            ((DocumentFragment)newFragment).Parent = parentFragment;

            logger.Debug("DocumentFragmentExtensions.AddDocumentFragment", "Added document fragment {0}", newFragment.Name);
        }

        /// <summary>
        /// Set atribute value on a DocumentFragment based on it's xPath.
        /// If intermediate nodes are not available then this function create those nodes too.
        /// </summary>
        /// <param name="currentDocument"></param>
        /// <param name="newFragment"></param>
        public static void AddDocumentFragment(this DocumentFragment document, string xPath, string value)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            if (string.IsNullOrEmpty(xPath))
                throw new ArgumentNullException("xPath");

            string attributeName = xPath.Substring(xPath.LastIndexOf(document.Pluglet.PathSeperator) + document.Pluglet.PathSeperator.Length);

            ILogger logger = LoggerFactory.Logger;
            logger.Debug("DocumentFragmentExtensions.AddDocumentFragment", "Adding attribute {0}", attributeName);

            DocumentFragment documentFragment = document.AddIntermediateDocumentFragment(xPath);

            if (documentFragment.Attributes == null)
                documentFragment.Attributes = new Dictionary<string, string>();
            documentFragment.Attributes.Add(attributeName, value);

            logger.Debug("DocumentFragmentExtensions.AddDocumentFragment", "Adding attribute {0}", attributeName);
        }

        /// <summary>
        /// Count all segments in a given document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static int CountAllChilds(this IDocumentFragment document)
        {
            if (document.Pluglet.PlugletType == PlugletType.Segment)
                return 1;

            if(document.Children == null)
                return 0;

            int count = 0;
            foreach (IDocumentFragment child in document.Children)
                count += child.CountAllChilds();

            return count;
        }

        /// <summary>
        /// Move to given xPath fragment (leaf as well as non-leaf)
        /// </summary>
        /// <param name="rootFragment"></param>
        /// <param name="xPath"></param>
        /// <returns></returns>
        public static IDocumentFragment MoveTo(this IDocumentFragment rootFragment, string xPath)
        {
            if (rootFragment == null)
                return null;

            IDocumentFragment documentFragment = null;

            string[] pathElements = xPath.Split(new string[] { rootFragment.Pluglet.PathSeperator }, StringSplitOptions.None);

            // Check if root element and 1st element in path match
            if (string.Equals(pathElements[0], rootFragment.Name, StringComparison.OrdinalIgnoreCase) == false)
                throw new PlugDataModelException(
                    string.Format("Error while traversing path {0}. First element in path {1} does not match with root element {2} of document",
                            xPath, pathElements[0], rootFragment.Name));

            IDocumentFragment prevFragment = rootFragment;

            for (int i = 1; i < pathElements.Length; i++)
            {
                documentFragment = prevFragment.Children.FirstOrDefault(
                            f => string.Equals(f.Name, pathElements[i], StringComparison.OrdinalIgnoreCase));

                if (documentFragment == null)
                    break;

                prevFragment = documentFragment;
            }

            return documentFragment;
        }

        /// <summary>
        /// Create plug based on the fatpipeDocument (instance tree). 
        /// Generated plug will only have pluglets referred in instance tree.
        /// Generated plug will have repeated pluglets in case of loops.
        /// </summary>
        /// <param name="fatpipeDocument"></param>
        /// <param name="existingDocumentPluglet"></param>
        /// <returns></returns>
        public static IDocumentPlug GenerateDocumentPlug(this IFatpipeDocument fatpipeDocument, IDocumentPlug existingDocumentPluglet)
        {
            if (fatpipeDocument == null)
                throw new ArgumentNullException("fatpipeDocument", "Cannot generate document plug for null IFatpipeDocument");

            DocumentPlug documentPlug = new DocumentPlug(null, fatpipeDocument.DocumentPlug.BusinessDomain);

            documentPlug.RootPluglet = fatpipeDocument.RootFragment.GeneratePluglet();

            documentPlug.RootPluglet = documentPlug.RootPluglet.RemoveLoops(fatpipeDocument.RootFragment.Pluglet);

            if (existingDocumentPluglet != null)
                documentPlug.RootPluglet = documentPlug.RootPluglet.Merge(existingDocumentPluglet.RootPluglet, fatpipeDocument.RootFragment.Pluglet);

            documentPlug.RootPluglet.SetParent(null);

            return documentPlug;
        }

        /// <summary>
        /// Create plug based on the fatpipeDocument (instance tree). 
        /// Generated plug will only have pluglets referred in instance tree.
        /// Generated plug will have repeated pluglets in case of loops.
        /// </summary>
        /// <param name="documentFragment"></param>
        /// <returns></returns>
        public static Pluglet GeneratePluglet(this IDocumentFragment documentFragment)
        {
            Pluglet pluglet = documentFragment.Pluglet.Clone(false);

            // Check if instance tree has any childrens, if not then copy all childrens from existingPluglet
            if (documentFragment.Children != null)
            {
                foreach (IDocumentFragment child in documentFragment.Children)
                {
                    pluglet.Children.Add(child.GeneratePluglet());
                }
            }

            return pluglet;
        }

        public static string GetDataSegmentValue(this IDocumentFragment documentFragment, string dataSegmentName)
        {
            string segmentValue = null;

            if (documentFragment != null && documentFragment.Children != null)
            {
                foreach (DocumentFragment child in documentFragment.Children)
                    if (string.Equals(child.Pluglet.Tag, dataSegmentName, StringComparison.OrdinalIgnoreCase))
                    {
                        segmentValue = child.Value;
                        break;
                    }
            }

            return segmentValue;
        }

        private static void SetParent(this IPluglet pluglet, IPluglet parent)
        {
            pluglet.Parent = parent;

            if (pluglet.Children != null)
            {
                foreach (IPluglet child in pluglet.Children)
                {
                    child.SetParent(pluglet);
                }
            }
        }
    }
}
