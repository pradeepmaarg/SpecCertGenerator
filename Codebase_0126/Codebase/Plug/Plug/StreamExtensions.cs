using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public static class StreamExtensions
    {
        public static string ReadLine(this TextReader reader, char delimeter, bool trimResult)
        {
            StringBuilder lineBuilder = new StringBuilder();

            while (reader.Peek() >= 0)
            {
                char c = (char)reader.Read();

                if (c == delimeter)
                {
                    break;
                }

                lineBuilder.Append(c);
            }

            string line = lineBuilder.ToString();

            if (trimResult)
                line = line.Trim();

            return line;
        }
    }
}
