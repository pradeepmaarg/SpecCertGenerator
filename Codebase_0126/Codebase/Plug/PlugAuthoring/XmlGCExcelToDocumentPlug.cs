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
    public class XmlGCExcelToDocumentPlug : GCExcelToDocumentPlug
    {
        class XmlFileSchemaRow : SchemaRow
        {
            public string ElementName { get; set; }
            public int Level { get; set; }
            public bool IsLeafNode { get; set; }

            public override string GetDataElementTag() { return ElementName; }
        };

        private string RootNodeName { get; set; }

        public XmlGCExcelToDocumentPlug() : base()
        {
        }

        protected override void InitializeColumnIndexes(string direction)
        {
            // Xml file Cert file column indexes
            int XmlFileMandatoryCodeIndex = 6;
            int XmlFileDataTypeIndex = 7;
            int XmlFileMinMaxIndex = 8;
            int XmlFileEnumCodeIndex = 9;
            int XmlFileEnumNameIndex = 10;

            int XmlFileSendMandatoryFlagIndex = 13;
            int XmlFileSendEnumFlagIndex = 14;
            int XmlFileSendContingenciesIndex = 15;
            int XmlFileSendContingencyTypeIndex = 16;
            int XmlFileSendIgnoreFlagIndex = 17;

            int XmlFileReceiveMandatorFlagIndex = 20;
            int XmlFileReceiveEnumFlagIndex = 21;
            int XmlFileReceiveContingenciesIndex = 22;
            int XmlFileReceiveContingencyTypeIndex = 23;
            int XmlFileReceiveIgnoreFlagIndex = 24;

            MandatoryCodeIndex = XmlFileMandatoryCodeIndex;
            DataTypeIndex = XmlFileDataTypeIndex;
            MinMaxIndex = XmlFileMinMaxIndex;
            EnumCodeIndex = XmlFileEnumCodeIndex;
            EnumNameIndex = XmlFileEnumNameIndex;

            if (direction.ToLowerInvariant() == "send")
            {
                MandatoryFlagIndex = XmlFileSendMandatoryFlagIndex;
                EnumFlagIndex = XmlFileSendEnumFlagIndex;
                ContingenciesIndex = XmlFileSendContingenciesIndex;
                ContingencyTypeIndex = XmlFileSendContingencyTypeIndex;
                IgnoreFlagIndex = XmlFileSendIgnoreFlagIndex;
            }
            else
            {
                MandatoryFlagIndex = XmlFileReceiveMandatorFlagIndex;
                EnumFlagIndex = XmlFileReceiveEnumFlagIndex;
                ContingenciesIndex = XmlFileReceiveContingenciesIndex;
                ContingencyTypeIndex = XmlFileReceiveContingencyTypeIndex;
                IgnoreFlagIndex = XmlFileReceiveIgnoreFlagIndex;
            }
        }

        private void ReadMetadata(ExcelWorksheet schemaWorksheet)
        {
            int row;
            string cellValue;

            for (row = 1; row < schemaWorksheet.Dimension.End.Row; ++row)
            {
                cellValue = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, 1);
                if (cellValue == null)
                    continue;

                if (string.Compare("Root Node", cellValue, true) == 0)
                {
                    RootNodeName = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, 3);

                    if (string.IsNullOrWhiteSpace(RootNodeName))
                        AddValidationResult(ResultType.Error, row, GCExcelReaderHelper.GetColumnIndex(3), string.Format("Root node name is empty", cellValue));

                    break;
                }
            }
        }

        protected override string GetStartRowText()
        {
            return "Loop";
        }

        protected override DocumentPlug ConstructDocumentPlug(ExcelWorksheet schemaWorksheet, int startRow)
        {
            ReadMetadata(schemaWorksheet);

            Pluglet rootPluglet = new Pluglet(
                                        new PlugletInput()
                                        {
                                            Name = RootNodeName,
                                            Definition = "GC root node",
                                            Type = PlugletType.Loop,
                                            Parent = null,
                                            IsTagSameAsName = true, // for XML spec cert tag is always same as name
                                        });

            DocumentPlug documentPlug = new DocumentPlug(rootPluglet, BusinessDomain.Xml);

            // Assumption: Element names are not repeated on teh subsequent lines

            IPluglet loopPluglet = null;
            IPluglet nextPluglet;
            int minOccurs, maxOccurs;
            int row = startRow;

            // This loop is for level-1 nodes
            try
            {
                while ((nextPluglet = GetNextNode(schemaWorksheet, 1, ref row)) != null)
                {
                    rootPluglet.Children.Add(nextPluglet);
                    nextPluglet.Parent = rootPluglet;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Row: {0}, Error: {1}", row, ex.Message));
            }

            return documentPlug;
        }

        protected override SchemaRow ReadRow(ExcelWorksheet schemaWorksheet, int row)
        {
            if (row > schemaWorksheet.Dimension.End.Row)
                return null;

            XmlFileSchemaRow schemaRow = new XmlFileSchemaRow();

            // Read all first 5 cell value
            for (int i = 0; i < 5; i++)
            {
                schemaRow.ElementName = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, i + 1);
                if (string.IsNullOrWhiteSpace(schemaRow.ElementName) == false)
                {
                    schemaRow.Level = i + 1;
                    break;
                }
            }

            base.ReadBasicRowData(schemaWorksheet, schemaRow, row);

            // If data type is not-null then set IsLeafNode flag true
            schemaRow.IsLeafNode = !string.IsNullOrWhiteSpace(schemaRow.DataType);

            return schemaRow;
        }

        private IPluglet GetNextNode(ExcelWorksheet schemaWorksheet, int currentLevel, ref int row)
        {
            // TODO: Handle this in better way
            if (row > schemaWorksheet.Dimension.End.Row)
            {
                return null;
            }

            // Currently setting Min and Max for segment is hard coded as 0, 100, since these values are not present in excel
            int segmentMinOccur = 0;
            int segmentMaxOccur = 100;
            int loopMinOccur = 0;
            int loopMaxOccur = 100;

            XmlFileSchemaRow currentRow = ReadRow(schemaWorksheet, row) as XmlFileSchemaRow;
            XmlFileSchemaRow nextRow = ReadRow(schemaWorksheet, row+1) as XmlFileSchemaRow;

            IPluglet nextNode = null;
            IPluglet childNode = null;

            if (nextRow.IsLeafNode)
            {
                segmentMinOccur = string.Compare(currentRow.MandatoryFlag, "Y", true) == 0 ? 1 : 0;
                segmentMaxOccur = string.Compare(currentRow.MandatoryFlag, "N", true) == 0 ? 0 : 1000;
                // If next row contains leaf node then we need to construct segment
                nextNode = new Pluglet(
                    new PlugletInput() 
                                {
                                    Name = currentRow.ElementName,
                                    Definition = currentRow.ElementName,
                                    Type = PlugletType.Segment,
                                    Parent = null,
                                    MinOccurs = segmentMinOccur,
                                    MaxOccurs = segmentMaxOccur,
                                    IsTagSameAsName = true, // for XML spec cert tag is always same as name
                                });

                IPluglet dataPluglet = null;

                // Now read all data elements
                currentRow = nextRow;
                ++row;
                while (currentRow != null && currentRow.Level == currentLevel + 1)
                {
                    if (currentRow.IsLeafNode)
                    {
                        ++row;
                        int minOccurs, maxOccurs;
                        bool isIgnore;

                        // TODO: What about mandatory flag value 'X'?
                        minOccurs = string.Compare(currentRow.MandatoryCode, "M", true) == 0 && string.Compare(currentRow.MandatoryFlag, "Y", true) == 0 ? 1 : 0;
                        maxOccurs = string.Compare(currentRow.MandatoryFlag, "N", true) == 0 ? 0 : 1;
                        isIgnore = string.Compare(currentRow.IgnoreFlag, "I", true) == 0;

                        dataPluglet = new Pluglet(
                                new PlugletInput() 
                                {
                                    Name = currentRow.ElementName,
                                    Definition = currentRow.ElementName,
                                    Type = PlugletType.Data,
                                    Parent = nextNode,
                                    MinOccurs = minOccurs,
                                    MaxOccurs = maxOccurs,
                                    IsIgnore = isIgnore,
                                    IsTagSameAsName = true, // for XML spec cert tag is always same as name
                                });

                        dataPluglet.DataType = ReadDataType(currentRow, schemaWorksheet, nextNode.Path, ref row);
                    }
                    else
                    {
                        childNode = GetNextNode(schemaWorksheet, currentLevel + 1, ref row);
                        nextNode.Children.Add(childNode);
                        childNode.Parent = nextNode;
                    }

                    currentRow = ReadRow(schemaWorksheet, row) as XmlFileSchemaRow;
                }
            }
            else
            {
                // If next row contains non-leaf node then we need to construct loop element
                nextNode = new Pluglet(
                    new PlugletInput() 
                                {
                                    Name = currentRow.ElementName,
                                    Definition = currentRow.ElementName,
                                    Type = PlugletType.Loop,
                                    Parent = null,
                                    MinOccurs = loopMinOccur,
                                    MaxOccurs = loopMaxOccur,
                                    IsTagSameAsName = true, // for XML spec cert tag is always same as name
                                });

                ++row;

                // Read all child elements (level is > current level)
                while (nextRow != null && nextRow.Level > currentLevel)
                {
                    childNode = GetNextNode(schemaWorksheet, currentLevel + 1, ref row);

                    nextNode.Children.Add(childNode);
                    childNode.Parent = nextNode;

                    nextRow = ReadRow(schemaWorksheet, row) as XmlFileSchemaRow;
                }
            }

            // Do we need this for xml cert files?
            // FillInMissingChildren(segment);

            return nextNode;
        }

        protected override bool IsDataTypeRowsOver(SchemaRow schemaRow, int row)
        {
            // TODO: read all segments and check data type is over
            return false;
        }
    }
}
