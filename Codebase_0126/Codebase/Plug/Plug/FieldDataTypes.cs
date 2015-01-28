using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Maarg.Fatpipe.Plug.DataModel
{
    #region abstract class X12BaseDataType
    /*
	 * This is the base class for all data validators. Each simple field in the
	 * schema has a DataType validator associated with it
	 */
    [Serializable]
	public abstract class X12BaseDataType
	{
        protected string mName;
        protected int minLength;
        protected int maxLength;

        public X12BaseDataType(string name) : this (name, -1, -1)
        {
        }

        public X12BaseDataType(string name, int minL, int maxL)
        {
            mName = name;
            minLength = minL;
            maxLength = maxL;
        }

        public string Name
        {
            get { return mName; }
        }

        public int MinLength
        {
            get { return minLength > 0 ? minLength : 0; }
        }

        public int MaxLength
        {
            get { return maxLength > 0 ? maxLength : 0; }
            set { this.maxLength = value; }
        }

        public abstract FieldError ValidateValue(StringBuilder data);
    }
    #endregion

    #region X12_NDataType
    /*
     * This represents an Nn data type which is an integer with optional
     * min and max length restrictions. The following rules apply
     * 
     * 1st char can be digit or - sign. + sign is not allowed
     * Leading zeroes can be present only to make length=minLength, otherwise invalid
     * if n>0 . is inserted during parsing and removed during serialization, likewise
     *  absesnce of a dot or insuffcient length to put a dot are flagged as errors
     */
    [Serializable]
	public class X12_NDataType : X12BaseDataType
	{
		int mPrecision;

        

		private X12_NDataType(int precision, int min, int max) : base(X12DataTypeFactory.N_DataTypeName, min, max)
		{
			mPrecision = precision;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(20);
			sb.Append("{"+mName+" "+mPrecision+" "+minLength+" "+maxLength+"}");
			return sb.ToString();
		}


		public static X12_NDataType GetDataTypeWithPrecision(int position, int minL, int maxL)
			
		{
			return new X12_NDataType(position, minL, maxL);
		}

		public override FieldError ValidateValue(StringBuilder data)

		{
			FieldError error = null;
            bool bError = ValidateNDataType(data, out error);
			return error;
		}

       /*
        * This method does validation of a numeric data type after precision point has been
        * removed during serialization and raw data during parsing. The following rules need
        * to be obeyed
        *
        * 1) 0th char is - or digit, + is not allowed
        * 2) All other chars are digit
        * 3) If length > minLength, then no leading zeroes are allowed
        * 4) Sign is not part of length calculation
        *
        * This method doesn't apply precision point nor does it check for it
        */
        private bool ValidateNDataType(StringBuilder data, out FieldError error)
        {
            error = null;
            int dataLen = data.Length;
            if (dataLen == 0) //too short error
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), string.Empty);
                return true;
            }

            char c = data[0];
            bool isNegSign = false;
            if (c == '+')
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode), data.ToString());
                return true;
            }

            else if (c == '-')
            {
                isNegSign = true;
            }

            for (int i = isNegSign ? 1 : 0; i < dataLen; i++)
                // "." is valid character
                if (!(DataTypeHelper.IsDigit(data[i]) || data[i] == '.'))
                {
                    error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                        X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode), data.ToString());
                    return true;
                }


            if (isNegSign)
            {
                dataLen--; //count of digits
                if (dataLen > 0) c = data[1];
                else
                {
                    error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                        X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), data.ToString());
                    return true;
                }
            }

            

            if (/*(mPrecision > 0 && dataLen < mPrecision) || */(minLength >= 0 && dataLen < minLength))
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), data.ToString());
                return true;
            }

            if (maxLength >= 0 && dataLen > maxLength)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooLongCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooLongCode), data.ToString());
                return true;
            }

            return false;
        }

        
    }
    #endregion

    #region X12_AnDataType
    [Serializable]
    public class X12_AnDataType : X12BaseDataType
    {
		public X12_AnDataType(string name, int minL, int maxL) : base(name, minL, maxL)
		{
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(20);
			sb.Append("{"+mName+" "+minLength+" "+maxLength+"}");
			return sb.ToString();
		}


       /*
        * All characters are allowed, however length restriction needs to be obeyed
        * Leading spaces are allowed only for min length restriction
        */
		public override FieldError ValidateValue(StringBuilder data)
		{
			FieldError error = null;
            string str = data.ToString();
            bool isLeadingSpace = data.Length > 0 && data[0] == ' ';
            bool isTrailingSpace = data.Length > 0 && data[data.Length - 1] == ' ' && data.ToString().Trim().Length != 0;

			if (minLength >= 0 && data.Length < minLength)
			{
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), str);
			}

			else if (maxLength >= 0 && data.Length > maxLength)
			{
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooLongCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooLongCode), str);
			}

            else if ( (isTrailingSpace || isLeadingSpace) && minLength >= 0 && data.Length > minLength
                && false)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                    //X12ErrorCode.X12DataElementLeadingOrTrailingSpaceFoundDescription, 
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode),
                    data.ToString());
            }

            else //no min max error
            {
                //todo Get this from the environment context
                CharSet charSet = CharSetFactory.GetCharSetClass(CharSetFactory.ExtendedCharSetIndex);

                for (int i = 0; i < data.Length; i++)
                {
                    if (!charSet.IsMember(data[i]))
                    {
                        error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                            X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode),
                                data.ToString());
                        break;
                    }
                }
            }

            return error;
		}

       
    }
    #endregion

    #region X12_TmDataType
    /*
     * This represents a Time data type. It can take the format
     * 
     * HHMM or HHMMSS[dd]
     * 
     * The above formats are differentiated by minLenght and maxLength
     * which can only be 4 & 6 respectively.
     * 
     * If one of them is specified but not the other, specified is taken as min and max
     * Otherwise both unspecified, both are defaulted to 4
     * 
     * Use of .Net data type for validation is not required as it is a heavy way of doing it
     */
    [Serializable]
    public class X12_TmDataType : X12BaseDataType
    {


        public X12_TmDataType(string name, int minL, int maxL) : base(name, minL, maxL)
        {
            //both unspecified
            if (minLength < 0) 
            {
                minLength = 4;
            }

            if (maxLength < 0)
            {
                maxLength = 8;
            }

            
            //allowed minLengths are 4, 6, 7, 8
            if (minLength < 4 || minLength == 5 || minLength > 8)
            {
                minLength = 4;
            }

            //allowed maxLengths are 4, 6, 7, 8
            if (maxLength < 4 || maxLength == 5 || maxLength > 8)
            {
                maxLength = 8;
            }

        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(20);
            sb.Append("{" + mName + " " + minLength + " " + maxLength + "}");
            return sb.ToString();
        }


        public override FieldError ValidateValue(
            StringBuilder data)
        {
            FieldError error = null;
            string str = data.ToString();
            if (data.Length < minLength || data.Length == 5)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidTimeCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidTimeCode), str);
                return error;
            }

            if (data.Length > maxLength)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidTimeCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidTimeCode), str);
                return error;
            }

            //run thru the data to ensure all are digits
            for (int i = 0; i < data.Length; i++)
                if (!DataTypeHelper.IsDigit(data[i]))
                {
                    error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidTimeCode,
                        X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidTimeCode), data.ToString());
                    return error;
                }

            //data is between min and max length, thus it is atleast 4 chars
            //and all are digits
            int hour = 10 * (data[0] - '0') + (data[1] - '0');
            int min = 10 * (data[2] - '0') + (data[3] - '0');
            int sec = data.Length == 4 ? 0 : 10 * (data[4] - '0') + (data[5] - '0');

            if (hour < 0 || hour > 23 || min < 0 || min > 59 || sec < 0 || sec > 59)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidTimeCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidTimeCode), str);
            }


            return error;
        }

        
    }
    #endregion

    #region X12_DtDataType
    /*
     * This represents a Date data type. It can take the format
     * 
     * YYMMDD or CCYYMMDD
     * 
     * The above formats are differentiated by minLenght and maxLength
     * which can only be 6 & 8 respectively.
     * 
     * If one of them is specified but not the other, specified is taken as min and max
     * Otherwise both unspecified, both are defaulted to 6
     * 
     * Use of .Net data type for validation is not required as it is a heavy way of doing it
     */
    [Serializable]
    public class X12_DtDataType : X12BaseDataType
	{
        public X12_DtDataType(string name, int minL, int maxL) : base(name, minL, maxL)
		{
            //both unspecified
            if (minLength < 0 && maxLength < 0)
            {
                minLength = 6;
                maxLength = 8;
            }

            else
            {
                if (minLength > 0 && minLength != 8)
                {
                    minLength = 6;
                }

                if (maxLength > 0 && maxLength != 6)
                {
                    maxLength = 8;
                }
            }

		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(20);
			sb.Append("{"+mName+" "+minLength+" "+maxLength+"}");
			return sb.ToString();
		}


		public override FieldError ValidateValue(StringBuilder data)
		{
            FieldError error = null;
			string str = data.ToString();

            // First try the .NET function to check if date is valid
            // if not we will follow other way (check yyyymmdd)

            DateTime dt;
            if (DateTime.TryParse(str, out dt))
                return error;

			if (data.Length < minLength || data.Length == 7)
			{
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidDateCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidDateCode), str);
                return error;
			}

			if (data.Length > maxLength)
			{
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidDateCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidDateCode), str);
                return error;
			}

            //run thru the data to ensure all are digits
            for (int i = 0; i < data.Length; i++)
                if (!DataTypeHelper.IsDigit(data[i]))
                {
                    error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidDateCode,
                        X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidDateCode), data.ToString());
                    return error;
                }

            //data is between min and max length, thus it is atleast 6 chars
            //and all are digits
            int offset = data.Length == 6 ? 2 : 4;
            int year = data.Length == 6 ? 10 * (data[0] - '0') + (data[1] - '0')
                    : 1000 * (data[0] - '0') + 100 * (data[1] - '0') + 10 * (data[2] - '0') + (data[3] - '0');

            int month = 10 * (data[offset] - '0') + (data[offset+1] - '0');
            int day = 10 * (data[offset+2] - '0') + (data[offset+3] - '0');

            if (year == 0)
            {
                year = 2000;
            }

            try
            {
                DateTime dateTime = new DateTime(year, month, day);
            }
            catch(ArgumentOutOfRangeException)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidDateCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidDateCode), str);
            }

            return error;
		}

       

        /*
         * Returns true for a valid Feb 29 date
         * Otherwise false
         * 
         * Leap Year Math
         * 
         * Leap Years occur on a year that is exactly divisible by four 
         * Leap Years do NOT occur on years ending in 00 UNLESS that year is exactly divisible by 400 
         * 
         * return true for leap year scenario
         */
        private static bool IsFeb29LeapYear(int month, int day, int year)
        {
            if (month == 2 && day == 29)
            {
                if (year < 100) return year % 4 == 0;

                //if year is a multiple of 100
                if (year % 100 == 0)
                {
                    if (year % 400 == 0) return true;
                    return false;
                }

                return year % 4 == 0;
            }

            return false;
        }
    }
    #endregion

    #region X12_IdDataType
    [Serializable]
    public enum ContingencyType
    {
        None,
        Enumeration,
        CrossSegment,
    };

    [Serializable]
    public class Contingency
    {
        public ContingencyType Type { get; set; }
        public List<string> ContingencyValues { get; private set; }

        public void AddContingencyValue(string contingencyValue)
        {
            if (ContingencyValues == null)
                ContingencyValues = new List<string>();
            ContingencyValues.Add(contingencyValue);
        }
    };

    [Serializable]
    public class X12_IdDataType : X12BaseDataType
	{
        private Dictionary<string, string> mAllowedValues;
        private List<string> OptionalValues { get; set; }
        private Dictionary<string, Contingency> Contingencies { get; set; }

        public X12_IdDataType(string name, Dictionary<string, string> allowedValues)
            : base(name)
		{
            mAllowedValues = allowedValues;
            Contingencies = new Dictionary<string, Contingency>();
            OptionalValues = null;
		}

        public X12_IdDataType(string name, List<string> optionalValues, Dictionary<string, string> allowedValues, Dictionary<string, Contingency> contingencies)
            : base(name)
        {
            mAllowedValues = allowedValues;
            Contingencies = contingencies;
            OptionalValues = optionalValues;
        }

        public Dictionary<string, string> AllowedValues
        {
            get { return mAllowedValues; }
        }

        public Contingency GetContingencies(string enumCode)
        {
            Contingency contingency = null;
            if (Contingencies.TryGetValue(enumCode, out contingency) == true)
            {
                return contingency;
            }

            return null;
        }

        public bool IsOptionalValue(string enumCode)
        {
            bool isOptionalValue = false;
            if (OptionalValues != null && OptionalValues.Count != 0)
                isOptionalValue = OptionalValues.Contains(enumCode);

            return isOptionalValue;
        }

		public override FieldError ValidateValue(StringBuilder data)
		{
			FieldError error = null;
			string str = data.ToString();

			if (minLength >= 0 && data.Length < minLength)
			{
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), str);
			}

			if (maxLength >= 0 && data.Length > maxLength)
			{
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooLongCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooLongCode), str);
			}

			if (mAllowedValues != null && mAllowedValues.Count > 0 && !mAllowedValues.ContainsKey(str))
			{
                //error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCodeValueCode,
                  //  X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCodeValueCode), str);

                StringBuilder allowedList = new StringBuilder();
                Dictionary<string, string>.KeyCollection keyColl = mAllowedValues.Keys;
                allowedList.Append('{');
                foreach (string key in keyColl)
                {
                    if (allowedList.Length > 1)
                    {
                        allowedList.Append(", ");
                    }

                    allowedList.Append(key);
                }
                allowedList.Append('}');

                string errorDescription = "Value {0} not found in list of possible values {1}";
                errorDescription = string.Format(errorDescription, str, allowedList.ToString());

                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCodeValueCode,
                    errorDescription, str);
			}
            else
            {
                CharSet charSet = CharSetFactory.GetCharSetClass(CharSetFactory.ExtendedCharSetIndex);

                for (int i = 0; i < data.Length; i++)
                {
                    if (!charSet.IsMember(data[i]))
                    {
                        error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                            X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode), data.ToString());
                        break;
                    }
                }
            }

			return error;
		}

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(20);
            sb.Append("{" + mName + "}");
            return sb.ToString();
        }
    }
    #endregion

    #region X12_RDataType
    /*
     * This represents an R data type which is a real number with optional
     * min and max length restrictions. The following rules apply
     * 
     * 1st char can be digit or - sign. + sign is not allowed
     * Leading zeroes can be present only to make length=minLength, otherwise invalid
     * Trailing zeroes donot matter
     * Only 1 . is valid
     */
    [Serializable]
	public class X12_RDataType : X12BaseDataType
	{


        public X12_RDataType(string name, int minL, int maxL) : base(X12DataTypeFactory.R_DataTypeName, minL, maxL)
        {
        }


		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(20);
			sb.Append("{"+mName+"}");
			return sb.ToString();
		}

		public override FieldError ValidateValue(StringBuilder data)
		{
			FieldError error = null;
            ValidateRDataType(data, minLength, maxLength, '.', out error);
            return error;
		}

        private static void AppendInvalidCharCode(StringBuilder data)
        {
                DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode), data.ToString());
        }

       /*
        * This method does validation of a numeric data type after precision point has been
        * removed during serialization and raw data during parsing. The following rules need
        * to be obeyed
        *
        * 1) 0th char is - or digit, + is not allowed
        * 2) All other chars are digit
        * 3) If length > minLength, then no leading zeroes are allowed
        * 4) Sign is not part of length calculation
        *
        * This method doesn't apply precision point nor does it check for it
        */
        public static bool ValidateRDataType(StringBuilder data, int minL, int maxL, int decimalSeparator, out FieldError error)
        {
            error = null;
            int dataLen = data.Length;
            if (dataLen == 0) //too short error
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), string.Empty);
                return true;
            }

            char c = data[0];
            bool isNegSign = false;
            bool isError = false;
            if (c == '+')
            {
                isError = true;
            }

            else if (c == '-')
            {
                isNegSign = true;
                if (data.Length == 1) isError = true; //data is -, single negative sign which is invalid
            }

            if (isError)
            {
                AppendInvalidCharCode(data);
                return true;
            }

            //This loop would ensure all are digits or digits + one point
            //otherwise loop would abort and return true
            bool bPointFound = false;
            int decimalIndex = -1;
            for (int i = isNegSign ? 1 : 0; i < dataLen; i++)
            {
                if (DataTypeHelper.IsDigit(data[i])) //digit found, no-op
                {
                }

                else if (!bPointFound && data[i] == decimalSeparator)
                {
                    bPointFound = true;
                    decimalIndex = i;
                    //Edifact data should have atleast one digit after decimal point
                    if (!(i+1 < dataLen && DataTypeHelper.IsDigit(data[i+1])) )
                    {
                        
                        error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeInvalidCharacterInDataElementCode,
                            X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeInvalidCharacterInDataElementCode), data.ToString());
                    }
                }

                else //error state
                {
                    AppendInvalidCharCode(data);
                    return true;
                }
            }


            if (isNegSign)
            {
                dataLen--; //count of digits
                if (dataLen > 0) c = data[1];
                else
                {
                    AppendInvalidCharCode(data);
                    return true;
                }
            }

            

            //check for leading zeroes
            //Leading zero is the 1st char being zero, otherwise not
            //leading zero is allowed only when data has minimum length
            //skip the check if minL is not specified, #3454
            if (c == '0' && minL > 0 && false)
            {
                if (decimalIndex < 0) //no decimal found
                {
                    if ((dataLen > minL && minL > 0) || minL < 0)
                    {
                        AppendInvalidCharCode(data);
                        return true;
                    }
                }

                else //decimal was found, so 0.1, -0.1 are valid but 00.1 or -00.1 are invalid
                {
                    if ((dataLen > minL && minL > 0) || minL < 0)
                    {
                        int offset = isNegSign ? 2 : 1;
                        if (data.Length > offset && data[offset] == decimalSeparator)
                        {
                        }

                        else
                        {
                            AppendInvalidCharCode(data);
                            return true;
                        }
                    }
                }
            }

            if (decimalIndex >= 0)
            {
                dataLen--;
                if (data[data.Length - 1] == '0' && false
                    && ((dataLen > minL && minL > 0) || minL < 0))
                {
                    AppendInvalidCharCode(data);
                    return true;
                }
            }

            if (minL > 0 && dataLen < minL)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooShortCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooShortCode), data.ToString());

                return true;
            }

            if (maxL > 0 && dataLen > maxL)
            {
                error = DataTypeHelper.GenerateFieldError(X12ErrorCode.DeDataElementTooLongCode,
                    X12ErrorCode.GetDataElementErrorDescription(X12ErrorCode.DeDataElementTooLongCode), data.ToString());
                return true;
            }

            return false;
        }
    }
    #endregion
}