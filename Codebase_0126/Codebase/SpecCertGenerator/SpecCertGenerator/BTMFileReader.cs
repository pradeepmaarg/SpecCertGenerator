using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SpecCertGenerator
{
    class BTMFileReader
    {
        public static BTMFileInfo ReadBTMFile(string fileName)
        {
            if (File.Exists(fileName) == false)
            {
                throw new SpecCertGeneratorException(string.Format("{0} file does not exist", fileName));
            }

            BTMFileInfo btmFileInfo = new BTMFileInfo();
            XElement btmFileRoot = XElement.Load(fileName);

            btmFileInfo.SourceTree = GetLocationValue(btmFileRoot, "SrcTree");
            btmFileInfo.TargetTree = GetLocationValue(btmFileRoot, "TrgTree"); ;

            return btmFileInfo;
        }

        private static string GetLocationValue(XElement rootElement, string elementName)
        {
            string locationValue = null;

            XElement element = rootElement.Descendants(elementName).FirstOrDefault();

            if (element != null)
            {
                XElement referenceElement = element.Descendants("Reference").FirstOrDefault();
                if (referenceElement != null)
                {
                    XAttribute locationAttribute = referenceElement.Attribute("Location");
                    if (locationAttribute != null)
                        locationValue = locationAttribute.Value;
                }
            }

            return locationValue;
        }
    }
}
