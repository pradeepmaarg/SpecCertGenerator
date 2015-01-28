using System;

namespace SpecCertGenerator
{
    public class ConsoleExtensions
    {
        #region Write message functions
        public static void WriteError(string format, params object[] args)
        {
            Write(ConsoleColor.Red, format, args);
        }

        public static void WriteWarning(string format, params object[] args)
        {
            Write(ConsoleColor.Yellow, format, args);
        }

        public static void WriteInfo(string format, params object[] args)
        {
            Write(ConsoleColor.White, format, args);
        }

        private static void Write(ConsoleColor msgColor, string format, params object[] args)
        {
            ConsoleColor existingColor = Console.ForegroundColor;
            Console.ForegroundColor = msgColor;
            try
            {
                Console.WriteLine(format, args);
            }
            finally
            {
                Console.ForegroundColor = existingColor;
            }
        }
        #endregion
    }
}
