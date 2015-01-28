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
    public class LinkInfo
    {
        public LinkInfo() { }
        public LinkInfo(string linkName) { this.LinkName = linkName; }

        public string LinkName { get; set; }
        public bool IsOptional { get; set; }
    }

    public class FlatFileSpecCertGenerator
    {
        private const int ElementDelimiterRowIndex = 6;
        private const int SegmentDelimiterRowIndex = 7;
        private const int StartRowIndex = 11;
        private const int ElementDelimiterColumnIndex = 3;
        private const int SegmentDelimiterColumnIndex = 3;

        private const int LoopColumnIndex = 1;
        private const int SegmentColumnIndex = 2; // Used only for segment - to set the name
        private const int SegmentCodeColumnIndex = 9; // We want 3 letter code
        private const int DataElementTagColumnIndex = 3;
        private const int DataElementNameColumnIndex = 3; // No element name for flat file
        private const int DataTypeColumnIndex = 5;
        private const int MinMaxColumnIndex = 6;

        private const int SendMandatoryColumnIndex = 12;

        private const int ReceiveMandatoryColumnIndex = 19;

        private const int LastColumnIndex = 29;
        private const string TemplateFile = @"BtmFileHandler\FlatFileSpecCertTemplate.xlsx";

        //TODO: Take this as command line parameter
        private const string DefaultElementDelimiter = "124"; // |
        private const string DefaultSegmentDelimiter = "13 10"; // \r\n

        public SpecCertGenerationResult GenerateSpecCert(MapDetail mapDetail)
        {
            SpecCertGenerationResult result = new SpecCertGenerationResult();

            string currentDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SpecCertGenerator)).Location);
            string specCertName = string.Format("{0} - Spec Cert - Flat File - {1} - {2}.xlsx", mapDetail.OrgName, mapDetail.DocumentType, mapDetail.Direction.ToLower());
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
                Dictionary<string, LinkInfo> xmlLinks = new Dictionary<string, LinkInfo>();

                IReferenceableElement refElement;
                string rootNodeName = null;

                specCertWorksheet.Cells[ElementDelimiterRowIndex, ElementDelimiterColumnIndex].Value = DefaultElementDelimiter;
                specCertWorksheet.Cells[SegmentDelimiterRowIndex, SegmentDelimiterColumnIndex].Value = DefaultSegmentDelimiter;

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

                        if (refElement.Name.Contains("->") == false)
                            continue;

                        //Skip first part of path
                        string path = refElement.Name.Substring(refElement.Name.IndexOf("->") + 2);

                        if (refElement.ReferenceType == ReferenceType.Document && !xmlLinks.ContainsKey(path))
                        {
                            xmlLinks.Add(path, new LinkInfo(link.Name));
                        }
                    }

                    foreach (IFormula formula in transformGroup.Formulas)
                        formulas.Add(formula.Name, formula);
                }

                // TODO: Check done for receive only
                // Set IsOptional to false if formula is not of type Logical*
                if (useSource)
                {
                    foreach (LinkInfo xmlLink in xmlLinks.Values)
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
                List<string> paths = xmlLinks.Keys.ToList();
                paths.Sort();
                string lastIterationSegmentName = null;
                foreach (string path in paths)
                {
                    LinkInfo link = xmlLinks[path];

                    string[] partsTemp = path.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> parts = partsTemp.ToList();
                    parts.Remove("Loop");

                    // Only loop name is ignored as loop name is added on the segment row
                    if (parts.Count <= 1)
                        continue;

                    if (parts.Count > 3)
                    {
                        throw new NotSupportedException(string.Format("More than 5 levels deep xml elements are not supported. Path: {0}", path));
                    }

                    string loopName = parts[0].Contains("Loop") ? parts[0] : string.Empty; // Loop name is optional
                    string segmentName = string.IsNullOrEmpty(loopName) ? parts[0] : parts[1];
                    // Some maps may have mapping only for segments (e.g. Header, BodyLoop etc.) that means
                    // data element is optional
                    string dataElementName = null;
                    int index = 2;
                    if (string.IsNullOrEmpty(loopName))
                        index = 1;

                    if (parts.Count > index)
                        dataElementName = parts[index];

                    // If segment changed then add segment row
                    if(segmentName.Equals(lastIterationSegmentName, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        AddSegmentRow(specCertWorksheet, row, loopName, segmentName, mandatoryColumnIndex, link.IsOptional || !string.IsNullOrEmpty(dataElementName));
                        lastIterationSegmentName = segmentName;
                        row++;
                    }

                    if (string.IsNullOrEmpty(dataElementName) == false)
                    {
                        AddDataElementRow(specCertWorksheet, row, dataElementName, mandatoryColumnIndex, link.IsOptional);
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

        public void AddSegmentRow(ExcelWorksheet specCertWorksheet, int row, string loopName, string segmentName, int mandatoryColumnIndex, bool isOptional)
        {
            specCertWorksheet.InsertRow(row, 1);
            specCertWorksheet.Cells[row, LoopColumnIndex].Value = loopName;
            specCertWorksheet.Cells[row, SegmentCodeColumnIndex].Value = segmentName;
            specCertWorksheet.Cells[row, SegmentColumnIndex].Value = segmentName;

            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Font.Size = 9;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            specCertWorksheet.Cells[row, 1, row, SendMandatoryColumnIndex - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            specCertWorksheet.Cells[row, 1, row, SendMandatoryColumnIndex - 1].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);

            if (isOptional == false)
            {
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Value = "y";
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Style.Fill.PatternType = ExcelFillStyle.Solid;
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Style.Fill.BackgroundColor.SetColor(Color.Green);
            }
        }

        public void AddDataElementRow(ExcelWorksheet specCertWorksheet, int row, string dataElementName, int mandatoryColumnIndex, bool isOptional)
        {
            specCertWorksheet.InsertRow(row, 1);
            specCertWorksheet.Cells[row, DataElementNameColumnIndex].Value = dataElementName;
            specCertWorksheet.Cells[row, DataElementTagColumnIndex].Value = dataElementName;

            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Font.Size = 9;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            specCertWorksheet.Cells[row, 1, row, LastColumnIndex].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            specCertWorksheet.Cells[row, DataTypeColumnIndex].Value = "AN";

            if (isOptional == false)
            {
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Value = "y";
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Style.Fill.PatternType = ExcelFillStyle.Solid;
                specCertWorksheet.Cells[row, mandatoryColumnIndex].Style.Fill.BackgroundColor.SetColor(Color.Green);
            }
        }
    }
}
