using System;
using System.Text;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;


namespace Maarg.Fatpipe.Plug.DataModel
{
    public class CharSetFactory
    {
        public const int BasicCharSetIndex = 0;
        public const int ExtendedCharSetIndex = 1;
        public const int Utf8CharSetIndex = 2;
        public const char GenericInvalidChar = (char)0xFFFD;

        static CharSet[] sCharSetList = { 
            BasicCharSet.Instance, ExtendedCharSet.Instance, OtherCharSet.Instance
        };

        public static int GetClassIndex(string charSetName)
        {
            int index = -1;

            if (string.Compare(charSetName, "BasicCharacterSet", true) == 0)
            {
                index = BasicCharSetIndex;
            }

            else if (string.Compare(charSetName, "ExtendedCharacterSet", true) == 0)
            {
                index = ExtendedCharSetIndex;
            }

            else if (string.Compare(charSetName, "Utf8CharacterSet", true) == 0)
            {
                index = Utf8CharSetIndex;
            }

            return index;
        }

        /*
         * Users would have called GetClassIndex previously.
         * If not, then unsupported
         * Since this method would be called for all char related data types and 
         * there would be numerous in the schema, so idea is to have the code path 
         * as short as possible. No switch statement, just a quick
         * index based lookup
         */
        public static CharSet GetCharSetClass(int index)
        {
            return sCharSetList[index];
        }

        public static Encoding GetEncodingCharSet(int charsetIndex)
        {
            Encoding result = Encoding.GetEncoding(65001, new EncoderReplacementFallback(), //UTF8
                        new DecoderReplacementFallback(new string(GenericInvalidChar, 1)));

            switch (charsetIndex)
            {
                case BasicCharSetIndex:
                    result = Encoding.GetEncoding(20127, new EncoderReplacementFallback(), //Ascii
                        new DecoderReplacementFallback(new string(GenericInvalidChar, 1)));
                    break;

                case ExtendedCharSetIndex:
                    result = Encoding.GetEncoding(28591, new EncoderReplacementFallback(), //LATIN1
                        new DecoderReplacementFallback(new string(GenericInvalidChar, 1)));
                    break;
            }

            return result;
        }
    }

    /*
     * This is the base class for Edifact character sets
     * In X12, such a thing is not needed because any Unicode character is
     * considered to be valid
     */
    public abstract class CharSet
    {
        protected string mName;
        public abstract bool IsMember(int ch); //crucial method

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} Char Set", mName);
        }
    }


    /*
     * This class encapsulates the Basic character set used for X12. The included characters are a subset
     * of Ascii and include the following
     * 
     * A-Z
     * 0-9
     * Space (32)
     * Full stop (46)
     * Comma (44)
     * Minus (45)
     * Open bracket (40)
     * Close bracket (41)
     * Front slash (47)
     * Equals (61)
     * Apostrophe (39)
     * Plus (43)
     * Question (63)
     * Colon (58)
     * SemiColon (59)
     * DoubleQuote (34)
     * Ampersand (38)
     * Asterisk (64)
     * 
     * Thus, the integer range is 32-34, 38-41, 43-59, 61, 63-90
     */
    public class BasicCharSet : CharSet
    {

        static byte[] CharBitMap = { 
                                // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5     - Each row of width 16
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 0-15 
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 16-31
                                   1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 32-47 
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, // - chars 48-63 
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 64-79
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, // - chars 80-95 
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 96-111 
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 112-127
                            };
        static BasicCharSet mInstance = new BasicCharSet();

        private BasicCharSet()
        {
            mName = "BasicCharacterSet";
        }

        public static CharSet Instance
        {
            get { return mInstance; }
        }

        public override bool IsMember(int ch)
        {
            if (ch < 0 || ch >= CharBitMap.Length) return false;

            return CharBitMap[ch] == 1;
        }
    }

    /*
     * This class encapsulates the Basic character set used for X12. The included characters are a subset
     * of ISO8859-1 and include the following in addition to Basic CharSet
     * 
     * A-Z
     * 0-9
     * Space (32)
     * Full stop (46)
     * Comma (44)
     * Minus (45)
     * Open bracket (40)
     * Close bracket (41)
     * Front slash (47)
     * Equals (61)
     * Apostrophe (39)
     * Plus (43)
     * Question (63)
     * Colon (58)
     * SemiColon (59)
     * DoubleQuote (34)
     * Ampersand (38)
     * Asterisk (64)
     * Percentage (%) 37
     * @ 64
     * [ 91
     * ] 93
     * _ 95
     * { 123
     * } 125
     * \ 92
     * | 124
     * < 60
     * > 62
     * ~ 126
     * # 35
     * $ 36
     * 
     * Select language chars
     * 
     *  “À” 192
     *  ”Á” 193
     *  ”Â” 194
     *  ”Ä” 196
     *  ”à” 224
     *  ”á” 225
     *  ”â” 226
     *  ”ä” 228
     *  “È” 200
     *  ”É” 201
     *  ”Ê” 202
     *  ”è” 232
     *  ”é” 233
     *  ”ê” 234
     *  ”ë” 235
     *  ”Ì” 204
     *  ”Í” 205
     *  ”Î” 206
     *  ”ì” 236
     *  ”í” 237
     *  ”î” 238
     *  ”ï” 239
     *  “Ò” 210
     *  ”Ó” 211
     *  ”Ô” 212
     *  ”Ö” 214
     *  ”ò” 242
     *  ”ó” 243
     *  ”ô” 244
     *  ”ö” 246
     *  ”Ù” 217
     *  ”Ú” 218
     *  ”Û” 219
     *  ”Ü” 220
     *  “ù” 249
     *  ”ú” 250
     *  ”û” 251
     *  ”ü” 252
     *  ”Ç” 199
     *  ”ç” 231
     *  ”Ñ” 209
     *  ”ñ” 241
     *  ”¿” 191
     *  ”¡” 161
     *
     * 
     * Thus, the integer range is 32-41, 43-93, 95, 97-125
     *                            191-5, 196, 199-2, 204-6, 210-2, 214, 217-0, 224-6, 228, 232-9
     *                            241-4, 246, 249-2
     */
    public class ExtendedCharSet : CharSet
    {

        static byte[] CharBitMap = { 
                                // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5     - Each row of width 16
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 0-15 
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 16-31
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 32-47 
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 48-63 
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 64-79
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, // - chars 80-95 
                                   0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 96-111 
                                   1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // - chars 112-127
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 128-143 
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 144-159
                                   0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // - chars 160-175 
                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, // - chars 176-191 
                                   1, 1, 1, 0, 1, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1, 0, // - chars 192-207
                                   0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 0, // - chars 208-223 
                                   1, 1, 1, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, // - chars 224-239 
                                   0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 0, // - chars 240-255
                            };
        static ExtendedCharSet mInstance = new ExtendedCharSet();

        private ExtendedCharSet()
        {
            mName = "ExtendedCharacterSet";
        }

        public static CharSet Instance
        {
            get { return mInstance; }
        }

        public override bool IsMember(int ch)
        {
            if (ch < 0 || ch >= CharBitMap.Length) return false;

            return CharBitMap[ch] == 1;
        }
    }


    /*
     * This class encapsulates the entire universe of characters.
     * In this char set, any character is valid
     */
    public class OtherCharSet : CharSet
    {
        static OtherCharSet mInstance = new OtherCharSet();

        private OtherCharSet()
        {
            mName = "Other";
        }

        public static CharSet Instance
        {
            get { return mInstance; }
        }

        public override bool IsMember(int ch)
        {
            return true;
        }
    }
}
