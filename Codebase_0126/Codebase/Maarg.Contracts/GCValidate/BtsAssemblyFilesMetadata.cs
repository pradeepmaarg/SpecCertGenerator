using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace Maarg.Contracts.GCValidate
{
    public class BtsAssemblyFilesMetadata : TableServiceEntity
    {
        public string FileName { get; set; }

        //public string UserName { get; set; }
        //public DateTime WhenUpdated { get; set; }
    }
}
