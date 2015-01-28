using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Maarg.Fatpipe.Plug.DataModel;

namespace Maarg.Fatpipe.EDIPlug
{
    public class InterchangeTokenizer
    {
        const int IsaMaxLength = 108;
        StreamReader reader;

        public string ISARecord { get; private set; }
        public int ISARecordLen { get; private set; }
        public string Error { get; private set; }

        public InterchangeTokenizer(Stream inputData, Encoding encoding)
        {
            this.reader = new StreamReader(inputData, encoding);
        }

        public InterchangeTokenizer(Stream inputData)
            : this(inputData, Encoding.UTF8)
        {
        }

        /// <summary>
        /// ISA contains 16 fields, all of which have fixed width.
        /// 
        ///  1) Field seperator is always at offset 3, i.e next char after ISA
        ///  2) ComponentSeperator - always at offset 104
        ///  3) SegmentDelimiter - 1st char at offset 105, suffix 1 and 2 are next but they are both optional
        /// </summary>
        /// <param name="delimiters">the list of delimiters</param>
        /// <returns>true if this is an X12 interchange. Otherwise false</returns>
        public bool IsX12Interchange(out Delimiters delimiters)
        {
            Error = string.Empty;

            delimiters = null;
            //always read 108 characters, at min isa length should be 106
            char[] isaRecord = new char[IsaMaxLength];
            bool bSuccess = false;
            int len = this.reader.Read(isaRecord, 0, IsaMaxLength);
            ISARecordLen = len;

            int fs = -1, cs = -1, sd = -1, suffix1 = -1, suffix2 = -1;
            if (len < 106) //error state
            {
                Error = string.Format("ISA record length({0}) is < 106", len);
                return false;
            }

            else //length is atleast 106
            {
                bool startsWithIsa = isaRecord[0] == 'I' && isaRecord[1] == 'S' && isaRecord[2] == 'A';
                if (!startsWithIsa)
                {
                    Error = string.Format("ISA record does not start with 'ISA': {0}", isaRecord);
                    return false;
                }

                fs = isaRecord[3];
                cs = isaRecord[104];
                sd = isaRecord[105];

                if (sd != Delimiters.CarriageReturn && sd != Delimiters.LineFeed)
                {
                    if (len > IsaMaxLength - 2) //atleast suffix1 may exist
                    {
                        suffix1 = isaRecord[IsaMaxLength - 2];
                        if (suffix1 == Delimiters.CarriageReturn || suffix1 == Delimiters.LineFeed)
                        {
                            if (len > IsaMaxLength - 1)
                            {
                                suffix2 = isaRecord[IsaMaxLength - 1];
                                bSuccess = true;
                                if (suffix2 == Delimiters.LineFeed)
                                {
                                    ISARecordLen = IsaMaxLength;
                                }

                                else
                                {
                                    ISARecordLen = IsaMaxLength - 1;
                                    suffix2 = -1;
                                }
                            }
                        }

                        else if (suffix1 != Delimiters.LineFeed) //LF
                        {
                            //segment only has sd, no suffix1 and suffix2
                            ISARecordLen = IsaMaxLength - 2;
                            bSuccess = true;
                            suffix1 = suffix2 = -1;
                        }
                    }

                    else
                    {
                        ISARecordLen = IsaMaxLength - 2;
                        bSuccess = true;
                    }
                }

                else //sd is CR or LF
                {
                    bSuccess = true;

                    if (len > IsaMaxLength - 2 && sd == Delimiters.CarriageReturn) //atleast suffix1 may exist
                    {
                        suffix1 = isaRecord[IsaMaxLength - 2];

                        if (suffix1 == Delimiters.LineFeed)
                        {
                            ISARecordLen = IsaMaxLength - 1; //13 10
                        }

                        else
                        {
                            suffix1 = -1;
                            ISARecordLen = IsaMaxLength - 2; //13
                        }
                    }

                    else
                    {
                        ISARecordLen = IsaMaxLength - 2; //13 or 10
                    }
                }
            }

            //check whether delimiters are unique or not
            if (fs == cs || fs == sd || cs == sd)
            {
                Error = string.Format("Delimiters are not unique. Field: '{0}', Component: '{1}', Segment: '{2}'", (char)fs, (char)cs, (char)sd);

                bSuccess = false;
            }

            // check if any delimiter is digit
            if( (fs >= 48 && fs <= 57) || (cs >= 48 && cs <= 57) || (sd >= 48 && sd <= 57) )
            {
                Error = string.Format("Delimiters should not be numeric (48-57). Field: '{0}', Component: '{1}', Segment: '{2}'", (char)fs, (char)cs, (char)sd);

                bSuccess = false;
            }

            delimiters = bSuccess ? new Delimiters(fs, cs, sd, suffix1, suffix2) : null;

            ISARecord = bSuccess ? new string(isaRecord).Substring(0, ISARecordLen) : string.Empty;

            if (bSuccess == false && string.IsNullOrWhiteSpace(Error))
            {
                Error = "Error identifying delimiters, please check if ISA segment has segment delimiter as well as new line at the end of the segment.";
            }

            return bSuccess;
        }
    }
}
