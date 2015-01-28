using OfficeOpenXml;

namespace Maarg.Fatpipe.Plug.Authoring
{
    public static class GCExcelReaderHelper
    {
        public static string ReadCell(ExcelWorksheet schemaWorksheet, int row, int col)
        {
            return ReadCell(schemaWorksheet, row, col, false);
        }

        public static string ReadCell(ExcelWorksheet schemaWorksheet, int row, int col, bool convertToUpperCase)
        {
            ExcelRange range = schemaWorksheet.Cells[row, col];
            object value = schemaWorksheet.Cells[row, col].Value;

            if (value == null)
                return null;

            string strValue = value.ToString();

            if (convertToUpperCase)
                strValue.ToUpperInvariant();

            return strValue;
        }

        public static string GetColumnIndex(int columnIndex)
        {
            const string alphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            return alphabets[columnIndex - 1].ToString();
        }
    }
}
