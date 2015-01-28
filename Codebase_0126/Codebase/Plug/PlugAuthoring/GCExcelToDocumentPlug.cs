using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using Maarg.Fatpipe.Plug.DataModel;
using OfficeOpenXml;
using Maarg.Contracts.GCValidate;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public abstract class SchemaRow
    {
        public string MandatoryCode { get; set; }
        public string DataType { get; set; }
        public string MinMaxLength { get; set; }
        public string EnumCode { get; set; }
        public string EnumName { get; set; }
        public string EnumFlag { get; set; }
        public string IgnoreFlag { get; set; }
        public string MandatoryFlag { get; set; }
        public string Contingencies { get; set; }
        public string ContingencyType { get; set; }
        public bool IsTriggerField { get; set; }

        public abstract string GetDataElementTag();
    };

    public abstract class GCExcelToDocumentPlug
    {
        private const string TriggerFieldValue = "TRIGGER";

        protected int MandatoryCodeIndex;
        protected int DataTypeIndex;
        protected int MinMaxIndex;
        protected int EnumCodeIndex;
        protected int EnumNameIndex;

        protected int TriggerFieldIndex;

        protected int MandatoryFlagIndex;
        protected int EnumFlagIndex;
        protected int ContingenciesIndex;
        protected int ContingencyTypeIndex;
        protected int IgnoreFlagIndex;

        private Dictionary<int, string> ContingencyValue;
        private Dictionary<int, string> ContingencyPath;
        private Dictionary<int, List<Contingency>> PendingContingencies;

        public SpecCertValidationResult SpecCertValidationResult { get; set; }

        public GCExcelToDocumentPlug()
        {
            SpecCertValidationResult = new SpecCertValidationResult();
            TriggerFieldIndex = -1;

            ContingencyValue = new Dictionary<int, string>();
            PendingContingencies = new Dictionary<int, List<Contingency>>();
            ContingencyPath = new Dictionary<int, string>();
        }

        public IDocumentPlug GenerateDocumentPlug(Stream stream, string tradingPartnerName, int documentType, string direction, SpecCertFileType certFileType)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            DocumentPlug documentPlug = null;

            using (ExcelPackage pck = new ExcelPackage(stream))
            {
                ExcelWorkbook workBook = pck.Workbook;
                // TODO: Add more validation
                ExcelWorksheet schemaWorksheet;

                string worksheetName = string.Format("{0} {1} Spec Worksheet", documentType, direction);

                // Special case for 'super spec'
                if (string.IsNullOrWhiteSpace(tradingPartnerName) == false
                    && string.Equals(tradingPartnerName, "Super Spec", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    worksheetName = "Segment & Element List";
                    
                    if (workBook.Worksheets[worksheetName] == null)
                        worksheetName = "Specification Worksheet";
                }

                schemaWorksheet = workBook.Worksheets[worksheetName];

                if (schemaWorksheet == null)
                {
                    AddValidationResult(ResultType.Error, -1, "N/A", string.Format("'{0}' worksheet doesn't exist", worksheetName));
                    return null;
                }

                InitializeColumnIndexes(direction);
                int row = GetStartRow(schemaWorksheet);
                documentPlug = ConstructDocumentPlug(schemaWorksheet, row);

                // TODO: Try to get source contingency row here instead of specifying -1
                foreach(int contingencyRow in PendingContingencies.Keys)
                    AddValidationResult(ResultType.Error, -1, "N/A", string.Format("Unresolved contingency. Row '{0}' does not exist", contingencyRow));
            }

            return documentPlug;
        }

        protected abstract void InitializeColumnIndexes(string direction);
        protected abstract string GetStartRowText();
        protected abstract DocumentPlug ConstructDocumentPlug(ExcelWorksheet schemaWorksheet, int startRow);
        protected abstract SchemaRow ReadRow(ExcelWorksheet schemaWorksheet, int row);
        protected abstract bool IsDataTypeRowsOver(SchemaRow schemaRow, int row);

        protected static void FillInMissingChildren(IPluglet segment)
        {
            IList<IPluglet> inputList = segment.Children;
            int origLength = inputList.Count;
            if (origLength < 2) return;

            int visitIndex = 0;

            string prefix;
            int expectedNumber = GetSequenceNumber(inputList[0].Name, out prefix);

            int visitedNodeNumber;
            int numberOfNodesVisited = 0;

            while (numberOfNodesVisited < origLength)
            {
                visitedNodeNumber = GetSequenceNumber(inputList[visitIndex].Name, out prefix);
                numberOfNodesVisited++;

                //initialize the sequence
                if (visitedNodeNumber >= 0 && expectedNumber < 0)
                {
                    expectedNumber = visitedNodeNumber;
                }

                //if a gap in sequence space is found
                if (visitedNodeNumber > expectedNumber)
                {
                    //fill in the range of numbers from expectedNumber to (visitedNodeNumber-1) 
                    for (int fillCount = 0; fillCount <= visitedNodeNumber - 1 - expectedNumber; fillCount++)
                    {
                        string newNodeName = GenerateName(prefix, expectedNumber + fillCount);

                        //more formally consturct a new node here using appropriate constructor
                        IPluglet child = new Pluglet(newNodeName, newNodeName, PlugletType.Data, null, 0, 1, true);
                        child.DataType = new X12_AnDataType("AN", -1, -1);
                        inputList.Insert(visitIndex + fillCount, child);
                        child.Parent = segment;
                    }

                    visitIndex += visitedNodeNumber - expectedNumber + 1;
                    expectedNumber = visitedNodeNumber + 1;
                }
                else
                {
                    visitIndex++;
                    if (expectedNumber >= 0) expectedNumber++;
                }
            }
        }

        protected X12BaseDataType ReadDataType(SchemaRow segmentRow, ExcelWorksheet schemaWorksheet, string segmentPath, ref int row)
        {
            List<string> optionalValues = new List<string>();
            Dictionary<string, string> allowedValues = new Dictionary<string, string>();
            Dictionary<string, Contingency> contingencies = new Dictionary<string, Contingency>();
            X12BaseDataType dataType;
            string dataTypeName = segmentRow.DataType;
            string currentTag = segmentRow.GetDataElementTag();
            int dataTypeRow = row-1;

            // In sample schema ISA016 has data type blank hence AN and default is set to AN
            if (string.IsNullOrEmpty(dataTypeName) == true)
            {
                dataTypeName = "AN";
                AddValidationResult(ResultType.Warning, dataTypeRow, GCExcelReaderHelper.GetColumnIndex(DataTypeIndex), 
                    string.Format("'{0}' segment is missing data type, treating it as AlphaNumeric", currentTag));
            }

            int minL, maxL;

            GetMinMax(segmentRow.MinMaxLength, row, out minL, out maxL);

            // add current row enum code if present
            if (string.IsNullOrWhiteSpace(segmentRow.EnumCode) == false && string.Compare(segmentRow.EnumFlag, "n", true) != 0)
            {
                AddIdValue(segmentRow, row, string.Format("{0}[{1}]", segmentPath, segmentRow.EnumCode), optionalValues, allowedValues, contingencies);
            }

            // Traverse all sub rows - these rows are mostly relevant for enums (containing enum values)
            // however sample schema shows multiple rows for other data types too
            while ((segmentRow = ReadRow(schemaWorksheet, row)) != null)
            {
                if ((string.IsNullOrEmpty(segmentRow.GetDataElementTag()) == false && string.Compare(segmentRow.GetDataElementTag(), currentTag, true) != 0)
                    || IsDataTypeRowsOver(segmentRow, row))
                    break;

                switch (dataTypeName)
                {
                    case "ID":
                        if (string.IsNullOrEmpty(segmentRow.EnumCode) == false && string.Compare(segmentRow.EnumFlag, "n", true) != 0)
                        {
                            AddIdValue(segmentRow, row, string.Format("{0}[{1}]", segmentPath, segmentRow.EnumCode), optionalValues, allowedValues, contingencies);
                        }
                        break;

                    case "DT":
                    case "AN":
                    case "N0":
                    case "N2":
                    case "TM":
                    case "R":
                        break;

                    default:
                        //AddValidationResult(ResultType.Error, row, GetColumnIndex(EnumCodeIndex), string.Format("Unknown data type {0}", dataTypeName));
                        //throw new SchemaReaderException(string.Format("Unknown data type {0} on row {1}", dataTypeName, row));
                        break;
                }
                ++row;
            }

            switch (dataTypeName)
            {
                case "ID":
                    if (allowedValues == null || allowedValues.Count == 0)
                    {
                        dataType = new X12_AnDataType(dataTypeName, minL, maxL);
                        AddValidationResult(ResultType.Warning, dataTypeRow, GCExcelReaderHelper.GetColumnIndex(EnumCodeIndex), 
                            string.Format("'{0}' segment has ID type without any values, treating it as AlphaNumeric.", currentTag));
                    }
                    else
                        dataType = new X12_IdDataType(dataTypeName, optionalValues, allowedValues, contingencies);
                    break;

                case "DT":
                    dataType = new X12_DtDataType(dataTypeName, minL, maxL);
                    break;

                case "Comp":
                case "AN":
                    dataType = new X12_AnDataType(dataTypeName, minL, maxL);
                    break;

                case "N":
                    dataType = X12_NDataType.GetDataTypeWithPrecision(0, minL, maxL);
                    break;

                case "N0":
                    dataType = X12_NDataType.GetDataTypeWithPrecision(0, minL, maxL);
                    break;

                case "N2":
                    dataType = X12_NDataType.GetDataTypeWithPrecision(2, minL, maxL);
                    break;

                case "TM":
                    dataType = new X12_TmDataType(dataTypeName, minL, maxL);
                    break;

                case "R":
                    dataType = new X12_RDataType(dataTypeName, minL, maxL);
                    break;

                default:
                    AddValidationResult(ResultType.Error, dataTypeRow, GCExcelReaderHelper.GetColumnIndex(EnumCodeIndex), string.Format("Unknown data type {0}", dataTypeName));
                    dataType = new X12_AnDataType(dataTypeName, minL, maxL); // This is to avoid compiler error.
                    //throw new SchemaReaderException(string.Format("Unknown data type {0} on row {1}", dataTypeName, row));
                    break;
            }

            return dataType;
        }

        private void ResolveContingency(int row, string crossSegmentPointer, string contingencyValue)
        {
            // First add current row value to ContingencyValue, this will be used
            // if any id type refer to this value later in the spec cert
            ContingencyValue.Add(row, contingencyValue);
            ContingencyPath.Add(row, crossSegmentPointer);

            List<Contingency> contingencies;
            if (PendingContingencies.TryGetValue(row, out contingencies) == true)
            {
                foreach (Contingency contingency in contingencies)
                {
                    if(contingency.Type == ContingencyType.Enumeration)
                        contingency.AddContingencyValue(contingencyValue);
                    else
                        if (contingency.Type == ContingencyType.CrossSegment)
                            contingency.AddContingencyValue(crossSegmentPointer);
                }

                PendingContingencies.Remove(row);
            }
        }

        // here contingencyValues are row numbers
        private void AddPendingContingencies(int currentRow, string contingencyValues, Contingency currentRowContingency)
        {
            if (string.IsNullOrWhiteSpace(contingencyValues) == true)
            {
                AddValidationResult(ResultType.Error, currentRow, GCExcelReaderHelper.GetColumnIndex(ContingenciesIndex), "No contigency specified");
                return;
            }

            string[] contingencyValueArr = contingencyValues.Split(',');
            int[] contingencyValueRows = new int[contingencyValueArr.Length];
            int contingencyRow;

            foreach (string contingencyValueRow in contingencyValueArr)
            {
                if (int.TryParse(contingencyValueRow.Trim(), out contingencyRow) == true)
                {
                    // Check if contingency value is already present
                    if (contingencyRow <= currentRow)
                    {
                        if (contingencyRow == currentRow)
                        {
                            AddValidationResult(ResultType.Error, currentRow, GCExcelReaderHelper.GetColumnIndex(ContingenciesIndex), "Same row cannot be marked as its own contingency");
                            continue;
                        }

                        string contingencyValue = null;

                        if (currentRowContingency.Type == ContingencyType.Enumeration && ContingencyValue.ContainsKey(contingencyRow))
                        {
                            contingencyValue = ContingencyValue[contingencyRow];
                        }
                        else
                            if (currentRowContingency.Type == ContingencyType.CrossSegment && ContingencyPath.ContainsKey(contingencyRow))
                            {
                                contingencyValue = ContingencyPath[contingencyRow];
                            }

                        if (contingencyValue != null)
                            currentRowContingency.AddContingencyValue(contingencyValue);
                        else
                            AddValidationResult(ResultType.Error, currentRow, GCExcelReaderHelper.GetColumnIndex(ContingenciesIndex),
                                string.Format("Invalid contingency row {0}. Rule: Cotingency row should be marked as each other's contingency.", contingencyRow));
                    }
                    else
                    {
                        List<Contingency> contingencies;
                        if (PendingContingencies.TryGetValue(contingencyRow, out contingencies) == true)
                        {
                            contingencies.Add(currentRowContingency);
                        }
                        else
                        {
                            contingencies = new List<Contingency>();
                            contingencies.Add(currentRowContingency);
                            PendingContingencies.Add(contingencyRow, contingencies);
                        }
                    }
                }
                else
                {
                    AddValidationResult(ResultType.Error, currentRow, GCExcelReaderHelper.GetColumnIndex(ContingenciesIndex), string.Format("Invalid contingency row {0}", contingencyValueRow));
                }
            }
        }

        // crossSegmentPointer = Path to data element followed by expected value in "[]" e.g. HLO->REF->REF01[PK]
        private void AddIdValue(SchemaRow segmentRow, int row, string crossSegmentPointer, List<string> optionalValues, Dictionary<string, string> allowedValues, Dictionary<string, Contingency> contingencies)
        {
            allowedValues.Add(segmentRow.EnumCode, segmentRow.EnumName);

            if (string.Compare(segmentRow.IgnoreFlag, "I", true) == 0 || string.Compare(segmentRow.EnumFlag, "?", true) == 0)
                optionalValues.Add(segmentRow.EnumCode);

            if (string.IsNullOrWhiteSpace(segmentRow.ContingencyType) == false)
            {
                segmentRow.ContingencyType = segmentRow.ContingencyType.Trim();
                if (string.Compare(segmentRow.ContingencyType, "E", true) != 0 && string.Compare(segmentRow.ContingencyType, "CS", true) != 0)
                {
                    AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(ContingencyTypeIndex), string.Format("Invalid contingency type {0}", segmentRow.ContingencyType));
                }
                else
                {
                    Contingency contingency = new Contingency();
                    if (string.Compare(segmentRow.ContingencyType, "E", true) == 0)
                        contingency.Type = ContingencyType.Enumeration;
                    else
                        if (string.Compare(segmentRow.ContingencyType, "CS", true) == 0)
                            contingency.Type = ContingencyType.CrossSegment;

                    AddPendingContingencies(row, segmentRow.Contingencies, contingency);
                    ResolveContingency(row, crossSegmentPointer, segmentRow.EnumCode);

                    contingencies.Add(segmentRow.EnumCode, contingency);
                }
            }
        }

        protected void GetMinMax(string minMax, int row, out int minL, out int maxL)
        {
            minL = -1;
            maxL = -1;

            if (string.IsNullOrEmpty(minMax))
                return;

            string[] numbers = minMax.Split('/');

            if (numbers == null || numbers.Length != 2)
            {
                AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(MinMaxIndex), string.Format("Invalid MinMax value", minMax));
            }
            else
            {
                // TODO: enhance SchemaReaderException to have segment, row as properties
                if (int.TryParse(numbers[0], out minL) == false)
                {
                    AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(MinMaxIndex), string.Format("Cannot parse Min value {0}", numbers[0]));
                    //throw new SchemaReaderException(string.Format("Cannot parse Min value {0} on row {1}", numbers[0], row));
                }

                if (int.TryParse(numbers[1], out maxL) == false)
                {
                    AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(MinMaxIndex), string.Format("Cannot parse Max value {0}", numbers[1]));
                    //throw new SchemaReaderException(string.Format("Cannot parse Max value {0} on row {1}", numbers[1], row));
                }
            }
        }

        protected void ReadBasicRowData(ExcelWorksheet schemaWorksheet, SchemaRow schemaRow, int row)
        {
            if (row > schemaWorksheet.Dimension.End.Row)
                return;

            schemaRow.MandatoryCode = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, MandatoryCodeIndex);
            schemaRow.DataType = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, DataTypeIndex);
            schemaRow.MinMaxLength = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, MinMaxIndex);
            schemaRow.EnumCode = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, EnumCodeIndex);
            schemaRow.EnumName = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, EnumNameIndex);
            schemaRow.EnumFlag = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, EnumFlagIndex, true);
            schemaRow.MandatoryFlag = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, MandatoryFlagIndex, true);
            schemaRow.IgnoreFlag = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, IgnoreFlagIndex, true);
            schemaRow.Contingencies = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, ContingenciesIndex, true);
            schemaRow.ContingencyType = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, ContingencyTypeIndex, true);

            if (TriggerFieldIndex != -1)
            {
                string triggerFieldText = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, TriggerFieldIndex, true);
                if (string.IsNullOrWhiteSpace(triggerFieldText) == false && triggerFieldText.Trim().ToUpperInvariant().Equals(TriggerFieldValue))
                    schemaRow.IsTriggerField = true;
            }

            if (string.IsNullOrWhiteSpace(schemaRow.EnumFlag))
                schemaRow.EnumFlag = "?";

            if (string.IsNullOrWhiteSpace(schemaRow.MandatoryFlag))
                schemaRow.MandatoryFlag = "";

            if (string.IsNullOrWhiteSpace(schemaRow.IgnoreFlag))
                schemaRow.IgnoreFlag = "P";
        }

        protected int GetStartRow(ExcelWorksheet schemaWorksheet)
        {
            int startColumnIndex = 1;
            int row;

            string startRowText = GetStartRowText();

            for (row = 1; row < schemaWorksheet.Dimension.End.Row; ++row)
            {
                if (schemaWorksheet.Cells[row, startColumnIndex].Value == null)
                    continue;

                if (string.Compare("Grouping", schemaWorksheet.Cells[row, startColumnIndex].Value.ToString(), true) == 0 // Special case for GC Schema
                    || string.Compare(startRowText, schemaWorksheet.Cells[row, startColumnIndex].Value.ToString(), true) == 0)
                    break;
            }

            // Move to next row and skip all merged rows till first segment
            ++row;
            while (schemaWorksheet.Cells[row, startColumnIndex].Value == null)
                ++row;

            return row;
        }

        protected static int GetBaseNumberBasedOnFirstNodeNumber(int firstNodeNumber)
        {
            int ret = -1;
            if (firstNodeNumber >= 1 && firstNodeNumber <= 9)
            {
                ret = 1;
            }

            else if (firstNodeNumber >= 10 && firstNodeNumber <= 99)
            {
                ret = firstNodeNumber / 10 * 10;
            }

            else if (firstNodeNumber >= 100 && firstNodeNumber <= 999)
            {
                ret = firstNodeNumber / 100 * 100;
            }

            return ret;
        }

        protected static int GetSequenceNumber(string nodeName, out string prefix)
        {
            int sequenceNumber = 0;
            prefix = string.Empty;
            if (!string.IsNullOrEmpty(nodeName))
            {
                int j;
                int multiplier = 1;
                for (j = nodeName.Length - 1; j >= 0 && IsDigit(nodeName[j]); j--)
                {
                    int digit = nodeName[j] - '0';
                    sequenceNumber += multiplier * digit;
                    multiplier *= 10;

                }

                if (j >= 0)
                {
                    prefix = nodeName.Substring(0, j + 1);
                }

                //no digit found at end
                if (j == nodeName.Length - 1)
                {
                    sequenceNumber = -1;
                }
            }

            else
            {
                sequenceNumber = -1;
            }

            return sequenceNumber;
        }

        protected static bool IsDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        protected static string GenerateName(string prefix, int fillIndex)
        {
            string suffix = fillIndex < 10 ? "0" + fillIndex.ToString() : fillIndex.ToString();

            if (string.IsNullOrEmpty(prefix))
            {
                return suffix;
            }

            else
            {
                return prefix + suffix;
            }
        }

        protected void AddValidationResult(ResultType resultType, int rowIndex, string columnIndex, string description)
        {
            this.SpecCertValidationResult.SegmentDefinitionValidationResults.Add(new SegmentDefinitionValidationResult()
            {
                ColumnIndex = columnIndex,
                RowIndex = rowIndex,
                Type = resultType,
                Description = description,
            });
        }

        public static GCExcelToDocumentPlug CreateInstance(SpecCertFileType certFileType)
        {
            GCExcelToDocumentPlug gcExcelToDocumentPlug = null;

            switch (certFileType)
            {
                case SpecCertFileType.X12:
                    gcExcelToDocumentPlug = new X12GCExcelToDocumentPlug();
                    break;
                case SpecCertFileType.FlatFile:
                    gcExcelToDocumentPlug = new FlatFileGCExcelToDocumentPlug();
                    break;
                case SpecCertFileType.Xml:
                    gcExcelToDocumentPlug = new XmlGCExcelToDocumentPlug();
                    break;
                default:
                    throw new NotSupportedException(string.Format("{0} cert file type is not supported", certFileType));
            }

            return gcExcelToDocumentPlug;
        }
    }
}
