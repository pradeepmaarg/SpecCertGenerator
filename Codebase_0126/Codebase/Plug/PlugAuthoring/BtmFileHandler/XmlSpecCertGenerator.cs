using Maarg.Fatpipe.Plug.DataModel;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maarg.Fatpipe.Plug.Authoring.BtmFileHandler
{
    public class XmlLinkInfo
    {
        public XmlLinkInfo() { }
        public XmlLinkInfo(string linkName) { this.LinkName = linkName; }

        public string LinkName { get; set; }
        public bool IsOptional { get; set; }
        public bool IsLeafNode { get; set; }
    }


    public class XmlSpecCertGenerator
    {
        private const int StartRowIndex = 10;
        private const int RootNodeNameRowIndex = 7;
        private const int RootNodeNameColumnIndex = 3;

        private const int SendMandatoryColumnIndex = 13;
        private const int ReceiveMandatoryColumnIndex = 20;
        private const int DataTypeColumnIndex = 7;
        private const int MinMaxColumnIndex = 8;
        private const int StartLevelColumnIndex = 1;
        private const int LastColumnIndex = 29;
        private const string TemplateFile = @"BtmFileHandler\XmlSpecCertTemplate.xlsx";

        public SpecCertGenerationResult GenerateSpecCert(MapDetail mapDetail)
        {
            SpecCertGenerationResult result = new SpecCertGenerationResult();

            string currentDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SpecCertGenerator)).Location);
            string specCertName = string.Format("{0} - Spec Cert - XML - {1} - {2}.xlsx", mapDetail.OrgName, mapDetail.DocumentType, mapDetail.Direction.ToLower());
            string specCertPath = Path.Combine(currentDir, specCertName);
            bool useSource = string.Equals(mapDetail.Direction, "receive", StringComparison.OrdinalIgnoreCase);

            string templateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplateFile);

            if (File.Exists(specCertPath))
                File.Delete(specCertPath);

            using (ExcelPackage pck = new ExcelPackage(new FileInfo(templateFile)))
            {
                ExcelWorkbook workBook = pck.Workbook;
                ExcelWorksheet specCertWorksheet = workBook.Worksheets[1];
                specCertWorksheet.Name = string.Format("{0} {1} Spec Worksheet", mapDetail.DocumentType, mapDetail.Direction.ToUpper());

                Dictionary<string, ITransformLink> links = new Dictionary<string, ITransformLink>();
                Dictionary<string, ITransformLink> fromLinks = new Dictionary<string, ITransformLink>();
                Dictionary<string, ITransformLink> toLinks = new Dictionary<string, ITransformLink>();
                Dictionary<string, IFormula> formulas = new Dictionary<string, IFormula>();
                Dictionary<string, XmlLinkInfo> xmlLinks = new Dictionary<string, XmlLinkInfo>();

                IReferenceableElement refElement;
                string rootNodeName = null;

                foreach (ITransformGroup transformGroup in mapDetail.Map.Facets)
                {
                    foreach (ITransformLink link in transformGroup.Links)
                    {
                        links.Add(link.Name, link);
                        if (link.Source != null && link.Source.ReferenceType == ReferenceType.Formula && !fromLinks.ContainsKey(link.Source.Name))
                            fromLinks.Add(link.Source.Name, link);
                        if (link.Target != null && link.Target.ReferenceType == ReferenceType.Formula && !toLinks.ContainsKey(link.Target.Name))
                            toLinks.Add(link.Target.Name, link);

                        refElement = useSource ? link.Source : link.Target;
                        //Skip first part of path
                        string path = refElement.Name.Substring(refElement.Name.IndexOf("->") + 2);

                        if (refElement.ReferenceType == ReferenceType.Document && !xmlLinks.ContainsKey(path))
                        {
                            if (rootNodeName == null)
                            {
                                rootNodeName = refElement.Name.Substring(0, refElement.Name.IndexOf("->"));
                                specCertWorksheet.Cells[RootNodeNameRowIndex, RootNodeNameColumnIndex].Value = rootNodeName;
                            }

                            xmlLinks.Add(path, new XmlLinkInfo(link.Name));
                        }
                    }

                    foreach (IFormula formula in transformGroup.Formulas)
                        formulas.Add(formula.Name, formula);
                }

                // TODO: Check done for receive only
                // Set IsOptional to false if formula is not of type Logical*
                if (useSource)
                {
                    foreach (XmlLinkInfo xmlLink in xmlLinks.Values)
                    {
                        ITransformLink link = links[xmlLink.LinkName];

                        if (link.Target.ReferenceType == ReferenceType.Formula)
                        {
                            IFormula formula = formulas[link.Target.Name];
                            if (formula.FormulaType == FormulaType.LogicalString
                                || formula.FormulaType == FormulaType.LogicalExistence)
                            {
                                xmlLink.IsOptional = true;
                            }
                        }
                    }
                }

                result.PathsUsed = new List<string>();
                int row = StartRowIndex;
                int mandatoryColumnIndex = useSource ? ReceiveMandatoryColumnIndex : SendMandatoryColumnIndex;
                string[] prevParts = null;
                List<string> paths = xmlLinks.Keys.ToList();
                paths.Sort();
                foreach (string path in paths)
                {
                    XmlLinkInfo xmlLink = xmlLinks[path];

                    string[] parts = path.Split(new string[] {"->"}, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 5)
                    {
                        throw new NotSupportedException(string.Format("More than 5 levels deep xml elements are not supported. Path: {0}", path));
                    }

                    int level = 0;
                    if (prevParts != null)
                    {
                        while (level < prevParts.Length 
                            && level < parts.Length
                            && prevParts[level] == parts[level])
                            level++;
                    }

                    prevParts = parts;

                    while (level < parts.Length)
                    {
                        bool highlightRow = level < parts.Length - 1 || xmlLinks.Count(entry => entry.Key.StartsWith(path + "->")) != 0;
                        AddRow(specCertWorksheet, row, level + 1, parts[level], mandatoryColumnIndex, highlightRow, xmlLink.IsOptional || level < parts.Length - 1);
                        level++;
                        row++;
                    }

                    result.PathsUsed.Add(path);
                }

                // Add errors to spec cert worksheet
                //AddErrorsWorksheet(workBook, result);

                if (result.PathsUsed.Count != 0)
                {
                    pck.SaveAs(new FileInfo(specCertPath));
                    result.SpecCertGenerated = true;
                }
            }

            result.SpecCertPath = specCertPath;

            return result;
        }

        public void AddRow(ExcelWorksheet specCertWorksheet, int row, int elementColumnIndex, string elementName, int mandatoryColumnIndex, bool nonLeafNode, bool isOptional)
        {
            specCertWorksheet.InsertRow(row, 1);
            specCertWorksheet.Cells[row, elementColumnIndex].Value = elementName;

            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Font.Size = 9;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            if (nonLeafNode)
            {
                specCertWorksheet.Cells[row, 1, row, SendMandatoryColumnIndex - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                specCertWorksheet.Cells[row, 1, row, SendMandatoryColumnIndex - 1].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
            }
            else // For leaf node set data type as AN
            {
                specCertWorksheet.Cells[row, DataTypeColumnIndex].Value = "AN";
            }

            if (isOptional == false)
            {
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Value = "y";
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Style.Fill.PatternType = ExcelFillStyle.Solid;
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Style.Fill.BackgroundColor.SetColor(Color.Green);
            }
        }
    }
}
