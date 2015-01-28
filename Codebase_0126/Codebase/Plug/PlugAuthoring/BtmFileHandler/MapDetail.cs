using Maarg.Fatpipe.Plug.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maarg.Fatpipe.Plug.Authoring.BtmFileHandler
{
    public class MapDetail
    {
        private string folderName;
        private string domainName;
        private string orgName;
        private string direction;
        private int documentType;

        public string FolderName 
        {
            get { return folderName; }
            set
            {
                folderName = value;

                //if (folderName.StartsWith("GCommerce", StringComparison.OrdinalIgnoreCase) || folderName.StartsWith("Maps", StringComparison.OrdinalIgnoreCase))
                //    return;

                //Console.WriteLine("Extracting metadata from {0}", value);

                //string tmpString = value;
                //int idx;
                //idx = tmpString.LastIndexOf('.');
                //string documentTypeStr = tmpString.Substring(idx + 1);
                //documentType = int.Parse(documentTypeStr);
                //tmpString = tmpString.Substring(0, idx);

                //idx = tmpString.LastIndexOf('.');
                //direction = tmpString.Substring(idx + 1);
                //if (string.Equals(direction, "outbound", StringComparison.OrdinalIgnoreCase))
                //    direction = "SEND";
                //else if (string.Equals(direction, "inbound", StringComparison.OrdinalIgnoreCase))
                //    direction = "RECEIVE";
                //else
                //    throw new InvalidOperationException("Invalid direction" + direction);
                //tmpString = tmpString.Substring(0, idx);
                //tmpString = tmpString.Replace(".complex", "");

                //domainName = tmpString;
            }
        }
        public string FileName { get; set; }
        public TransformPlug Map { get; set; }

        public string OrgName { get { return orgName; } }
        public string Direction { get { return direction; } }
        public int DocumentType { get { return documentType; } }

        public MapDetail(string currentFileName, string folderName, TransformPlug transformPlug, string specCertType)
        {
            this.FileName = currentFileName;
            this.FolderName = folderName;
            this.Map = transformPlug;

            if (folderName.StartsWith("GCommerce", StringComparison.OrdinalIgnoreCase) || folderName.StartsWith("Maps", StringComparison.OrdinalIgnoreCase))
                return;

            Console.WriteLine("Extracting metadata from {0}", folderName);

            // Try to get documentType, direction and domainName from folder
            // If documentType is not part of folder name then use map.SourceLocation or TargetLocation 
            // based on direction and decide documentType
            string tmpString = folderName;
            int idx;
            idx = tmpString.LastIndexOf('.');
            string documentTypeStr = tmpString.Substring(idx + 1);
            documentType = -1;
            if (int.TryParse(documentTypeStr, out documentType))
            {
                tmpString = tmpString.Substring(0, idx);
            }
            else
                documentType = -1;

            idx = tmpString.LastIndexOf('.');
            direction = tmpString.Substring(idx + 1);
            if (string.Equals(direction, "outbound", StringComparison.OrdinalIgnoreCase)
                || string.Equals(direction, "oubound", StringComparison.OrdinalIgnoreCase))
                direction = "SEND";
            else if (string.Equals(direction, "inbound", StringComparison.OrdinalIgnoreCase))
                direction = "RECEIVE";
            else
                throw new InvalidOperationException("Invalid direction" + direction);
            tmpString = tmpString.Substring(0, idx);
            tmpString = tmpString.Replace(".complex", "");

            domainName = tmpString;
            orgName = OrganizationMetadata.GetOrganizationName(domainName);
            if (string.IsNullOrWhiteSpace(orgName))
                throw new InvalidOperationException(string.Format("Cannot find organization name for domain {0}", domainName));

            if (documentType == -1 && !string.IsNullOrEmpty(Map.SourceLocation) && !string.IsNullOrEmpty(Map.TargetLocation))
            {
                string location = null;
                if (direction == "SEND")
                {
                    location = Map.TargetLocation;
                }
                else
                {
                    location = Map.SourceLocation;
                }

                location = location.ToLower();

                if (location.Contains(".t810"))
                    documentType = 810;
                else
                    if (location.Contains(".t850"))
                        documentType = 850;
                    else if (location.Contains(".t856"))
                        documentType = 856;
                    else
                        if (specCertType == "xml")
                        {
                            if (location.Contains("invoice"))
                                documentType = 810;
                            else if (location.Contains("purchaseorder"))
                                documentType = 850;
                            else if (location.Contains("shipment"))
                                documentType = 856;
                        }
                        else if (specCertType == "flatfile")
                        {
                            documentTypeStr = location.Substring(location.Length - 3);
                            if (int.TryParse(documentTypeStr, out documentType) == false)
                                documentType = -1;

                            if (documentType == -1)
                            {
                                // try to get document type from file name
                                string tmpStr = Path.GetFileNameWithoutExtension(FileName);
                                documentTypeStr = tmpStr.Substring(tmpStr.Length - 3);
                                if (int.TryParse(documentTypeStr, out documentType) == false)
                                    documentType = -1;
                            }
                        }

            }
        }

        public Dictionary<string, List<string>> MapCovastToX12()
        {
            if (Map == null)
                throw new InvalidOperationException("Map does not exist");

            Dictionary<string, ITransformLink> links = new Dictionary<string, ITransformLink>();
            foreach (ITransformGroup transformGroup in Map.Facets)
            {
                foreach (ITransformLink link in transformGroup.Links)
                    links.Add(link.Name, link);
            }

            bool isSourceCovast = true;
            if (FileName.ToLower().Contains("inbound"))
                isSourceCovast = false;

            Dictionary<string, string> formulaSource = new Dictionary<string, string>();
            Dictionary<string, List<string>> covastToX12Mapping = new Dictionary<string, List<string>>();

            // Get formula source first
            foreach (ITransformLink link in links.Values)
            {
                IReferenceableElement toRefElement = link.Target;
                IReferenceableElement fromRefElement = link.Source;

                if (!isSourceCovast)
                {
                    toRefElement = link.Source;
                    fromRefElement = link.Target;
                }

                if (fromRefElement.ReferenceType == ReferenceType.Document && toRefElement.ReferenceType == ReferenceType.Formula)
                {
                    if (formulaSource.ContainsKey(toRefElement.Name) == true)
                        Console.WriteLine("Formula source {0} already exist. Existing: {1}, New: {2}", fromRefElement.Name, formulaSource[toRefElement.Name], fromRefElement.Name);
                    else
                        formulaSource.Add(toRefElement.Name, fromRefElement.Name);
                }
            }

            foreach (ITransformLink link in links.Values)
            {
                IReferenceableElement toRefElement = link.Target;
                IReferenceableElement fromRefElement = link.Source;

                if (!isSourceCovast)
                {
                    toRefElement = link.Source;
                    fromRefElement = link.Target;
                }

                if (fromRefElement.ReferenceType == ReferenceType.Document && toRefElement.ReferenceType == ReferenceType.Document)
                {
                    AddEntry(covastToX12Mapping, fromRefElement.Name, toRefElement.Name);
                }
                else
                    if (fromRefElement.ReferenceType == ReferenceType.Formula && toRefElement.ReferenceType == ReferenceType.Document)
                    {
                        if (formulaSource.ContainsKey(fromRefElement.Name) == false)
                            Console.WriteLine("Formula source for {0} does not exist", fromRefElement.Name);
                        else
                        {
                            AddEntry(covastToX12Mapping, formulaSource[fromRefElement.Name], toRefElement.Name);
                        }
                    }
            }

            return covastToX12Mapping;
        }

        private void AddEntry(Dictionary<string, List<string>> covastToX12Mapping, string source, string target)
        {
            List<string> targetList = null;
            if (!covastToX12Mapping.TryGetValue(source, out targetList))
            {
                targetList = new List<string>();
                covastToX12Mapping.Add(source, targetList);
            }

            targetList.Add(target);
        }
    }
}
