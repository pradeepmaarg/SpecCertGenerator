using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Maarg.AllAboard
{
    class StreamHelper
    {
        const string HeaderFormat = "{0}:{1}";
        const char CarriageReturn = (char)13;
        const char NewLine = (char)10;

        public static Stream SerializeHeadersToStream(IList<KeyValuePair<string, string>> nameValuePairList, bool appendCrLf)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in nameValuePairList)
            {
                sb.Append(string.Format(HeaderFormat, pair.Key, pair.Value));
                AppendCarriageReturnAndNewLine(sb);
            }

            //Write the final CR LF pair
            if (appendCrLf)
            {
                AppendCarriageReturnAndNewLine(sb);
            }

            string str = sb.ToString();
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            MemoryStream header = new MemoryStream(buffer);
            header.Position = 0;

            return header;
        }

        public static MemoryStream DeserializeStreamToHeadersAndBody(Stream inputStream, out IList<KeyValuePair<string, string>> nameValuePairList)
        {
            int startIndex = (int)inputStream.Position;
            MemoryStream masterStream = inputStream as MemoryStream;
            if (masterStream == null)
            {
                masterStream = new MemoryStream();
                inputStream.CopyTo(masterStream);
            }

            byte[] masterBuffer = masterStream.GetBuffer();
            int bodyStartPosition = -1;

            for (int index = startIndex; index < masterBuffer.Length-4 ; index++)
            {
                if (masterBuffer[index] == (byte)CarriageReturn &&
                    masterBuffer[index+1] == (byte)NewLine &&
                    masterBuffer[index+2] == (byte)CarriageReturn &&
                    masterBuffer[index+3] == (byte)NewLine)
                {
                        bodyStartPosition = index + 4;
                        break;
                }
            }


            // entire header as a string
            string headerStrWithCrLf = Encoding.UTF8.GetString(masterBuffer, startIndex, bodyStartPosition - 4 - startIndex);
            char[] delim = new char[1];
            delim[0] = NewLine;

            // got each line separately
            string[] headerStrLineTokens = headerStrWithCrLf.Split(delim);
            nameValuePairList = new List<KeyValuePair<string, string>>();
            delim[0] = ':';
            string key, value;
            for (int i = 0; i < headerStrLineTokens.Length; i++)
            {
                int index = headerStrLineTokens[i].IndexOf(delim[0]);
                key = headerStrLineTokens[i].Substring(0, index);

                //trim the Lf at end
                value = headerStrLineTokens[i].Substring(index + 1);
                if (!string.IsNullOrEmpty(value) && value[value.Length-1] == CarriageReturn)
                {
                    value = value.Substring(0, value.Length - 1);
                }
                
                nameValuePairList.Add(new KeyValuePair<string, string>(key, value));
            }


            masterStream.Position = bodyStartPosition;
            return masterStream;
        }

        private static void AppendCarriageReturnAndNewLine(StringBuilder sb)
        {
            sb.Append((char)13);
            sb.Append((char)10);
        }

        
    }
}
