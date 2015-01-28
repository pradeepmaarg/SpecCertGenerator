using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Fatpipe.Plug.DataModel
{
    public class DocumentPlugValidationInfo
    {
        public string SpecCertName { get; set; }
        public string FileName { get; set; }
        public string FileContents { get; set; }
        public IDocumentPlug DocumentPlug { get; set; }
        public IFatpipeDocument FatpipeDocument { get; set; }
    }
}
