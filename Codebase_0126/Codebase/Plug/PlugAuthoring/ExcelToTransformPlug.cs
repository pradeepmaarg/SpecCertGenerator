using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using System.IO;
using Maarg.Fatpipe.Plug.DataModel;
using System.Text.RegularExpressions;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public class ExcelToTransformPlug
    {
        const int SegmentNameIndex = 1;
        const int TargetFieldIndex = 2;
        const int SourceFieldIndex = 3;
        const int TypeIndex = 4;
        const int GroupNameIndex = 5;
        const int TargetRecordIndex = 6;
        const int SourceRecordIndex = 7;

        const string PathSeperator = @"->";

        //static string RepositoryRoot  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\");

        //static StreamWriter f = new StreamWriter("test.txt");

        public static ITransformPlug LoadTransformPlugFromExcel(Stream stream)
        {

            //String path = Path.Combine(RepositoryRoot, @"sources\test\PlugTestHarness\output\Plug_X12850To810.xlsx");
            //FileInfo fileInfo = new FileInfo(path);
            using (ExcelPackage pck = new ExcelPackage(stream))
            {
                ExcelWorkbook workBook = pck.Workbook;
                ExcelWorksheet current = workBook.Worksheets[3];
                return GenerateTransformPlugFromExcel(current);

            }



        }

        public static ITransformPlug GenerateTransformPlugFromExcel(ExcelWorksheet current)
        {
            ITransformPlug plug = new TransformPlug(null, null, null);
            int rowCount = current.Dimension.End.Row - current.Dimension.Start.Row + 1;

            string previousGroupName = string.Empty;
            string currentGroupName = string.Empty;
            for (int row = 2; row < rowCount; row++)
            {
                if (current.Cells[row, TargetFieldIndex].Value == null)
                    continue;

                //currentGroupName = current.Cells[row, GroupNameIndex].Value.ToString();
                currentGroupName = string.Empty;
                //if (currentGroupName.Equals(previousGroupName))
                //  continue;
                ITransformGroup group = new TransformGroup(currentGroupName);
                plug.Facets.Add(group);
                CreateTransformLinks(current, ref row, ref group);
                previousGroupName = currentGroupName;
            }
            return plug;

            /*
            foreach (ITransformGroup group in plug.Facets)
            {
                foreach (ITransformLink link in group.Links)
                    f.WriteLine(link.Source.Name+"\t"+link.Target.Name);
            }

            f.Close();
             */
        }

        private static void CreateTransformLinks(ExcelWorksheet current, ref int row, ref ITransformGroup group)
        {
            int index = 0;
            while (true)
            {
                if (current.Cells[row, TargetFieldIndex].Value == null)
                    break;
                ITransformLink link = new TransformLink(group.Name + index);
                index++;
                link.Address = row.ToString();

                link.Target.Name = BuildLinkName(current.Cells[row, TargetFieldIndex].Value.ToString(), current.Cells[row, TargetRecordIndex].Value.ToString());

                if (current.Cells[row, TypeIndex].Value.ToString().Equals("Data Copy"))
                {
                    link.Source.Name = BuildLinkName(current.Cells[row, SourceFieldIndex].Value.ToString(), current.Cells[row, SourceRecordIndex].Value.ToString());
                }

                //code to handle missing fields

                
                if (current.Cells[row, TypeIndex].Value.ToString().Equals("Missing Value"))
                {
                    link.Source.Name = "Missing Name";
                    link.Source.ReferenceType = ReferenceType.Document;
                    link.Target.ReferenceType = ReferenceType.Document;
                    group.Links.Add(link);
                    row++;
                    continue;
                }

                //code to auto generate some fields


                if (current.Cells[row, TypeIndex].Value.ToString().Equals("AutoField"))
                {
                    string value = current.Cells[row, SourceRecordIndex].Value.ToString();

                    switch (value)
                    {
                        case "MAARG.GetInvoiceDate(PartnerID, Invoice#)": Random random = new Random();
                            link.Source.Name = "INV" + random.Next();
                            break;
                        case "MAARG.GetInvoiceNumber(PartnerID)": link.Source.Name = System.Convert.ToDateTime(DateTime.Today).ToString("yyyyMMdd");
                            break;
                    }

                    link.Source.ReferenceType = ReferenceType.Literal;
                    link.Target.ReferenceType = ReferenceType.Document;
                    group.Links.Add(link);
                    row++;
                    continue;
                }
                //finish auto

                if (current.Cells[row, TargetFieldIndex].Formula == null || current.Cells[row, TargetFieldIndex].Formula == "")
                {
                    link.Target.ReferenceType = ReferenceType.Document;
                }
                else
                {
                    link.Target.ReferenceType = ReferenceType.Formula;
                }

                if (current.Cells[row, SourceFieldIndex].Formula == null || current.Cells[row, SourceFieldIndex].Formula == "")
                {
                    link.Source.ReferenceType = ReferenceType.Document;
                }
                else
                {
                    link.Source.ReferenceType = ReferenceType.Formula;
                }
                group.Links.Add(link);
                row++;
            }
        }


        private static void buildFormula(string formulaContent)
        {
            Regex rg = new Regex("");

        }

        private static string BuildLinkName(string fieldName, string recordName)
        {
            return PathSeperator + recordName + PathSeperator + fieldName;
        }
    }
}
