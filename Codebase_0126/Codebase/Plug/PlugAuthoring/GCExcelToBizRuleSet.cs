using Maarg.Contracts.GCValidate;
using Maarg.Fatpipe.Plug.DataModel;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class GCExcelToBizRuleSet
    {
        int RuleNameIndex;
        int RuleOptionIndex;
        int SegmentPathStartIndex;

        public BizRuleCertValidationResult BizRuleCertValidationResult { get; set; }

        public GCExcelToBizRuleSet()
        {
            BizRuleCertValidationResult = new BizRuleCertValidationResult();
        }

        public BizRuleSet GenerateBizRuleSet(Stream certFileStream)
        {
            if (certFileStream == null)
                throw new ArgumentNullException("certFileStream");

            BizRuleSet bizRuleSet = new BizRuleSet();

            using (ExcelPackage pck = new ExcelPackage(certFileStream))
            {
                ExcelWorkbook workBook = pck.Workbook;
                string worksheetName = "Sheet1";

                ExcelWorksheet schemaWorksheet = workBook.Worksheets[worksheetName];

                if (schemaWorksheet == null)
                {
                    AddValidationResult(ResultType.Error, -1, "N/A", string.Format("'{0}' worksheet doesn't exist", worksheetName));
                    return null;
                }

                int row = GetStartRow(schemaWorksheet);
                ReadMessageDomainIds(schemaWorksheet, row, bizRuleSet);
                if (bizRuleSet.MessageDomainIds.Count < 2)
                {
                    AddValidationResult(ResultType.Error, row, "N/A", "More than 1 domain ids required in rule set");
                    return null;
                }

                ReadAllRules(schemaWorksheet, row + 1, bizRuleSet);
            }

            return bizRuleSet;
        }

        private void ReadAllRules(ExcelWorksheet schemaWorksheet, int row, BizRuleSet bizRuleSet)
        {
            List<string> bizRuleRow;
            List<BizRule> bizRules;
            BizRule bizRule;
            string segmentPathKey;

            int columnCount = bizRuleSet.MessageDomainIds.Count + 2; // +2 => Name and rule matching option

            try
            {
                while ((bizRuleRow = ReadBizRuleRow(schemaWorksheet, columnCount, ref row)) != null)
                {
                    if (bizRuleRow.Count != 0)
                    {
                        try
                        {
                            bizRule = new BizRule(bizRuleRow);

                            segmentPathKey = bizRuleRow[2].Substring(0, bizRuleRow[2].LastIndexOf("->"));

                            if (bizRuleSet.BizRules.TryGetValue(segmentPathKey, out bizRules) == false)
                            {
                                bizRules = new List<BizRule>();
                                bizRuleSet.BizRules.Add(segmentPathKey, bizRules);
                            }

                            bizRules.Add(bizRule);
                        }
                        catch (Exception ex)
                        {
                            AddValidationResult(ResultType.Error, row, "N/A", ex.Message);
                        }
                    }

                    row++;
                }
            }
            catch (Exception ex)
            {
                AddValidationResult(ResultType.Error, row, "N/A", "Error occurred reading rules: " + ex.Message);
            }
        }

        private List<string> ReadBizRuleRow(ExcelWorksheet schemaWorksheet, int columnCount, ref int row)
        {
            while (row <= schemaWorksheet.Dimension.End.Row
                && GCExcelReaderHelper.ReadCell(schemaWorksheet, row, 1) == null)
                row++;

            if (row > schemaWorksheet.Dimension.End.Row)
                return null;

            List<string> bizRuleRow = new List<string>();

            for (int i = 1; i <= columnCount; i++)
            {
                bizRuleRow.Add(GCExcelReaderHelper.ReadCell(schemaWorksheet, row, i));
            }

            return bizRuleRow;
        }

        private void AddValidationResult(ResultType resultType, int rowIndex, string columnIndex, string description)
        {
            this.BizRuleCertValidationResult.RuleDefinitionValidationResults.Add(new RuleDefinitionValidationResult()
                {
                    ColumnIndex = columnIndex,
                    RowIndex = rowIndex,
                    Type = resultType,
                    Description = description,
                });
        }

        private int GetStartRow(ExcelWorksheet schemaWorksheet)
        {
            int startColumnIndex = 1;
            int row;

            string startRowText = "Rule Name";

            for (row = 1; row < schemaWorksheet.Dimension.End.Row; ++row)
            {
                if (schemaWorksheet.Cells[row, startColumnIndex].Value == null)
                    continue;

                if (string.Compare(startRowText, schemaWorksheet.Cells[row, startColumnIndex].Value.ToString(), true) == 0)
                    break;
            }

            return row;
        }

        private void ReadMessageDomainIds(ExcelWorksheet schemaWorksheet, int row, BizRuleSet bizRuleSet)
        {
            int domainIdIndex = 3;
            int domainId;
            while(true)
            {
                string cellValue = GCExcelReaderHelper.ReadCell(schemaWorksheet, row, domainIdIndex);

                // Stop if cell value is non-integer
                if (cellValue == null || int.TryParse(cellValue, out domainId) == false)
                    return;

                bizRuleSet.MessageDomainIds.Add(domainId);

                domainIdIndex++;
            }
        }
    }
}
