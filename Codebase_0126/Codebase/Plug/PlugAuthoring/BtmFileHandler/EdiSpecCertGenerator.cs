using Maarg.Fatpipe.Plug.DataModel;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maarg.Fatpipe.Plug.Authoring.BtmFileHandler
{
    public class EdiSpecCertGenerator
    {
        private const int StartRowIndex = 8;
        private const int SendMandatoryColumnIndex = 15;
        private const int ReceiveMandatoryColumnIndex = 22;
        private const int SegmentNameColumnIndex = 3;
        private const int DataSegmentNameColumnIndex = 4;
        private const int DataTypeColumnIndex = 7;
        private const int MinMaxColumnIndex = 8;
        private const int EnumCodeColumnIndex = 9;
        private const int EnumValueColumnIndex = 10;
        private const int ExistsCheckNoteColumnIndex = 11;
        private const int X12PathColumnIndex = 38;
        private const int CovastPathColumnIndex = 39;
        private const string TemplateFile_810 = @"BtmFileHandler\810_SpecCertTemplate.xlsx";
        private const string TemplateFile_850 = @"BtmFileHandler\850_SpecCertTemplate.xlsx";
        private const string TemplateFile_856 = @"BtmFileHandler\856_SpecCertTemplate.xlsx";
        private const string CovastToX12Map_810_Inbound = @"BtmFileHandler\810_Inbound_CovastToX12Map.csv";
        private const string CovastToX12Map_810_Outbound = @"BtmFileHandler\810_Outbound_CovastToX12Map.csv";
        private const string CovastToX12Map_850_Inbound = @"BtmFileHandler\850_Inbound_CovastToX12Map.csv";
        private const string CovastToX12Map_850_Outbound = @"BtmFileHandler\850_Outbound_CovastToX12Map.csv";
        private const string CovastToX12Map_856_Inbound = @"BtmFileHandler\856_Inbound_CovastToX12Map.csv";
        private const string CovastToX12Map_856_Outbound = @"BtmFileHandler\856_Outbound_CovastToX12Map.csv";

        private Dictionary<string, List<string>> Map_810_Inbound;
        private Dictionary<string, List<string>> Map_810_Outbound;
        private Dictionary<string, List<string>> Map_850_Inbound;
        private Dictionary<string, List<string>> Map_850_Outbound;
        private Dictionary<string, List<string>> Map_856_Inbound;
        private Dictionary<string, List<string>> Map_856_Outbound;
        private bool mappingsAvailable = false;
        private List<string> x12Paths = null;

        public SpecCertGenerationResult GenerateSpecCert(MapDetail mapDetail)
        {
            SpecCertGenerationResult result = new SpecCertGenerationResult();
            if (!mappingsAvailable)
            {
                // Not thread safe, but we don't really need it right now
                ReadCovastToX12Maps();
            }

            string currentDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SpecCertGenerator)).Location);
            //string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string specCertName = string.Format("{0} - Spec Cert - 4010 - {1} - {2}.xlsx", mapDetail.OrgName, mapDetail.DocumentType, mapDetail.Direction.ToLower());
            string specCertPath = Path.Combine(currentDir, specCertName);
            bool useSource = string.Equals(mapDetail.Direction, "receive", StringComparison.OrdinalIgnoreCase);

            Dictionary<string, List<string>> covastToX12Map;
            string templateFile = string.Empty;
            switch (mapDetail.DocumentType)
            {
                case 810:
                    templateFile = TemplateFile_810;
                    if (useSource)
                        covastToX12Map = Map_810_Inbound;
                    else
                        covastToX12Map = Map_810_Outbound;
                    break;

                case 850:
                    templateFile = TemplateFile_850;
                    if (useSource)
                        covastToX12Map = Map_850_Inbound;
                    else
                        covastToX12Map = Map_850_Outbound;
                    break;

                case 856:
                    templateFile = TemplateFile_856;
                    if (useSource)
                        covastToX12Map = Map_856_Inbound;
                    else
                        covastToX12Map = Map_856_Outbound;
                    break;

                default:
                    result.Errors.Add(string.Format("Spec cert generation for document type {0} not supported", mapDetail.DocumentType));
                    return result;
            }

            templateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateFile);

            if (File.Exists(specCertPath))
                File.Delete(specCertPath);

            //using (StreamReader streamReader = new StreamReader(templateFile))
            //using (ExcelPackage pck = new ExcelPackage(streamReader.BaseStream))
            using (ExcelPackage pck = new ExcelPackage(new FileInfo(templateFile)))
            {
                ExcelWorkbook workBook = pck.Workbook;
                ExcelWorksheet specCertWorksheet = workBook.Worksheets[1];
                specCertWorksheet.Name = string.Format("{0} {1} Spec Worksheet", mapDetail.DocumentType, mapDetail.Direction.ToUpper());

                Dictionary<string, ITransformLink> links = new Dictionary<string, ITransformLink>();
                Dictionary<string, ITransformLink> fromLinks = new Dictionary<string, ITransformLink>();
                Dictionary<string, ITransformLink> toLinks = new Dictionary<string, ITransformLink>();
                Dictionary<string, IFormula> formulas = new Dictionary<string, IFormula>();

                // Check if version is supported
                // Take any link and check source/target name
                IList<ITransformLink> refLinks = mapDetail.Map.Facets[0].Links;
                IReferenceableElement refElement = null;

                for (int i = 0; i < refLinks.Count; i++)
                {
                    if (useSource)
                        refElement = mapDetail.Map.Facets[0].Links[i].Source;
                    else
                        refElement = mapDetail.Map.Facets[0].Links[i].Target;

                    if (refElement.ReferenceType == ReferenceType.Document)
                    {
                        break;
                    }
                }

                if (refElement.Name.StartsWith("ASC_X12_810_004_010_DEFAULT_X") == false
                    && refElement.Name.StartsWith("ASC_X12_850_004_010_DEFAULT_X") == false
                    && refElement.Name.StartsWith("ASC_X12_856_004_010_DEFAULT_X") == false)
                    throw new NotSupportedException(string.Format("Invalid root (version) name {0}. Valid value is ASC_X12_810|850|856_004_010_DEFAULT_X", refElement.Name.Substring(0, refElement.Name.IndexOf("->"))));

                foreach (ITransformGroup transformGroup in mapDetail.Map.Facets)
                {
                    foreach (ITransformLink link in transformGroup.Links)
                    {
                        links.Add(link.Name, link);
                        if (link.Source != null && link.Source.ReferenceType == ReferenceType.Formula && !fromLinks.ContainsKey(link.Source.Name))
                            fromLinks.Add(link.Source.Name, link);
                        if (link.Target != null && link.Target.ReferenceType == ReferenceType.Formula && !toLinks.ContainsKey(link.Target.Name))
                            toLinks.Add(link.Target.Name, link);
                    }

                    foreach (IFormula formula in transformGroup.Formulas)
                        formulas.Add(formula.Name, formula);
                }

                // Get all enum values
                // Enum values are determined as 
                // Document(1) => One or more Formula (Equal) => Logical Or formula => Document(2)
                // allEnumValues store first Document(1), Document(2), enum values
                Dictionary<string, Dictionary<string, List<string>>> allEnumValues = new Dictionary<string, Dictionary<string, List<string>>>();
                foreach (ITransformLink link in links.Values)
                {
                    if (useSource == false)
                    {
                        if (link.Source.ReferenceType == ReferenceType.Document && link.Target.ReferenceType == ReferenceType.Formula)
                        {
                            IFormula formula = formulas[link.Target.Name];

                            // In case of enum formulas it's possible that scripting formula is first applied to convert enum value
                            // If that is the case ignore scripting formula and move to next formula
                            if (formula.Parameters != null && formula.FormulaType == FormulaType.Scripting)
                            {
                                ITransformLink equalLink = fromLinks[formula.Name];
                                if (equalLink.Target != null && equalLink.Target.ReferenceType == ReferenceType.Formula)
                                {
                                    string equalFormulaName = equalLink.Target.Name;
                                    formula = formulas[equalFormulaName];
                                }
                            }

                            if (formula.Parameters != null && formula.FormulaType == FormulaType.Equality)
                            {
                                // Assumption: only 1 enum value and only 1 target formula exist
                                string enumValue = null;
                                foreach (IParameter parameter in formula.Parameters)
                                {
                                    if (parameter.Reference.ReferenceType == ReferenceType.Literal)
                                    {
                                        if(enumValue != null)
                                            throw new NotSupportedException("Multiple enum values in 1 formula");
                                        enumValue = parameter.Reference.Name;
                                    }
                                }

                                // If there are enum values check what's the target of these values
                                if (!string.IsNullOrEmpty(enumValue))
                                {
                                    ITransformLink equalLink = fromLinks[formula.Name];
                                    if(equalLink.Target != null && equalLink.Target.ReferenceType == ReferenceType.Formula)
                                    {
                                        string targetFormulaName = equalLink.Target.Name;
                                        IFormula targetFormula = formulas[targetFormulaName];

                                        // Check if the equal link points to logical or formula, if it is then the target link of that will give us the document
                                        if (targetFormula.FormulaType == FormulaType.LogicalOr)
                                        {
                                            string targetDocumentName = null;

                                            ITransformLink logicalOrLink = fromLinks[targetFormulaName];
                                            if (logicalOrLink.Target != null && logicalOrLink.Target.ReferenceType == ReferenceType.Document)
                                            {
                                                targetDocumentName = logicalOrLink.Target.Name;

                                                if (!string.IsNullOrEmpty(targetDocumentName))
                                                {
                                                    Dictionary<string, List<string>> nodeEnumValues;
                                                    if (!allEnumValues.TryGetValue(link.Source.Name, out nodeEnumValues))
                                                    {
                                                        nodeEnumValues = new Dictionary<string, List<string>>();
                                                        allEnumValues.Add(link.Source.Name, nodeEnumValues);
                                                    }

                                                    List<string> enumValues = null;
                                                    if (!nodeEnumValues.TryGetValue(targetDocumentName, out enumValues))
                                                    {
                                                        enumValues = new List<string>();
                                                        nodeEnumValues.Add(targetDocumentName, enumValues);
                                                    }
                                                    enumValues.Add(enumValue);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (ITransformGroup transformGroup in mapDetail.Map.Facets)
                {
                    foreach (ITransformLink link in transformGroup.Links)
                    {
                        IReferenceableElement toRefElement = link.Target;
                        IReferenceableElement fromRefElement = link.Source;

                        if (useSource)
                        {
                            toRefElement = link.Source;
                            fromRefElement = link.Target;
                        }

                        if (toRefElement.ReferenceType == ReferenceType.Document)
                        {
                            try
                            {
                                x12Paths = null;
                                // Check if source has enum values
                                List<string> enumValues = GetEnumValues(link.Source.Name, toRefElement, allEnumValues);
                                if(enumValues != null)
                                {
                                    AddEnumValues(specCertWorksheet, covastToX12Map, toRefElement.Name, useSource, enumValues);
                                }
                                else
                                {
                                    if (fromRefElement.ReferenceType == ReferenceType.Formula
                                        && (formulas[fromRefElement.Name].FormulaType == FormulaType.LogicalExistence
                                            || formulas[fromRefElement.Name].FormulaType == FormulaType.LogicalString
                                            || formulas[fromRefElement.Name].FormulaType == FormulaType.GreaterThan))
                                    {
                                        AddExistsCheckNote(specCertWorksheet, covastToX12Map, toRefElement.Name, useSource);
                                        //SetMandatoryValue(specCertWorksheet, covastToX12Map, toRefElement.Name, useSource, "n");
                                    }
                                    else
                                    {
                                        // In case of looping do not set the mandatory flag for the segment
                                        if (!(fromRefElement.ReferenceType == ReferenceType.Formula
                                            && formulas[fromRefElement.Name].FormulaType == FormulaType.Looping))
                                        {
                                            SetMandatoryValue(specCertWorksheet, covastToX12Map, toRefElement.Name, useSource, "y");
                                        }
                                        //else
                                            //SetMandatoryValue(specCertWorksheet, covastToX12Map, toRefElement.Name, useSource, "n");
                                    }
                                }

                                if (x12Paths != null)
                                {
                                    result.PathsUsed.AddRange(x12Paths);
                                }
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add(ex.Message);
                            }
                        }
                    }
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

        private List<string> GetEnumValues(string sourceName, IReferenceableElement toRefElement, Dictionary<string, Dictionary<string, List<string>>> allEnumValues)
        {
            List<string> enumValues = null;

            Dictionary<string, List<string>> documentEnumValues;
            if (allEnumValues.TryGetValue(sourceName, out documentEnumValues))
            {
                string parentName = toRefElement.Name.Substring(0, toRefElement.Name.LastIndexOf("->"));
                foreach (string documentName in documentEnumValues.Keys)
                {
                    if (string.Equals(documentName, parentName, StringComparison.OrdinalIgnoreCase))
                    {
                        enumValues = documentEnumValues[documentName];
                        break;
                    }
                }
            }

            return enumValues;
        }

        private void AddErrorsWorksheet(ExcelWorkbook workBook, SpecCertGenerationResult result)
        {
            if (result.Errors.Count > 0)
            {
                ExcelWorksheet errorsWorksheet = workBook.Worksheets.Add("Errors");

                int row = 1;
                
                errorsWorksheet.Cells[row, 1].Value = "Errors encountered during spec cert generation";
                row++;

                foreach (string error in result.Errors)
                {
                    errorsWorksheet.Cells[row, 1].Value = error;
                    row++;
                }
            }
        }

        private void ReadCovastToX12Maps()
        {
            Map_810_Inbound = new Dictionary<string, List<string>>();
            Map_810_Outbound = new Dictionary<string, List<string>>();
            Map_850_Inbound = new Dictionary<string, List<string>>();
            Map_850_Outbound = new Dictionary<string, List<string>>();
            Map_856_Inbound = new Dictionary<string, List<string>>();
            Map_856_Outbound = new Dictionary<string, List<string>>();

            ReadCovastToX12Map(Map_810_Inbound, CovastToX12Map_810_Inbound);
            ReadCovastToX12Map(Map_810_Outbound, CovastToX12Map_810_Outbound);
            ReadCovastToX12Map(Map_850_Inbound, CovastToX12Map_850_Inbound);
            ReadCovastToX12Map(Map_850_Outbound, CovastToX12Map_850_Outbound);
            ReadCovastToX12Map(Map_856_Inbound, CovastToX12Map_856_Inbound);
            ReadCovastToX12Map(Map_856_Outbound, CovastToX12Map_856_Outbound);

            mappingsAvailable = true;
        }

        private void ReadCovastToX12Map(Dictionary<string, List<string>> map, string mapFile)
        {
            string[] mappings = File.ReadAllLines(mapFile);
            foreach (string mapping in mappings)
            {
                string[] tmpArr = mapping.Split(',');
                AddEntry(map, tmpArr[0], tmpArr[1]);
            }
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

        private void AddExistsCheckNote(ExcelWorksheet specCertWorksheet, Dictionary<string, List<string>> covastToX12Map, string path, bool receiveSpecCert)
        {
            List<int> rows = GetRowNumber(specCertWorksheet, covastToX12Map, path);

            foreach (int row in rows)
            {
                if (string.IsNullOrWhiteSpace((string)specCertWorksheet.Cells[row, DataSegmentNameColumnIndex].Value) == false)
                {
                    specCertWorksheet.Cells[row, ExistsCheckNoteColumnIndex].Value = "Mapped if exists";
                    specCertWorksheet.Cells[row, CovastPathColumnIndex].Value = path;
                }
            }
        }

        private void SetMandatoryValue(ExcelWorksheet specCertWorksheet, Dictionary<string, List<string>> covastToX12Map, string path, bool receiveSpecCert, string value)
        {
            List<int> rows = GetRowNumber(specCertWorksheet, covastToX12Map, path);
            int mandatoryColumnIndex = SendMandatoryColumnIndex;

            if (receiveSpecCert)
                mandatoryColumnIndex = ReceiveMandatoryColumnIndex;

            foreach (int row in rows)
            {
                // Set value "y" only for leaf nodes
                if (string.IsNullOrWhiteSpace((string)specCertWorksheet.Cells[row, SegmentNameColumnIndex].Value) == true)
                {
                    specCertWorksheet.Cells[row, mandatoryColumnIndex].Value = value;
                    specCertWorksheet.Cells[row, CovastPathColumnIndex].Value = path;
                }

                // If this is leaf node then set segment as mandatory if its not already set as non-mandatory
                // If uncommenting below lines then also uncomment SetMandatoryValue function call with "n" param
                //if (string.IsNullOrWhiteSpace((string)specCertWorksheet.Cells[row, DataSegmentNameColumnIndex].Value) == false)
                //{
                //    int segmentRow = row -1;
                //    while (segmentRow > 0)
                //    {
                //        if (string.IsNullOrWhiteSpace((string)specCertWorksheet.Cells[segmentRow, SegmentNameColumnIndex].Value) == false)
                //        {
                //            if (string.IsNullOrWhiteSpace((string)specCertWorksheet.Cells[segmentRow, mandatoryColumnIndex].Value) == true)
                //            {
                //                specCertWorksheet.Cells[segmentRow, mandatoryColumnIndex].Value = value;
                //            }

                //            break;
                //        }
                //        segmentRow--;
                //    }
                //}
            }
        }

        private void AddEnumValues(ExcelWorksheet specCertWorksheet, Dictionary<string, List<string>> covastToX12Map, string path, bool receiveSpecCert, List<string> enumValues)
        {
            List<int> rows = GetRowNumber(specCertWorksheet, covastToX12Map, path);

            int mandatoryColumnIndex = SendMandatoryColumnIndex;

            if (receiveSpecCert)
                mandatoryColumnIndex = ReceiveMandatoryColumnIndex;

            foreach (int row in rows)
            {
                int dataRow = row;
                int min, max;
                min = int.MaxValue;
                max = int.MinValue;
                foreach (string enumValue in enumValues)
                {
                    if (min > enumValue.Length)
                        min = enumValue.Length;
                    if (max < enumValue.Length)
                        max = enumValue.Length;

                    dataRow++;
                    specCertWorksheet.InsertRow(dataRow, 1);
                    specCertWorksheet.Cells[dataRow, mandatoryColumnIndex].Value = "y";
                    specCertWorksheet.Cells[dataRow, EnumCodeColumnIndex].Value = enumValue;
                    specCertWorksheet.Cells[dataRow, EnumValueColumnIndex].Value = enumValue;
                    specCertWorksheet.Cells[dataRow, 1, dataRow, CovastPathColumnIndex].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    specCertWorksheet.Cells[dataRow, 1, dataRow, CovastPathColumnIndex].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    specCertWorksheet.Cells[dataRow, 1, dataRow, CovastPathColumnIndex].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    specCertWorksheet.Cells[dataRow, 1, dataRow, CovastPathColumnIndex].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                specCertWorksheet.Cells[row, DataTypeColumnIndex].Value = "ID";
                specCertWorksheet.Cells[row, MinMaxColumnIndex].Value = string.Format("{0}/{1}", min, max);
                specCertWorksheet.Cells[row, CovastPathColumnIndex].Value = path;
            }
        }

        private List<int> GetRowNumber(ExcelWorksheet specCertWorksheet, Dictionary<string, List<string>> covastToX12Map, string covastPath)
        {
            if (!covastToX12Map.TryGetValue(covastPath, out x12Paths))
            {
                // Try searching for 4010 version
                //string covastPath40 = covastPath.Replace("_004_040_", "_004_010_");
                //if (!covastToX12Map.TryGetValue(covastPath40, out x12Paths))
                    throw new InvalidOperationException(string.Format("Covast path is mapped in btm file but no match found in X12 template. Covast path:{0}", covastPath));
            }

            List<int> rows = new List<int>();

            foreach (string x12Path in x12Paths)
            {
                int row = StartRowIndex;

                while (row < specCertWorksheet.Dimension.End.Row)
                {
                    object pathObject = specCertWorksheet.Cells[row, X12PathColumnIndex].Value;
                    if (pathObject == null || string.IsNullOrWhiteSpace(pathObject.ToString()))
                    {
                        // Ignore the enum values rows
                        pathObject = specCertWorksheet.Cells[row, EnumCodeColumnIndex].Value;
                        if (pathObject == null || string.IsNullOrWhiteSpace(pathObject.ToString()))
                            break;
                    }
                    else
                        if (string.Equals(pathObject.ToString(), x12Path, StringComparison.OrdinalIgnoreCase))
                        {
                            rows.Add(row);
                            break;
                        }
                    row++;
                }

                if (row == specCertWorksheet.Dimension.End.Row)
                    throw new InvalidOperationException(string.Format("X12 path {0} not found in template", x12Path));
            }

            return rows;
        }
    }
}
