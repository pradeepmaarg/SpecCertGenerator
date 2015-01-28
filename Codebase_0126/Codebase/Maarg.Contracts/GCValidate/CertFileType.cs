using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts.GCValidate
{
    public enum SpecCertFileType
    {
        None = 0,
        X12 = 1,
        FlatFile = 2,
        Xml = 3,
    }

    public enum CertFileType
    {
        Invalid,
        SpecCert,
        BizRuleCert,
    }
}
