using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class FormulaFactory
    {
        public static string StringLeft(string inputString, int index)
        {
            return inputString.Substring(0, index);
        }

        public static string StringRight(string inputString, int index)
        {
            return inputString.Substring(inputString.Length-index, inputString.Length);
        }

        public static string StringConcatenate(string input1, string input2)
        {
            return input1+input2;
        }

        public static string UpperCase(string input)
        {
            return input.ToUpper();
        }

        public static string Date()
        {
            return DateTime.Today.ToString();
        }

        public static string Time()
        {
            return DateTime.Now.TimeOfDay.ToString();
        }

        public static bool Equality(string[] parameters)
        {
            return parameters[0].Equals(parameters[1]);
        }

        public static bool LogicalOR(string[] parameters)
        {
            foreach (string cond in parameters)
            {
                bool result = bool.Parse(cond);
                if (result)
                    return result;
            }
            return false;
        }

        public static string ValueMapping(string[] parameters)
        {
            return bool.Parse(parameters[0]) ? parameters[1] : null;
        }

        public static bool GraeterThan(double input1, double input2)
        {
            return (input1>input2);
        }

        public static int Size(string input)
        {
            return input.Trim().Length;
        }

        public static int RecordCount(string inputDoc, string record)
        {
            return Regex.Matches(inputDoc, record).Count;
        }

        public static double Addition(double input1, double input2)
        {
            return (input1 + input2);
        }

        public static string Trim(string input)
        {
            return input.Trim();
        }
    }
}
