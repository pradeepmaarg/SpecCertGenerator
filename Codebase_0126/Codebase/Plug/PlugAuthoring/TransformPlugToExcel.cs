using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Web;

using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Style.XmlAccess;
using OfficeOpenXml.Table;
using Maarg.Fatpipe.Plug.DataModel;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class TransformPlugToExcel
    {
        //Make this function resilience to nulls to allow Plug to be created
        // even when partial information is available
        public static byte[] GenerateExcelFromTransformPlug(ITransformPlug transformPlug)
        {
            ExcelPackage pck = new ExcelPackage();

            if (transformPlug.SourceDocument != null)
            {
                DocumentPlugtoExcel.GenerateWorksheetFromDocumentPlug(pck.Workbook, transformPlug.SourceDocument, "Source#{0}");
            }

            if (transformPlug.TargetDocument != null)
            {
                DocumentPlugtoExcel.GenerateWorksheetFromDocumentPlug(pck.Workbook, transformPlug.TargetDocument, "Target#{0}");
            }

            if (transformPlug.Facets != null)
            {
                string srcName = transformPlug.SourceDocument != null ? transformPlug.SourceDocument.Name : "Source";
                string targetName = transformPlug.TargetDocument != null ? transformPlug.TargetDocument.Name : "Target";
                GenerateWorksheetFromFacets(pck.Workbook, transformPlug.Facets, srcName, targetName);
                //GenerateFormulaWorksheet(pck.Workbook, transformPlug.Facets);
            }


            byte[] buffer = pck.GetAsByteArray();

            return buffer;
        }


        public static void WriteTransformPlugToHttp(ITransformPlug transformPlug, HttpListenerResponse response)
        {
            byte[] b = GenerateExcelFromTransformPlug(transformPlug);
            response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            response.AddHeader("content-disposition", string.Format("attachment;  filename={0}.xlsx", transformPlug.SourceDocument));

            response.ContentLength64 = b.Length;
            response.OutputStream.Write(b, 0, b.Length);
            response.OutputStream.Close();
        }

        public static void WriteTransformPlugToHttp(ITransformPlug transformPlug, HttpResponseBase response)
        {
            byte[] b = GenerateExcelFromTransformPlug(transformPlug);
            response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            response.AddHeader("content-disposition", string.Format("attachment;  filename={0}.xlsx", transformPlug.SourceDocument));

            response.OutputStream.Write(b, 0, b.Length);
            response.OutputStream.Close();
        }


        const int SegmentNameIndex = 1;
        const int TargetFieldIndex = 2;
        const int SourceFieldIndex = 3;
        const int TypeIndex = 4;
        const int GroupNameIndex = 5;
        const int TargetRecordIndex = 6;
        const int SourceRecordIndex = 7;



        //const int NotesIndex = 5;
        //const int LinkGroupNameIndex = 5;
        const string LinkSourceColumn = "C";
        const string AutoFilterFormat = "A1:G{0}";

        static void GenerateWorksheetFromFacets(ExcelWorkbook workBook, IList<ITransformGroup> facets, string sourceDocName, string targetDocName)
        {
            string name = string.Format("{0} -> {1}", sourceDocName, targetDocName);

            var ws = workBook.Worksheets.Add(name);
            ws.View.ShowGridLines = true;

            ws.Column(SegmentNameIndex).Width = 10;
            ws.Column(SourceFieldIndex).Width = 10;
            ws.Column(TargetFieldIndex).Width = 10;
            ws.Column(TypeIndex).Width = 8;
            ws.Column(GroupNameIndex).Width = 10;
            ws.Column(TargetRecordIndex).Width = 15;
            ws.Column(SourceRecordIndex).Width = 15;


            //ws.Column(NotesIndex).Width = 10;

            //Headers
            ws.Cells["A1"].Value = "Segment";
            ws.Cells["B1"].Value = "Target Field";
            ws.Cells["C1"].Value = "Source Field";
            ws.Cells["D1"].Value = "Type";
            ws.Cells["E1"].Value = "Group Name";
            ws.Cells["F1"].Value = "Target Record";
            ws.Cells["G1"].Value = "Source Record";
            ws.Cells["A1:G1"].Style.Font.Bold = true;

            ws.View.FreezePanes(2, 1);
            ws.Select("A1");

            //Start at row 2;
            int row = 2;

            foreach (ITransformGroup group in facets)
            {
                Dictionary<string, ITransformLink> referencedLinkMap;
                Dictionary<string, IFormula> formulaMap;
                GetReferencedLinksAndFormula(group, out referencedLinkMap, out formulaMap);

                //Pass1
                //There are 2 things to be done in this pass
                //1. Set LinkName, Src and Target colums for Document XPath references
                //2. If Target contains formula, set formula.Address to TargetFieldIndex

                string fieldName, recordName;
                string prevTargetRecordName = string.Empty;
                int level = 0;
                foreach (ITransformLink link in group.Links)
                {
                    if (link.Ignore) continue;

                    //set the link address to the base row
                    //All references from formula are to either the source or target of link, which can be
                    //derived from the link base address

                    //ws.Cells[row, LinkGroupNameIndex].Value = link.Name;



                    if (link.Target.ReferenceType == ReferenceType.Document)
                    {
                        GetFieldAndRecordName(link.Target.Name, out fieldName, out recordName);

                        /*
                        if (recordName != prevTargetRecordName)
                        {
                            //a new record is being encountered
                            //reset level to 0 and create a header row
                            level = 1;
                            ws.Cells[row, SegmentNameIndex].Value = recordName;
                            ws.Row(row).OutlineLevel = level;
                            ws.Row(row).Collapsed = true;
                            prevTargetRecordName = recordName;
                            row++;
                        }

                        else
                        {
                            level = 2;
                        }
                         */

                        ws.Cells[row, TargetFieldIndex].Value = fieldName;
                        ws.Cells[row, TargetRecordIndex].Value = recordName;
                        ws.Cells[row, TypeIndex].Value = "Data Copy";
                        //ws.Row(row).OutlineLevel = level;
                        //ws.Row(row).Collapsed = true;
                    }

                    else if (link.Target.ReferenceType == ReferenceType.Formula)
                    {
                        string formulaAddress = GetCellAddress(ws, row, TargetFieldIndex);
                        IFormula formula = formulaMap[link.Target.Name];
                        if (string.IsNullOrEmpty(formula.Address))
                        {
                            formula.Address = formulaAddress;
                        }

                        //ws.Cells[row, TargetFieldIndex].Value = ToString(formula);
                        ws.Cells[row, TargetFieldIndex].Style.Font.Bold = true;
                        ws.Cells[row, TypeIndex].Value = "Formula";
                    }

                    if (link.Source.ReferenceType == ReferenceType.Document)
                    {
                        GetFieldAndRecordName(link.Source.Name, out fieldName, out recordName);

                        ws.Cells[row, SourceFieldIndex].Value = fieldName;
                        ws.Cells[row, SourceRecordIndex].Value = recordName;
                        ws.Cells[row, TypeIndex].Value = "Data Copy";
                        //ws.Row(row).OutlineLevel = level;
                        //ws.Row(row).Collapsed = true;
                    }

                    else if (link.Source.ReferenceType == ReferenceType.Formula)
                    {
                        ws.Cells[row, SourceFieldIndex].Style.Font.Bold = true;
                        ws.Cells[row, TypeIndex].Value = "Formula";
                    }

                    link.Address = row.ToString();
                    ws.Cells[row, GroupNameIndex].Value = group.Name;

                    row++;
                }

                //Pass2
                //2nd pass, populate the address for links whose source contains a formula
                //Given that this formula is the source of value, it must have been the target
                //somewhere. Find that part and add it's address
                foreach (ITransformLink link in group.Links)
                {
                    if (link.Ignore) continue;

                    int rowNum = int.Parse(link.Address);
                    if (link.Source.ReferenceType == ReferenceType.Formula)
                    {
                        IFormula formula = formulaMap[link.Source.Name];
                        if (string.IsNullOrEmpty(formula.Address))
                            throw new ArgumentNullException("formula has null address");
                        ws.Cells[rowNum, SourceFieldIndex].Formula = formula.Address;
                    }
                }

                //Pass3
                //Now populate the address of the parameters in each formula
                foreach (IFormula formula in group.Formulas)
                {
                    if (formula.Ignore) continue;

                    foreach (IParameter param in formula.Parameters)
                    {
                        if (param.Reference.ReferenceType == ReferenceType.Document)
                        {
                            ITransformLink link = referencedLinkMap[param.Reference.Name];

                            //B stands for sourceColumns. Todo: remove hardcoding and programmatically 
                            //determine the columns letter.User may have moved around columns
                            param.Address = LinkSourceColumn + link.Address;
                        }
                    }

                    //ExcelRange range = ws.Cells[formula.Address];
                    //range.Value = ToString(formula);
                }

                //Pass4
                //Render the formula
                foreach (ITransformLink link in group.Links)
                {
                    if (link.Ignore) continue;

                    int rowNum = int.Parse(link.Address);
                    if (link.Target.ReferenceType == ReferenceType.Formula)
                    {
                        IFormula formula = formulaMap[link.Target.Name];
                        if (string.IsNullOrEmpty(formula.Address))
                            throw new ArgumentNullException("formula has null address");
                        bool isFormulaRender;
                        string expression = ToString(formula, out isFormulaRender);
                        if (isFormulaRender)
                        {
                            ws.Cells[rowNum, TargetFieldIndex].Formula = expression;
                        }

                        else
                        {
                            ws.Cells[rowNum, TargetFieldIndex].Value = expression;
                        }
                    }
                }

            }

            string filterRange = string.Format(AutoFilterFormat, row.ToString());
            ws.Cells[filterRange].AutoFilter = true;
            ws.Cells[1, 1, row, 7].AutoFitColumns();
        }


        static string GetCellAddress(ExcelWorksheet ws, int rowIndex, int columnIndex)
        {
            ExcelRange range = ws.Cells[rowIndex, columnIndex];
            return range.Address;
        }

        const string PathSeperator = @"->";
        private static void GetFieldAndRecordName(string fullName, out string fieldName, out string recordName)
        {
            int index = fullName.LastIndexOf(PathSeperator);

            if (index >= 0)
            {
                recordName = fullName.Substring(0, index);
                fieldName = fullName.Substring(index + PathSeperator.Length);
            }

            else
            {
                recordName = string.Empty;
                fieldName = fullName;
            }
        }


        static string ToString(IFormula formula, out bool isFormulaContent)
        {
            isFormulaContent = false;
            string expression = null;
            switch (formula.FormulaType)
            {
                case FormulaType.Equality:
                    expression = ToStringEqualityFormula(formula, out isFormulaContent);
                    break;

                case FormulaType.LogicalOr:
                    expression = ToStringLogicalOrFormula(formula, out isFormulaContent);
                    break;

                case FormulaType.ValueMapping:
                    expression = ToStringValueMappingFormula(formula, out isFormulaContent);
                    break;

            }

            if (!isFormulaContent)
            {
                StringBuilder sb = new StringBuilder(50);
                string data = string.Format("Name: {0}, Address: {1}, Type: {2}", formula.Name, formula.Address, formula.FormulaType);
                sb.Append(data);

                foreach (IParameter par in formula.Parameters)
                {
                    sb.AppendLine();
                    sb.Append("  -->Parameter: Address " + par.Address + "Name " + par.Reference.Name + " " + par.Reference.ReferenceType);
                }

                expression = sb.ToString();
            }

            return expression;
        }

        static string ToStringEqualityFormula(IFormula formula, out bool isFormulaContent)
        {
            isFormulaContent = true;
            StringBuilder sb = new StringBuilder(50);

            sb.Append(GetExpressionForParameter(formula.Parameters[0]));
            sb.Append(@"=");
            sb.Append(GetExpressionForParameter(formula.Parameters[1]));
            return sb.ToString();

        }

        static string ToStringLogicalOrFormula(IFormula formula, out bool isFormulaContent)
        {
            isFormulaContent = true;
            StringBuilder sb = new StringBuilder(50);
            IParameter param1 = formula.Parameters[0];
            sb.Append("OR(");
            sb.Append(param1.Address);

            for (int i = 1; i < formula.Parameters.Count; i++)
            {
                sb.Append(",");
                sb.Append(formula.Parameters[i].Address);
            }

            sb.Append(")");
            return sb.ToString();

        }

        static string ToStringValueMappingFormula(IFormula formula, out bool isFormulaContent)
        {
            isFormulaContent = true;
            StringBuilder sb = new StringBuilder(50);
            sb.Append("IF(");
            sb.Append(GetExpressionForParameter(formula.Parameters[0]));

            sb.Append(",");
            sb.Append(GetExpressionForParameter(formula.Parameters[1]));

            sb.Append(")");
            return sb.ToString();

        }


        const string DoubleQuote = @"""";

        static string GetExpressionForParameter(IParameter param)
        {
            string expression = null;
            if (param.Reference.ReferenceType == ReferenceType.Document)
            {
                expression = param.Address;
            }

            else
            {
                expression = DoubleQuote + param.Reference.Name + DoubleQuote;
            }

            return expression;
        }

        static bool GetReferencedLinksAndFormula(ITransformGroup group, out Dictionary<string, ITransformLink> referencedLinkMap, 
            out Dictionary<string, IFormula> formulaMap)
        {
            referencedLinkMap = new Dictionary<string, ITransformLink>();
            foreach (ITransformLink link in group.Links)
            {
                if (link.Source.ReferenceType == ReferenceType.Formula
                    || link.Target.ReferenceType == ReferenceType.Formula)
                {
                    referencedLinkMap[link.Name] = link;
                }
            }

            formulaMap = new Dictionary<string, IFormula>();
            foreach (IFormula formula in group.Formulas)
            {
                formulaMap[formula.Name] = formula;
            }

            return true;
        }


        const int DescriptionIndex = 1;
        const int FormulaTypeIndex = 2;
        const int ParametersIndex = 3;
        const int ExpressionIndex = 4;
        const int IgnoreIndex = 5;

        public static byte[] GenerateFormulaWorksheet(Stream stream, IList<ITransformGroup> facets)
        {
            using (ExcelPackage pck = new ExcelPackage(stream))
            {
                ExcelWorkbook workBook = pck.Workbook;

                var ws = workBook.Worksheets.Add("FormulaDetails");
                ws.View.ShowGridLines = true;

                ws.Column(DescriptionIndex).Width = 15;
                ws.Column(FormulaTypeIndex).Width = 15;
                ws.Column(ParametersIndex).Width = 100;
                ws.Column(ExpressionIndex).Width = 15;
                ws.Column(IgnoreIndex).Width = 15;



                //ws.Column(NotesIndex).Width = 10;

                //Headers
                ws.Cells["A1"].Value = "Description";
                ws.Cells["B1"].Value = "Formula Type";
                ws.Cells["C1"].Value = "Parameters";
                ws.Cells["D1"].Value = "Expression";
                ws.Cells["E1"].Value = "Ignore";

                ws.Cells["A1:E1"].Style.Font.Bold = true;


                ws.View.FreezePanes(2, 1);
                ws.Select("A1");

                //Start at row 2;
                int row = 2;

                var transformSheet = workBook.Worksheets[3];

                foreach (ITransformGroup group in facets)
                {
                    foreach (IFormula formula in group.Formulas)
                    {
                        if (formula.Ignore)
                            continue;
                        ws.Cells[row, DescriptionIndex].Value = formula.Description;
                        ws.Cells[row, FormulaTypeIndex].Value = formula.FormulaType;

                        //ws.Cells[row, ExpressionIndex].Value = transformSheet.Cells[int.Parse(formula.Address.ToString()), 2].Value.ToString();
                        ws.Cells[row, IgnoreIndex].Value = formula.Ignore;
                        foreach (IParameter param in formula.Parameters)
                        {

                            if (param.Address != null)
                                ws.Cells[row, ParametersIndex].Value += "/" + transformSheet.Cells[int.Parse(param.Address.ToString()), 3].Formula.ToString();
                        }
                        row++;
                    }
                }
                byte[] buffer = pck.GetAsByteArray();

                return buffer;
            }
        }
    }


}