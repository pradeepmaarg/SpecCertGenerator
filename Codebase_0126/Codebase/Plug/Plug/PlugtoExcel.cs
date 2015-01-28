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


namespace Maarg.Fatpipe.Plug.DataModel.DataModel
{
    public class PlugtoExcel
    {
    }

    public class FilesystemStatsInExcel
    {
        
        /// <summary>
        /// Reads the filesystem and makes a report.
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <param name="dir">Directory to scan</param>
        /// <param name="depth">How many levels?</param>
        /// <param name="skipIcons">Skip the icons in column A. A lot faster</param>
        public static string Render(string outputFilePath, string sourceDirPath, int maxDepth)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirPath);

            FileInfo newFile = new FileInfo(outputFilePath);
            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(outputFilePath);
            }

            //Create the workbook
            ExcelPackage pck = new ExcelPackage(newFile);

            //Add the Content sheet
            var ws = pck.Workbook.Worksheets.Add("Content");

            ws.View.ShowGridLines = false;

            ws.Column(1).Width = 60;
            ws.Column(2).Width = 16;
            ws.Column(3).Width = 20;
            ws.Column(4).Width = 20;

            //This set the outline for column 4 and 5 and hide them
            ws.Column(3).OutlineLevel = 1;
            ws.Column(3).Collapsed = true;
            ws.Column(4).OutlineLevel = 1;
            ws.Column(4).Collapsed = true;
            ws.OutLineSummaryRight = true;

            //Headers
            ws.Cells["A1"].Value = "Name";
            ws.Cells["B1"].Value = "Size";
            ws.Cells["C1"].Value = "Created";
            ws.Cells["D1"].Value = "Last modified";
            ws.Cells["A1:D1"].Style.Font.Bold = true;

            ws.View.FreezePanes(2, 1);
            ws.Select("A1");

            //Start at row 2;
            int row = 2;

            //Load the directory content to sheet 1
            row = AddDirectory(ws, sourceDir, row, 0, maxDepth);
            ws.OutLineSummaryBelow = false;

            //Format columns
            ws.Cells[1, 2, row - 1, 2].Style.Numberformat.Format = "#,##0";
            ws.Cells[1, 3, row - 1, 3].Style.Numberformat.Format = "yyyy-MM-dd hh:mm";
            ws.Cells[1, 4, row - 1, 4].Style.Numberformat.Format = "yyyy-MM-dd hh:mm";

            //Add the textbox
            var shape = ws.Drawings.AddShape("txtDesc", eShapeStyle.Rect);
            shape.SetPosition(1, 5, 6, 5);
            shape.SetSize(400, 200);

            shape.Text = @"Directory stats of c:\data\temp";
            shape.Fill.Style = eFillStyle.SolidFill;
            shape.Fill.Color = Color.DarkSlateGray;
            shape.Fill.Transparancy = 20;
            shape.Border.Fill.Style = eFillStyle.SolidFill;
            shape.Border.LineStyle = eLineStyle.LongDash;
            shape.Border.Width = 1;
            shape.Border.Fill.Color = Color.Black;
            shape.Border.LineCap = eLineCap.Round;
            shape.TextAnchoring = eTextAnchoringType.Top;
            shape.TextVertical = eTextVerticalType.Horizontal;
            shape.TextAnchoringControl = false;
            ws.Cells[1, 2, row, 5].AutoFitColumns();

            //Add the graph sheet
            //AddGraphs(pck, row, dir.FullName);

            //Add a HyperLink to the statistics sheet. 
            //var namedStyle = pck.Workbook.Styles.CreateNamedStyle("HyperLink");   //This one is language dependent
            //namedStyle.Style.Font.UnderLine = true;
            //namedStyle.Style.Font.Color.SetColor(Color.Blue);
            //ws.Cells["K13"].Hyperlink = new ExcelHyperLink("Statistics!A1", "Statistics");
            //ws.Cells["K13"].StyleName = "HyperLink";

            //Printer settings
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;
            ws.PrinterSettings.RepeatRows = new ExcelAddress("1:1"); //Print titles
            ws.PrinterSettings.PrintArea = ws.Cells[1, 1, row - 1, 5];

            //Done! save the sheet
            pck.Save();

            return newFile.FullName;
        }
       
       
       
       

        private static int AddDirectory(ExcelWorksheet ws, DirectoryInfo dir, int row, int level, int maxDepth)
        {
            
            Console.WriteLine("Directory " + dir.Name);
            
            ws.Cells[row, 1].Value = dir.Name;
            ws.Cells[row, 3].Value = dir.CreationTime;
            ws.Cells[row, 4].Value = dir.LastAccessTime;

            ws.Cells[row, 1, row, 4].Style.Font.Bold = true;
            //Sets the outline depth
            ws.Row(row).OutlineLevel = level;

            int prevRow = row;
            row++;
            //Add subdirectories
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (level < maxDepth)
                {
                    row = AddDirectory(ws, subDir, row, level + 1, maxDepth);
                }
            }

            //Add files in the directory
            foreach (FileInfo file in dir.GetFiles())
            {
                ws.Cells[row, 1].Value = file.Name;
                ws.Cells[row, 2].Value = file.Length;
                ws.Cells[row, 3].Value = file.CreationTime;
                ws.Cells[row, 4].Value = file.LastAccessTime;

                ws.Row(row).OutlineLevel = level + 1;

                //AddStatistics(file);

                row++;
            }

            //Add a subtotal for the directory
            if (row - 1 > prevRow)
            {
                ws.Cells[prevRow, 2].Formula = string.Format("SUBTOTAL(9, {0})", ExcelCellBase.GetAddress(prevRow + 1, 2, row - 1, 2));
            }
            else
            {
                ws.Cells[prevRow, 2].Value = 0;
            }

            return row;
        }
      
      
    }
}
