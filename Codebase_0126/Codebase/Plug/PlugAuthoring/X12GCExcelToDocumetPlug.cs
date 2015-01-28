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
    public class X12GCExcelToDocumentPlug : GCExcelToDocumentPlug
    {
        class X12SchemaRow : SchemaRow
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

        public X12GCExcelToDocumentPlug() : base()
        {
        }

        protected override void InitializeColumnIndexes(string direction)
        {
            // EDI Cert file column indexes
            int X12GroupingIndex = 1;
            int X12LoopIndex = 2;
            int X12SegmentIndex = 3;
            int X12DataElementTagIndex = 4;
            int X12DataElementNameIndex = 5;
            int X12MandatoryCodeIndex = 6;
            int X12DataTypeIndex = 7;
            int X12MinMaxIndex = 8;
            int X12EnumCodeIndex = 9;
            int X12EnumNameIndex = 10;

            int X12TriggerFieldIndex = 11;

            int X12SendMandatoryFlagIndex = 15;
            int X12SendEnumFlagIndex = 16;
            int X12SendContingenciesIndex = 17;
            int X12SendContingencyTypeIndex = 18;
            int X12SendIgnoreFlagIndex = 19;

            int X12ReceiveMandatorFlagIndex = 20;
            int X12ReceiveEnumFlagIndex = 21;
            int X12ReceiveContingenciesIndex = 22;
            int X12ReceiveContingencyTypeIndex = 23;
            int X12ReceiveIgnoreFlagIndex = 24;

            GroupingIndex = X12GroupingIndex;
            LoopIndex = X12LoopIndex;
            SegmentIndex = X12SegmentIndex;
            DataElementTagIndex = X12DataElementTagIndex;
            DataElementNameIndex = X12DataElementNameIndex;
            MandatoryCodeIndex = X12MandatoryCodeIndex;
            DataTypeIndex = X12DataTypeIndex;
            MinMaxIndex = X12MinMaxIndex;
            EnumCodeIndex = X12EnumCodeIndex;
            EnumNameIndex = X12EnumNameIndex;
            TriggerFieldIndex = X12TriggerFieldIndex;

            if (direction.ToLowerInvariant() == "send")
            {
                MandatoryFlagIndex = X12SendMandatoryFlagIndex;
                EnumFlagIndex = X12SendEnumFlagIndex;
                ContingenciesIndex = X12SendContingenciesIndex;
                ContingencyTypeIndex = X12SendContingencyTypeIndex;
                IgnoreFlagIndex = X12SendIgnoreFlagIndex;
            }
            else
            {
                MandatoryFlagIndex = X12ReceiveMandatorFlagIndex;
                EnumFlagIndex = X12ReceiveEnumFlagIndex;
                ContingenciesIndex = X12ReceiveContingenciesIndex;
                ContingencyTypeIndex = X12ReceiveContingencyTypeIndex;
                IgnoreFlagIndex = X12ReceiveIgnoreFlagIndex;
            }
        }

        protected override string GetStartRowText()
        {
            return "Control";
        }

        protected override DocumentPlug ConstructDocumentPlug(ExcelWorksheet schemaWorksheet, int startRow)
        {
            Pluglet rootPluglet = new Pluglet("X12", "GC root Node", PlugletType.Loop, null, 1, -1);

            DocumentPlug documentPlug = new DocumentPlug(rootPluglet, BusinessDomain.X12);

            if (string.IsNullOrWhiteSpace(schemaWorksheet.Name) == false)
            {
                int documentType;
                string strDocumentType = schemaWorksheet.Name.Substring(0, schemaWorksheet.Name.IndexOf(" "));
                if (int.TryParse(strDocumentType, out documentType) == true)
                {
                    documentPlug.DocumentType = documentType;
                }
            }

            string currentLoopName = string.Empty;
            string nextLoopName;
            string loopName;
            List<string> intermediateLoops = new List<string>();

            IPluglet loopPluglet = null;
            IPluglet nextPluglet;
            IPluglet loopParent;
            int minOccurs, maxOccurs;
            int row = startRow;
            int current, next;

            loopPluglet = rootPluglet;
            while ((nextPluglet = GetSegment(schemaWorksheet, ref row, out nextLoopName)) != null)
            {
                // In case of flat file, we do not have loops
                if (string.IsNullOrEmpty(nextLoopName))
                {
                    rootPluglet.Children.Add(nextPluglet);
                    nextPluglet.Parent = rootPluglet;
                    loopPluglet = rootPluglet;
                    intermediateLoops.Clear();
                    currentLoopName = nextLoopName;
                }
                else
                {
                    // Check if at least one intermediate loop name is different in next loop
                    if (string.Compare(currentLoopName, nextLoopName) != 0)
                    {
                        loopName = nextLoopName;

                        string[] nextLoops = nextLoopName.Split(new string[] {"->"}, StringSplitOptions.None);

                        // Find first non-matching loop name between intermediateLoops and nextLoops
                        current = next = 0;
                        while (current < intermediateLoops.Count && next < nextLoops.Length)
                        {
                            if (string.Equals(intermediateLoops[current], nextLoops[next], StringComparison.OrdinalIgnoreCase) == false)
                                break;
                            current++;
                            next++;
                        }

                        // Get loopParent from current intermediate loops
                        for (int i = intermediateLoops.Count - 1; i >= current; i--)
                        {
                            loopPluglet = loopPluglet.Parent;
                        }

                        loopParent = loopPluglet;

                        // Remove all non-matching intermediate loops
                        int loopsCount = intermediateLoops.Count;
                        for (int i = current; i < loopsCount; i++)
                            intermediateLoops.RemoveAt(current);

                        // Add new intermediate loops
                        for (int j = next; j < nextLoops.Length; j++)
                        {
                            // TODO: Any criteria for setting min and max occurs for loop?
                            minOccurs = 1;
                            maxOccurs = 100;

                            // create new loop
                            loopPluglet = new Pluglet(nextLoops[j] + "Loop", "Loop Node", PlugletType.Loop, loopParent, minOccurs, maxOccurs);
                            intermediateLoops.Add(nextLoops[j]);
                            loopParent = loopPluglet;
                        }

                        currentLoopName = nextLoopName;
                    }

                    loopPluglet.Children.Add(nextPluglet);
                    nextPluglet.Parent = loopPluglet;
                }
            }

            return documentPlug;
        }

        protected override SchemaRow ReadRow(ExcelWorksheet schemaWorksheet, int row)
        {
            if (row > schemaWorksheet.Dimension.End.Row)
                return null;

            X12SchemaRow schemaRow = new X12SchemaRow();

            schemaRow.Grouping = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, GroupingIndex);
            schemaRow.Loop = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, LoopIndex);
            schemaRow.Segment = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, SegmentIndex);
            schemaRow.DataElementTag = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, DataElementTagIndex);
            schemaRow.DataElementName = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, DataElementNameIndex);

            base.ReadBasicRowData(schemaWorksheet, schemaRow, row);

            // This code was added during contingency field handling, however maps migration project generate spec certs without contingencies and hence the following code was removed.
            // if (string.IsNullOrWhiteSpace(schemaRow.DataElementTag) == false && string.Compare(schemaRow.DataElementTag, "HL03", true) == 0)
            //        schemaRow.IsTriggerField = true;
                
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
            // TODO: Handle this in better way
            if (row > schemaWorksheet.Dimension.End.Row)
            {
                nextLoopName = null;
                return null;
            }

            // Currently setting Min and Max for segment is hard coded as 0, 1, since these values are not present in excel
            int segmentMinOccur = 0;
            int segmentMaxOccur = 100;
            bool isIgnore;

            // First read Segment row
            X12SchemaRow segmentRow = ReadRow(schemaWorksheet, row) as X12SchemaRow;
            nextLoopName = segmentRow.Loop;
            if (nextLoopName == null)
                nextLoopName = string.Empty;

            segmentMinOccur = string.Compare(segmentRow.MandatoryFlag, "Y", true) == 0 ? 1 : 0;
            segmentMaxOccur = string.Compare(segmentRow.MandatoryFlag, "N", true) == 0 ? 0 : 1000;
            isIgnore = string.Compare(segmentRow.IgnoreFlag, "I", true) == 0;
            IPluglet segment = new Pluglet(segmentRow.Segment, segmentRow.DataElementTag, PlugletType.Segment, null, segmentMinOccur, segmentMaxOccur, isIgnore);

            string xPath = nextLoopName;
            if(string.Compare(xPath, "n/a", true) == 0)
                xPath = nextLoopName = string.Empty;
            if (string.IsNullOrWhiteSpace(xPath) == false)
                xPath = string.Format("X12{0}{1}Loop{2}{3}{4}", segment.PathSeperator, xPath, segment.PathSeperator, segment.Tag, segment.PathSeperator);
            else
                xPath = string.Format("X12{0}{1}{2}", segment.PathSeperator, segment.Tag, segment.PathSeperator);

            ++row;

            IPluglet dataPluglet = null;

            int minOccurs, maxOccurs;

            // Now read all data elements till groupping column has some value (indicates new Segment started)
            while ((segmentRow = ReadRow(schemaWorksheet, row) as X12SchemaRow) != null)
            {
                if (string.IsNullOrEmpty(segmentRow.Grouping) == false)
                    break;

                // TODO: What about mandatory flag value 'X'?
                minOccurs = string.Compare(segmentRow.MandatoryCode, "M", true) == 0 && string.Compare(segmentRow.MandatoryFlag, "Y", true) == 0 ? 1 : 0;
                maxOccurs = string.Compare(segmentRow.MandatoryFlag, "N", true) == 0 ? 0 : 1;
                isIgnore = string.Compare(segmentRow.IgnoreFlag, "I", true) == 0;

                dataPluglet = new Pluglet(
                    new PlugletInput() 
                                {
                                    Name = segmentRow.DataElementTag,
                                    Definition = segmentRow.DataElementName,
                                    Type = PlugletType.Data,
                                    Parent = segment,
                                    MinOccurs = minOccurs,
                                    MaxOccurs = maxOccurs,
                                    IsIgnore = isIgnore,
                                    IsTriggerField = segmentRow.IsTriggerField
                                });
                ++row;

                dataPluglet.DataType = ReadDataType(segmentRow, schemaWorksheet, string.Format("{0}{1}", xPath, segmentRow.DataElementTag), ref row);
            }

            FillInMissingChildren(segment);
            return segment;
        }

        protected override bool IsDataTypeRowsOver(SchemaRow schemaRow, int row)
        {
            // no special check for X12
            return false;
        }
    }
}
