using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;


namespace Maarg.Fatpipe.Plug.DataModel
{
    public class DataTypeFactory
    {
        public static X12BaseDataType CreateDataTypeFromXmlSchema2(XmlSchemaElement element, bool bTestMode)
        {
            X12BaseDataType dataType = X12DataTypeFactory.CreateX12DataTypeFromXmlSchema2(element, bTestMode);
            return dataType;
        }

        public static X12BaseDataType CreateDataTypeFromXmlSchema(XmlSchemaElement element, bool bTestMode, string dataTypeName, string stringMinL, string stringMaxL)
        {
            X12BaseDataType dataType = X12DataTypeFactory.CreateX12DataTypeFromXmlSchema(element, bTestMode, dataTypeName, stringMinL, stringMaxL);
            return dataType;
        }
    }


    #region X12DataTypeFactory
    /*
	 * This is a factory for creating X12 Data Types. The following types
	 * are supported
	 * 
	 * 1) R - real
	 * 2) Nn - integer with 'n' implied decimal point
	 * 3) AN - alphanumeric
	 * 4) ID - enumeration list
	 * 5) Date
	 * 6) Time
	 * 7) B - binary data type - not currently supported
	 */
    public class X12DataTypeFactory
    {
        public const string R_DataTypeName = "X12_R";
        public const string AN_DataTypeName = "X12_AN";
        public const string N_DataTypeName = "X12_N";
        public const string ID_DataTypeName = "X12_ID";
        public const string Date_DataTypeName = "X12_DT";
        public const string Time_DataTypeName = "X12_TM";

        public const string EFACT_A_DataTypeName = "EDIFACT_A";
        public const string EFACT_AN_DataTypeName = "EDIFACT_AN";
        public const string EFACT_N_DataTypeName = "EDIFACT_N";


        public const string RNew_DataTypeName = "R";
        public const string ANNew_DataTypeName = "AN";
        public const string NNew_DataTypeName = "N";
        public const string IDNew_DataTypeName = "ID";
        public const string DateNew_DataTypeName = "DT";
        public const string TimeNew_DataTypeName = "TM";

        public static X12BaseDataType CreateX12DataTypeFromXmlSchema(XmlSchemaElement element, bool bDesignTime, string dataTypeName, string stringMinL, string stringMaxL)
        {
            if (string.IsNullOrEmpty(dataTypeName))
            {
                dataTypeName = "STRING";
            }

            else
            {
                dataTypeName = dataTypeName.ToUpper();
            }

            X12BaseDataType dataType = null;
            int minL, maxL;
            minL = maxL = -1;
            Dictionary<string, string> allowedValues = null;

            if (string.Equals(dataTypeName, IDNew_DataTypeName, StringComparison.OrdinalIgnoreCase))
            {
                allowedValues = DataTypeHelper.RetrieveFacetMetadata(element, stringMinL, stringMaxL, out minL, out maxL);
            }

            switch (dataTypeName)
            {
                case RNew_DataTypeName:
                    dataType = new X12_RDataType(RNew_DataTypeName, minL, maxL);
                    break;

                case "STRING":
                case "SYSTEM.STRING":
                case EFACT_A_DataTypeName:
                case EFACT_AN_DataTypeName:
                case ANNew_DataTypeName:
                    dataType = new X12_AnDataType(ANNew_DataTypeName, minL, maxL);
                    break;

                case IDNew_DataTypeName:
                    dataType = new X12_IdDataType(IDNew_DataTypeName, allowedValues);
                    break;

                case DateNew_DataTypeName:
                    dataType = new X12_DtDataType(DateNew_DataTypeName, minL, maxL);
                    break;

                case TimeNew_DataTypeName:
                    dataType = new X12_TmDataType(TimeNew_DataTypeName, minL, maxL);
                    break;

                case EFACT_N_DataTypeName:
                case NNew_DataTypeName:
                    dataType = X12_NDataType.GetDataTypeWithPrecision(0, minL, maxL);
                    break;

                default:
                    if (dataTypeName.Length == 2 && dataTypeName.StartsWith("N", StringComparison.Ordinal) &&
                        dataTypeName[1] >= '0' && dataTypeName[1] <= '9')
                    {
                        dataType = X12_NDataType.GetDataTypeWithPrecision(dataTypeName[1] - '0',
                            minL, maxL);
                    }

                    else
                    {
                        throw new Exception(string.Format("{0} data type is not supported", dataTypeName));
                    }

                    break;

            }

            return dataType;
        }
        public static X12BaseDataType CreateX12DataTypeFromXmlSchema2(XmlSchemaElement element, bool bDesignTime)
        {
            string dataTypeName;
            X12BaseDataType dataType = null;
            XmlSchemaSimpleTypeRestriction restriction = null;
            int minL, maxL;
            minL = maxL = -1;
            Dictionary<string, string> sortedList = null;

            if (element.ElementType is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType sType = (XmlSchemaSimpleType)element.ElementType;
                XmlQualifiedName name = sType.QualifiedName;
                dataTypeName = sType.Name;

                restriction = sType.Content as XmlSchemaSimpleTypeRestriction;
                if (restriction != null)
                {
                    dataTypeName = name.Name != null && name.Name.StartsWith(ID_DataTypeName, StringComparison.Ordinal)
                        ? ID_DataTypeName : restriction.BaseTypeName.Name;
                    sortedList = DataTypeHelper.RetrieveFacets(restriction, out minL, out maxL);
                }
                //Console.WriteLine("Element name = " + element.Name + " SimpleType name = " + dataTypeName);
            }

            else
            {
                XmlSchemaDatatype dType = (XmlSchemaDatatype)element.ElementType;
                dataTypeName = dType.ValueType.ToString();
                //Console.WriteLine("Element name = " + element.Name + " DataType name = " + dType.ValueType);
            }

            switch (dataTypeName.ToUpper())
            {
                case R_DataTypeName:
                    dataType = new X12_RDataType(R_DataTypeName, minL, maxL);
                    break;

                case "STRING":
                case "SYSTEM.STRING":
                case EFACT_A_DataTypeName:
                case EFACT_AN_DataTypeName:
                case AN_DataTypeName:
                    dataType = new X12_AnDataType(AN_DataTypeName, minL, maxL);
                    break;

                case ID_DataTypeName:
                    dataType = new X12_IdDataType(ID_DataTypeName, sortedList);
                    break;

                case Date_DataTypeName:
                    dataType = new X12_DtDataType(Date_DataTypeName, minL, maxL);
                    break;

                case Time_DataTypeName:
                    dataType = new X12_TmDataType(Time_DataTypeName, minL, maxL);
                    break;

                case EFACT_N_DataTypeName:
                case N_DataTypeName:
                    dataType = X12_NDataType.GetDataTypeWithPrecision(0, minL, maxL);
                    break;

                default:
                    if (dataTypeName.Length == 6 && dataTypeName.StartsWith("X12_N", StringComparison.Ordinal) &&
                        dataTypeName[5] >= '0' && dataTypeName[5] <= '9')
                    {
                        dataType = X12_NDataType.GetDataTypeWithPrecision(dataTypeName[5] - '0',
                            minL, maxL);
                    }
                   
                    else
                    {
                        //throw new Exception(string.Format("{0} data type is not supported", dataTypeName));
                        dataType = new X12_AnDataType(AN_DataTypeName, -1, -1);
                    }

                    break;

            }

            return dataType;
        }
    }
    #endregion

    public class DataTypeHelper
    {
        public static FieldError GenerateFieldError(int errorCode, string errorDescription, string dataInError)
        {
            FieldError fieldError = new FieldError(string.Empty, -1, -1, -1, errorCode, errorDescription, -1, dataInError, string.Empty);
            return fieldError;
        }

        public static bool IsDigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        public static Dictionary<string, string> RetrieveFacetMetadata(XmlSchemaElement element, string stringMinL, string stringMaxL, 
            out int minL, out int maxL)
        {
            minL = ParseInt(stringMinL);
            maxL = ParseInt(stringMaxL);
            Dictionary<string, string> allowedValues = null;

            XmlSchemaSimpleTypeRestriction sr = null;
            if (element.ElementType is XmlSchemaSimpleType)
            {
                sr = (element.ElementType as XmlSchemaSimpleType).Content as XmlSchemaSimpleTypeRestriction;
            }
            if (sr == null) return allowedValues;

            XmlSchemaObjectCollection objC = sr.Facets;

            foreach (object obj in objC)
            {
                if (obj is XmlSchemaEnumerationFacet)
                {
                    if (allowedValues == null)
                    {
                        allowedValues = new Dictionary<string, string>(10);
                    }

                    string val = ((XmlSchemaEnumerationFacet)obj).Value;
                    string description = XmlSchemaHelper.ReadDocumentationFromEnumeration((XmlSchemaEnumerationFacet)obj);
                    allowedValues[val] = description;
                }
            }

            return allowedValues;
        }

        private static int ParseInt(string data)
        {
            int result;
            bool success = int.TryParse(data, out result);
            if (!success) result = -1;
            return result;
        }

        /*
         * This method is used during schema construction and dataType (in SimpleField) to retrieve 
         * various facets from a XmlSchemaSimpleTypeRestriction object. Typically, they are 
         * 
         * 1) minLength
         * 2) maxLength
         * 3) Length
         * 4) Enumeration
         */
        public static Dictionary<string, string> RetrieveFacets(XmlSchemaSimpleTypeRestriction sr, out int minL, out int maxL)
        {
            minL = maxL = -1;
            Dictionary<string, string> allowedValues = null;

            if (sr == null) return allowedValues;

            XmlSchemaObjectCollection objC = sr.Facets;

            foreach (object obj in objC)
            {
                if (obj is XmlSchemaMinLengthFacet)
                {
                    minL = int.Parse(((XmlSchemaMinLengthFacet)obj).Value, CultureInfo.CurrentCulture);
                }

                else if (obj is XmlSchemaMaxLengthFacet)
                {
                    maxL = int.Parse(((XmlSchemaMaxLengthFacet)obj).Value, CultureInfo.CurrentCulture);
                }

                else if (obj is XmlSchemaLengthFacet)
                {
                    minL = int.Parse(((XmlSchemaLengthFacet)obj).Value, CultureInfo.CurrentCulture);
                    maxL = minL;
                }

                else if (obj is XmlSchemaEnumerationFacet)
                {
                    if (allowedValues == null)
                    {
                        allowedValues = new Dictionary<string, string>(10);
                    }

                    string val = ((XmlSchemaEnumerationFacet)obj).Value;
                    allowedValues[val] = val;
                }
            }

            return allowedValues;
        }

    }
}
