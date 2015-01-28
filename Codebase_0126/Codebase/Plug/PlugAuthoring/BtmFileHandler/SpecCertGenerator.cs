using Maarg.Fatpipe.Plug.DataModel;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Maarg.Fatpipe.Plug.Authoring.BtmFileHandler
{
    public class SpecCertGenerationResult
    {
        public SpecCertGenerationResult()
        {
            PathsUsed = new List<string>();
            Errors = new List<string>();
        }

        public string SpecCertPath { get; set; }
        public List<string> Errors { get; set; }
        public List<string> PathsUsed { get; set; }
        public bool SpecCertGenerated { get; set; }
    }

    public class SpecCertGenerator
    {
        public SpecCertGenerationResult GenerateSpecCert(MapDetail mapDetail, string specCertType)
        {
            SpecCertGenerationResult result;

            switch (specCertType)
            {
                case "edi":
                    result = new EdiSpecCertGenerator().GenerateSpecCert(mapDetail);
                    break;

                case "xml":
                    result = new XmlSpecCertGenerator().GenerateSpecCert(mapDetail);
                    break;

                case "flatfile":
                    result = new FlatFileSpecCertGenerator().GenerateSpecCert(mapDetail);
                    break;

                default:
                    throw new NotSupportedException(string.Format("Spec cert type {0} is not supported", specCertType));
                    break;
            }
            
            return result;
        }
    }
}
