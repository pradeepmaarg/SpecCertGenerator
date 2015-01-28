using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;

using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Style.XmlAccess;
using OfficeOpenXml.Table;
using Maarg.Fatpipe.Plug.DataModel;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class DocumentPlugtoSpecCert
    {
        const int LoopIndex = 2;
        const int SegmentIndex = 3;
        const int SegmentDescriptionIndex = 4;
        const int DataIndex = 4;
        const int DataDescriptionIndex = 5;
        const int DataTypeIndex = 7;
        const int MinMaxIndex = 8;
        const int PathIndex = 38;
        const int SupportedNestedLevels = 2;

        const string AutoFilterFormat = "A1:L{0}";

        public static void GenerateWorksheetFromDocumentPlug(ExcelWorkbook workBook, IDocumentPlug plug, int startRow, bool usingTemplate)
        {
            ExcelWorksheet ws;

            if (usingTemplate)
            {
                ws = workBook.Worksheets[1];
            }
            else
            {
                //Add the Content sheet
                string name = plug.Name;
                ws = workBook.Worksheets.Add(name);
            }

            ws.View.ShowGridLines = true;
            ws.OutLineSummaryRight = true;

            int row = startRow;

            int lastRow = AddPluglet(ws, plug, ref row, 0);
            ws.OutLineSummaryBelow = false;
        }
        
        public static void GenerateExcelFromDocumentPlug(string outputFilePath, IDocumentPlug plug)
        {
            Console.WriteLine("Generating plug in " + outputFilePath);
            FileInfo newFile = new FileInfo(outputFilePath);
            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(outputFilePath);
            }

            //Create the workbook
            ExcelPackage pck = new ExcelPackage(newFile);
            GenerateWorksheetFromDocumentPlug(pck.Workbook, plug, 1, false);

            //Done! save the sheet
            pck.Save();

            Console.WriteLine("Plug written to " + outputFilePath);
        }

        public static void GenerateExcelFromDocumentPlug(string outputFilePath, string templateFilePath, IDocumentPlug plug)
        {
            int startRow = 8;
            Console.WriteLine("Generating plug in " + outputFilePath);
            FileInfo newFile = new FileInfo(outputFilePath);
            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(outputFilePath);
            }

            FileInfo templateFile = new FileInfo(templateFilePath);

            //Create the workbook
            ExcelPackage pck = new ExcelPackage(templateFile);
            GenerateWorksheetFromDocumentPlug(pck.Workbook, plug, startRow, true);

            //Done! save the sheet
            pck.SaveAs(newFile);

            Console.WriteLine("Plug written to " + outputFilePath);
        }

        private static int AddPluglet(ExcelWorksheet ws, IDocumentPlug plug, ref int row, int level)
        {
            foreach (IPluglet child in plug.RootPluglet.Children)
            {
                AddPluglet(ws, child, ref row, level);
            }

            return row;
        }

        private static int AddPluglet(ExcelWorksheet ws, IPluglet rootPluglet, ref int row, int level)
        {
            IPluglet parent = rootPluglet.Parent;

            string loopName = string.Empty;

            switch(rootPluglet.PlugletType)
            {
                case PlugletType.Segment:
                    loopName = GetLoopName(parent);
                    if(!string.IsNullOrWhiteSpace(loopName))
                        ws.Cells[row, LoopIndex].Value = loopName;

                    // Grouping name
                    ws.Cells[row, 1].Value = "TBD";

                    ws.Cells[row, SegmentIndex].Value = rootPluglet.Tag;
                    ws.Cells[row, SegmentDescriptionIndex].Value = string.IsNullOrWhiteSpace(rootPluglet.Definition) ? rootPluglet.Tag : rootPluglet.Definition;
                    ws.Cells[row, SegmentDescriptionIndex, row, SegmentDescriptionIndex + 1].Merge = true;
                    ws.Cells[row, SegmentDescriptionIndex + 2, row, SegmentDescriptionIndex + 6].Merge = true;
                    ws.Cells[row, 1, row, PathIndex-1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, PathIndex-1].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    break;

                case PlugletType.Data:
                    ws.Cells[row, DataIndex].Value = rootPluglet.Tag;
                    ws.Cells[row, DataDescriptionIndex].Value = rootPluglet.Definition;
                    ws.Cells[row, DataTypeIndex].Value = GetDataType(rootPluglet.DataType);
                    ws.Cells[row, MinMaxIndex].Value = GetDataTypeMinMax(rootPluglet.DataType);
                    break;

                case PlugletType.CompositeData:
                case PlugletType.Unknown:
                    return row;
                    break;
            }

            ws.Cells[row, PathIndex].Value = rootPluglet.Path;
            ws.Cells[row, 1, row, PathIndex+1].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            ws.Cells[row, 1, row, PathIndex+1].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[row, 1, row, PathIndex+1].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            ws.Cells[row, 1, row, PathIndex+1].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            // For path cells set wrap text to false
            ws.Cells[row, PathIndex].Style.WrapText = false;
            ws.Cells[row, PathIndex+1].Style.WrapText = false;

            int nextLevel = level;
            if (rootPluglet.PlugletType != PlugletType.Loop)
            {
                ws.Row(row).OutlineLevel = level;
                row++;
                nextLevel++;
            }

            //Add children
            foreach (IPluglet attr in rootPluglet.Attributes)
            {
                Console.WriteLine("Attributes: {0}", attr.Name);
            }

            if (rootPluglet.PlugletType != PlugletType.Data)
            {
                foreach (IPluglet child in rootPluglet.Children)
                {
                    // Only allow 1 level of nested loop - See GetLoopName function
                    bool allowNestedLoop = LoopAllowed(child);

                    if ((child.PlugletType == PlugletType.Loop && !allowNestedLoop) 
                        || (child.PlugletType == rootPluglet.PlugletType && child.PlugletType == PlugletType.Segment)
                        || child.PlugletType == PlugletType.CompositeData)
                        continue;

                    AddPluglet(ws, child, ref row, nextLevel);
                }
            }
            
            return row;
        }

        private static bool LoopAllowed(IPluglet child)
        {
            bool allowLoop = false;
            int level = 0;

            while (child.PlugletType == PlugletType.Loop && child.Parent != null)
            {
                level++;
                child = child.Parent;
            }

            // +1 for root node (X12)
            if (level <= (SupportedNestedLevels+1))
                allowLoop = true;

            return allowLoop;
        }

        private static string GetLoopName(IPluglet parent)
        {
            string loopName = string.Empty;
            int level = 0;

            while (parent.PlugletType == PlugletType.Loop && parent.Parent != null)
            {
                if (level == 0)
                    loopName = parent.Name;
                else
                    loopName = string.Format("{0}->{1}", parent.Name, loopName);

                level++;
                if (level == SupportedNestedLevels)
                    break;

                parent = parent.Parent;
            }

            return loopName;
        }

        private static string GetDataType(X12BaseDataType x12BaseDataType)
        {
            string dataType = string.Empty;
            if (x12BaseDataType is X12_AnDataType || x12BaseDataType is X12_IdDataType)
                dataType = "AN";
            else if (x12BaseDataType is X12_DtDataType)
                dataType = "DT";
            else if (x12BaseDataType is X12_TmDataType)
                dataType = "TM";
            else if (x12BaseDataType is X12_RDataType)
                dataType = "R";
            else if (x12BaseDataType is X12_NDataType)
                dataType = "N";

            return dataType;
        }

        private static string GetDataTypeMinMax(X12BaseDataType dataType)
        {
            string minMax = string.Empty;

            if (!(dataType.MinLength == dataType.MaxLength && dataType.MaxLength == 0))
                minMax = string.Format("{0}/{1}", dataType.MinLength, dataType.MaxLength);

            return minMax;
        }
    }
}
