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
    public class FlatFileGCExcelToDocumentPlug : GCExcelToDocumentPlug
    {
        class FlatFileSchemaRow : SchemaRow
        {
            public string Grouping { get; set; }
            public string Loop { get; set; }
            public string Segment { get; set; }
            public string DataElementTag { get; set; }
            public string DataElementName { get; set; }

            public override string GetDataElementTag() { return DataElementTag; }
        };

        protected int GroupingIndex;
        protected int LoopIndex;
        protected int SegmentIndex;
        protected int DataElementTagIndex;
        protected int DataElementNameIndex;

        public FlatFileGCExcelToDocumentPlug() : base()
        {
        }

        protected override void InitializeColumnIndexes(string direction)
        {
            // Flat file Cert file column indexes
            int FlatFileGroupingIndex = 1;
            int FlatFileLoopIndex = 2; // Used only for segment - to set the name
            int FlatFileSegmentIndex = 9; // We want 3 letter code
            int FlatFileDataElementTagIndex = 3;
            int FlatFileDataElementNameIndex = 3; // No element name for flat file
            int FlatFileMandatoryCodeIndex = 4;
            int FlatFileDataTypeIndex = 5;
            int FlatFileMinMaxIndex = 6;
            int FlatFileEnumCodeIndex = 7;
            int FlatFileEnumNameIndex = 8;

            int FlatFileSendMandatoryFlagIndex = 12;
            int FlatFileSendEnumFlagIndex = 13;
            int FlatFileSendContingenciesIndex = 14;
            int FlatFileSendContingencyTypeIndex = 15;
            int FlatFileSendIgnoreFlagIndex = 16;

            int FlatFileReceiveMandatorFlagIndex = 19;
            int FlatFileReceiveEnumFlagIndex = 20;
            int FlatFileReceiveContingenciesIndex = 21;
            int FlatFileReceiveContingencyTypeIndex = 22;
            int FlatFileReceiveIgnoreFlagIndex = 23;

            GroupingIndex = FlatFileGroupingIndex;
            LoopIndex = FlatFileLoopIndex;
            SegmentIndex = FlatFileSegmentIndex;
            DataElementTagIndex = FlatFileDataElementTagIndex;
            DataElementNameIndex = FlatFileDataElementNameIndex;
            MandatoryCodeIndex = FlatFileMandatoryCodeIndex;
            DataTypeIndex = FlatFileDataTypeIndex;
            MinMaxIndex = FlatFileMinMaxIndex;
            EnumCodeIndex = FlatFileEnumCodeIndex;
            EnumNameIndex = FlatFileEnumNameIndex;

            if (direction.ToLowerInvariant() == "send")
            {
                MandatoryFlagIndex = FlatFileSendMandatoryFlagIndex;
                EnumFlagIndex = FlatFileSendEnumFlagIndex;
                ContingenciesIndex = FlatFileSendContingenciesIndex;
                ContingencyTypeIndex = FlatFileSendContingencyTypeIndex;
                IgnoreFlagIndex = FlatFileSendIgnoreFlagIndex;
            }
            else
            {
                MandatoryFlagIndex = FlatFileReceiveMandatorFlagIndex;
                EnumFlagIndex = FlatFileReceiveEnumFlagIndex;
                ContingenciesIndex = FlatFileReceiveContingenciesIndex;
                ContingencyTypeIndex = FlatFileReceiveContingencyTypeIndex;
                IgnoreFlagIndex = FlatFileReceiveIgnoreFlagIndex;
            }
        }

        protected override string GetStartRowText()
        {
            return "Section";
        }

        protected override DocumentPlug ConstructDocumentPlug(ExcelWorksheet schemaWorksheet, int startRow)
        {
            Pluglet rootPluglet = new Pluglet("X12", "GC root Node", PlugletType.Loop, null, 1, -1);

            DocumentPlug documentPlug = new DocumentPlug(rootPluglet, BusinessDomain.FlatFile);

            ReadMetadata(documentPlug, schemaWorksheet);

            string currentLoopName = string.Empty;
            string nextLoopName;

            IPluglet loopPluglet = null;
            IPluglet nextPluglet;
            int minOccurs, maxOccurs;
            int row = startRow;

            while ((nextPluglet = GetSegment(schemaWorksheet, ref row, out nextLoopName)) != null)
            {
                // In case of flat file, we do not have loops
                rootPluglet.Children.Add(nextPluglet);
                nextPluglet.Parent = rootPluglet;
            }

            return documentPlug;
        }

        protected override SchemaRow ReadRow(ExcelWorksheet schemaWorksheet, int row)
        {
            if (row > schemaWorksheet.Dimension.End.Row)
                return null;

            FlatFileSchemaRow schemaRow = new FlatFileSchemaRow();

            schemaRow.Grouping = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, GroupingIndex);
            schemaRow.Loop = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, LoopIndex);
            schemaRow.Segment = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, SegmentIndex);
            schemaRow.DataElementTag = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, DataElementTagIndex);
            schemaRow.DataElementName = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, DataElementNameIndex);

            base.ReadBasicRowData(schemaWorksheet, schemaRow, row);

            // Check for invalid loop names
            if (string.IsNullOrWhiteSpace(schemaRow.Loop) == false
                && string.Equals(schemaRow.Loop, "n/a", StringComparison.InvariantCultureIgnoreCase) == false
                && schemaRow.Loop.Trim().Contains('|'))
            {
                AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(MinMaxIndex), string.Format("Invalid loop value {0}", schemaRow.Loop));
            }

            return schemaRow;
        }

        private IPluglet GetSegment(ExcelWorksheet schemaWorksheet, ref int row, out string nextLoopName)
        {
            nextLoopName = null;
            // TODO: Handle this in better way
            if (row > schemaWorksheet.Dimension.End.Row)
            {
                return null;
            }

            // Currently setting Min and Max for segment is hard coded as 0, 1, since these values are not present in excel
            int segmentMinOccur = 0;
            int segmentMaxOccur = 100;

            // First read Segment row
            FlatFileSchemaRow segmentRow = ReadRow(schemaWorksheet, row) as FlatFileSchemaRow;

            while (segmentRow != null && string.IsNullOrWhiteSpace(segmentRow.Segment) == true)
            {
                row++;
                segmentRow = ReadRow(schemaWorksheet, row) as FlatFileSchemaRow;
            }

            if (segmentRow == null)
                return null;


            nextLoopName = segmentRow.Loop;
            segmentMinOccur = string.Compare(segmentRow.MandatoryFlag, "Y", true) == 0 ? 1 : 0;
            segmentMaxOccur = string.Compare(segmentRow.MandatoryFlag, "N", true) == 0 ? 0 : 1000;

            IPluglet segment = new Pluglet(segmentRow.Segment, segmentRow.DataElementTag, (PlugletType)Enum.Parse(typeof(PlugletType), "Segment"), null
                , segmentMinOccur, segmentMaxOccur);
            if (string.IsNullOrWhiteSpace(segmentRow.Loop) == false)
            {
                segment.Name = segmentRow.Loop;
            }

            ++row;

            IPluglet dataPluglet = null;

            int minOccurs, maxOccurs;
            bool isIgnore;
            int tempRow;

            // Now read all data elements till groupping column has some value (indicates new Segment started)
            while ((segmentRow = ReadRow(schemaWorksheet, row) as FlatFileSchemaRow) != null)
            {
                if (string.IsNullOrEmpty(segmentRow.Segment) == false)
                    break;

                // TODO: What about mandatory flag value 'X'?
                minOccurs = string.Compare(segmentRow.MandatoryCode, "M", true) == 0 && string.Compare(segmentRow.MandatoryFlag, "Y", true) == 0 ? 1 : 0;
                maxOccurs = string.Compare(segmentRow.MandatoryFlag, "N", true) == 0 ? 0 : 1;
                isIgnore = string.Compare(segmentRow.IgnoreFlag, "I", true) == 0;

                dataPluglet = new Pluglet(segmentRow.DataElementTag, segmentRow.DataElementName, 
                                            (PlugletType)Enum.Parse(typeof(PlugletType), "Data"), segment, minOccurs, maxOccurs, isIgnore);
                ++row;

                 dataPluglet.DataType = ReadDataType(segmentRow, schemaWorksheet, segment.Path, ref row);
            }

            FillInMissingChildren(segment);
            return segment;
        }

        private void ReadMetadata(DocumentPlug documentPlug, ExcelWorksheet schemaWorksheet)
        {
            int row;
            string cellValue;
            int delimiterCount = 0;

            for (row = 1; row < schemaWorksheet.Dimension.End.Row && delimiterCount < 2; ++row)
            {
                // column 1 has name 'Element Segment' or 'Segment Delimiter'
                // Column 3 has delimiter
                cellValue = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, 1);
                if (cellValue == null)
                    continue;

                if (string.Compare("Element Delimiter", cellValue, true) == 0)
                {
                    cellValue = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, 3);
                    // delimiter can be more than 1 char, 
                    // split the string by ' ', convert each string to int
                    documentPlug.ElementDelimiters = GetDelimiters(cellValue);

                    if (documentPlug.ElementDelimiters == null)
                        AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(3), string.Format("'{0}' is invalid element delimiter", cellValue));

                    delimiterCount++;
                }
                else
                    if (string.Compare("Segment Delimiter", cellValue, true) == 0)
                    {
                        cellValue = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, 3);
                        documentPlug.SegmentDelimiters = GetDelimiters(cellValue);

                        if (documentPlug.SegmentDelimiters == null)
                            AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(3), string.Format("'{0}' is invalid segment delimiter", cellValue));

                        delimiterCount++;
                    }
            }
        }

        private static List<int> GetDelimiters(string cellValue)
        {
            if (string.IsNullOrWhiteSpace(cellValue))
                return null;

            string[] delimiterChars = cellValue.Split(' ');

            List<int> delimiters = new List<int>();

            int delimiterInt;
            try
            {
                foreach (string str in delimiterChars)
                {
                    if (int.TryParse(str.Trim(), out delimiterInt) == false)
                        return null;

                    delimiters.Add(delimiterInt);
                }
            }
            catch
            {
                return null;
            }

            return delimiters;
        }

        protected override bool IsDataTypeRowsOver(SchemaRow schemaRow, int row)
        {
            FlatFileSchemaRow flatFileSchemaRow = schemaRow as FlatFileSchemaRow;
            return string.IsNullOrEmpty(flatFileSchemaRow.Segment) == false;
        }
    }
}
