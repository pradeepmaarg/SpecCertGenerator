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
    public class DocumentPlugtoExcel
    {

        const int NodeTypeIndex = 1;
        const int MandatoryIndex = 2;
        const int Level1NameIndex = 3;
        const int Level2NameIndex = 4;
        const int Level3NameIndex = 5;
        const int Level4NameIndex = 6;
        const int Level5NameIndex = 7;
        const int Level6NameIndex = 8;
        const int Level7NameIndex = 9;
        const int Level8NameIndex = 10;
        const int DescriptionIndex = 11;
        const int PathIndex = 12;
        const int RepeatableIndex = 13;

        const string AutoFilterFormat = "A1:L{0}";

        public static void GenerateWorksheetFromDocumentPlug(ExcelWorkbook workBook, IDocumentPlug plug)
        {
            GenerateWorksheetFromDocumentPlug(workBook, plug, null);
        }

        public static void GenerateWorksheetFromDocumentPlug(ExcelWorkbook workBook, IDocumentPlug plug, string nameFormat)
        {
            //Add the Content sheet
            string name = plug.Name;
            if (!string.IsNullOrEmpty(nameFormat))
            {
                name = string.Format(nameFormat, name);
            }


            var ws = workBook.Worksheets.Add(name);
            ws.View.ShowGridLines = true;

            ws.Column(NodeTypeIndex).Width = 10;
            ws.Column(MandatoryIndex).Width = 7;
            ws.Column(Level1NameIndex).Width = 15;
            ws.Column(Level2NameIndex).Width = 15;
            ws.Column(Level3NameIndex).Width = 15;
            ws.Column(Level4NameIndex).Width = 15;
            ws.Column(Level5NameIndex).Width = 15;
            ws.Column(Level6NameIndex).Width = 15;
            ws.Column(Level7NameIndex).Width = 15;
            ws.Column(Level8NameIndex).Width = 15;
            ws.Column(DescriptionIndex).Width = 30;
            ws.Column(PathIndex).Width = 30;
            ws.Column(RepeatableIndex).Width = 7;

            ws.Column(Level5NameIndex).OutlineLevel = 1;
            ws.Column(Level5NameIndex).Collapsed = true;
            ws.Column(Level6NameIndex).OutlineLevel = 1;
            ws.Column(Level6NameIndex).Collapsed = true;
            ws.Column(Level7NameIndex).OutlineLevel = 1;
            ws.Column(Level7NameIndex).Collapsed = true;
            ws.Column(Level8NameIndex).OutlineLevel = 1;
            ws.Column(Level8NameIndex).Collapsed = true;
            ws.OutLineSummaryRight = true;


            //Headers
            ws.Cells["A1"].Value = "NodeType";
            ws.Cells["B1"].Value = "Mandatory";
            ws.Cells["C1"].Value = "Level 1 Name";
            ws.Cells["D1"].Value = "Level 2 Name";
            ws.Cells["E1"].Value = "Level 3 Name";
            ws.Cells["F1"].Value = "Level 4 Name";
            ws.Cells["G1"].Value = "Level 5 Name";
            ws.Cells["H1"].Value = "Level 6 Name";
            ws.Cells["I1"].Value = "Level 7 Name";
            ws.Cells["J1"].Value = "Level 8 Name";
            ws.Cells["K1"].Value = "Description";
            ws.Cells["L1"].Value = "Full Name";
            ws.Cells["M1"].Value = "Is Repeatable";
            ws.Cells["A1:M1"].Style.Font.Bold = true;

            ws.View.FreezePanes(2, 1);
            ws.Select("A1");

            //Start at row 2;
            int row = 2;

            //Load the directory content to sheet 1
            int lastRow = AddPluglet(ws, plug, ref row, 0);
            ws.OutLineSummaryBelow = false;

            string filterRange = string.Format(AutoFilterFormat, lastRow.ToString());
            ws.Cells[filterRange].AutoFilter = true;
            ws.Cells[1, 2, row, 5].AutoFitColumns();

            #region Comments
            //Add the textbox
            /*
            var shape = ws.Drawings.AddShape("txtDesc", eShapeStyle.Rect);
            shape.SetPosition(1, 300, 3, 300);
            shape.SetSize(200, 100);

            shape.Text = @"Dcoument plug of " + plug.Name;
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
             
           


            //Printer settings
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;
            ws.PrinterSettings.RepeatRows = new ExcelAddress("1:1"); //Print titles
            ws.PrinterSettings.PrintArea = ws.Cells[1, 1, row - 1, 5];
            */
            #endregion

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
            GenerateWorksheetFromDocumentPlug(pck.Workbook, plug);

            //Done! save the sheet
            pck.Save();

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
            ws.Cells[row, NodeTypeIndex].Value = rootPluglet.PlugletType;
            ws.Cells[row, MandatoryIndex].Value = rootPluglet.IsRecursiveMandatory ? "Y" : "N";
            ws.Cells[row, Level1NameIndex + level].Value = rootPluglet.Name;
            ws.Cells[row, DescriptionIndex].Value = rootPluglet.Definition;
            ws.Cells[row, PathIndex].Value = rootPluglet.Path;
            ws.Cells[row, RepeatableIndex].Value = rootPluglet.IsRepeatable ? "Y" : "N";

            ws.Cells[row, Level1NameIndex + level].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, Level1NameIndex + level].Style.Fill.BackgroundColor.SetColor(GetColor(rootPluglet.PlugletType));

            //ws.Cells[row, 1, row, 4].Style.Font.Bold = true;
            //Sets the outline depth
            ws.Row(row).OutlineLevel = level;
            ws.Row(row).Collapsed = level >= 3;
            ExcelRange range = ws.Cells[row, PathIndex];
            //Console.WriteLine(range.Address + " " + rootPluglet.Path);
            row++;

            //Add children
            foreach (IPluglet attr in rootPluglet.Attributes)
            {

                if (level < 7)
                {
                    //Console.WriteLine("Getting called " + child.Name + " for row " + row + " level " + (level + 1));

                    AddPluglet(ws, attr, ref row, level + 1, true);
                }
            }
            foreach (IPluglet child in rootPluglet.Children)
            {
                if (level < 7)
                {
                    //Console.WriteLine("Getting called " + child.Name + " for row " + row + " level " + (level + 1));
                    
                    AddPluglet(ws, child, ref row, level + 1);
                }
            }
            
            return row;
        }

        private static int AddPluglet(ExcelWorksheet ws, IPluglet rootPluglet, ref int row, int level, bool isAttribute)
        {
            ws.Cells[row, NodeTypeIndex].Value = rootPluglet.PlugletType;
            ws.Cells[row, MandatoryIndex].Value = rootPluglet.IsMandatory ? "Y" : "N";
            ws.Cells[row, Level1NameIndex + level].Value = rootPluglet.Name;
            ws.Cells[row, DescriptionIndex].Value = rootPluglet.Definition;
            ws.Cells[row, PathIndex].Value = rootPluglet.Path;

            ws.Cells[row, Level1NameIndex + level].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, Level1NameIndex + level].Style.Fill.BackgroundColor.SetColor(GetColor(rootPluglet.PlugletType));

            //ws.Cells[row, 1, row, 4].Style.Font.Bold = true;
            //Sets the outline depth
            ws.Row(row).OutlineLevel = level;
            ws.Row(row).Collapsed = level >= 3;
            ExcelRange range = ws.Cells[row, PathIndex];
            //Console.WriteLine(range.Address + " " + rootPluglet.Path);
            row++;

            //Add children
            foreach (IPluglet attr in rootPluglet.Attributes)
            {

                if (level < 7)
                {
                    //Console.WriteLine("Getting called " + child.Name + " for row " + row + " level " + (level + 1));

                    AddPluglet(ws, attr, ref row, level + 1, true);
                }
            }
            foreach (IPluglet child in rootPluglet.Children)
            {
                if (level < 7)
                {
                    //Console.WriteLine("Getting called " + child.Name + " for row " + row + " level " + (level + 1));

                    AddPluglet(ws, child, ref row, level + 1);
                }
            }

            return row;
        }

        private static Color GetColor(PlugletType type)
        {
            Color fillColor = Color.White;
            switch (type)
            {
                case PlugletType.Loop:
                    fillColor = Color.LightBlue;
                    break;

                case PlugletType.Segment:
                    fillColor = Color.Yellow;
                    break;

                case PlugletType.Data:
                    fillColor = Color.Green;
                    break;

            }
            return fillColor;
        }

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
