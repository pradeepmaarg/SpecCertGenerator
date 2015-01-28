using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace Maarg.Contracts.GCValidate
{
    public class TradingPartnerSpecCertMetadata : TableServiceEntity
    {
        public string TradingPartnerName { get; set; }
        public int DocumentType { get; set; }
        public string SchemaFileName { get; set; }
        public string Direction { get; set; }
        public string UserName { get; set; }
        public DateTime WhenUpdated { get; set; }

        // CertFileType is int here since Azure table storage does not support enum
        public int FileType { get; set; }

        public string Type { get; set; }

        public TradingPartnerSpecCertMetadata() { }

        /// <summary>Initialize TradingPartnerSpecCertMetadata from cert file name. It returns list of errors.</summary>
        /// <param name="certFileFullName"></param>
        /// <returns>Errors</returns>
        public List<string> Initialize(string certFileFullName)
        {
            return this.Initialize(certFileFullName, null, DateTime.MinValue);
        }

        /// <summary>Initialize TradingPartnerSpecCertMetadata from cert file name. It returns list of errors.</summary>
        /// <param name="certFileFullName"></param>
        /// <param name="userName"></param>
        /// <param name="updateTime"></param>
        /// <returns>Errors</returns>
        public List<string> Initialize(string certFileFullName, string userName, DateTime updateTime)
        {
            FileType = (int)SpecCertFileType.X12;
            List<string> errors = new List<string>();

            string fileExtension = Path.GetExtension(certFileFullName).ToLowerInvariant();
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
                errors.Add("Invalid file type");
            else
            {
                string certFileName = Path.GetFileNameWithoutExtension(certFileFullName);

                string[] fileNameParts = certFileName.Split(new string[] {" - "}, StringSplitOptions.RemoveEmptyEntries);

                if (fileNameParts == null || fileNameParts.Length < 4)
                {
                    errors.Add("Spec cert file name does not adhere to format '<Partner Name> - Spec Cert - <Version> - <Doc Type> [- <Direction>]  [- <text>]'");
                }
                else
                {
                    this.SchemaFileName = Path.GetFileName(certFileFullName);
                    this.TradingPartnerName = fileNameParts[0].Trim();

                    if (string.IsNullOrWhiteSpace(fileNameParts[2]) == false)
                    {
                        if (string.Equals(fileNameParts[2].Trim(), "flat file", StringComparison.InvariantCultureIgnoreCase))
                        {
                            FileType = (int)SpecCertFileType.FlatFile;
                        }
                        else if (string.Equals(fileNameParts[2].Trim(), "xml", StringComparison.InvariantCultureIgnoreCase))
                        {
                            FileType = (int)SpecCertFileType.Xml;
                        }
                    }

                    if (fileNameParts.Length > 4 && string.IsNullOrWhiteSpace(fileNameParts[4]) == false)
                        this.Direction = fileNameParts[4].Trim().ToUpper();
                    else
                        this.Direction = "SEND";

                    if(fileNameParts[1].Trim().ToLowerInvariant() != "spec cert")
                        errors.Add("Second part in spec cert file name should be 'spec cert'.");

                    if (this.TradingPartnerName.Length == 0)
                        errors.Add("Partner name cannont be empty in spec cert file name.");

                    if (fileNameParts[2].Trim().Length == 0)
                        errors.Add("Version number cannot be empty in spec cert file name.");

                    int docType;
                    if (int.TryParse(fileNameParts[3].Trim(), out docType) == false)
                        errors.Add(string.Format("Invalid document type ({0}) in spec cert file name.", fileNameParts[3]));
                    else
                        this.DocumentType = docType;

                    this.Type = ((SpecCertFileType)FileType).ToString() + "_" + fileNameParts[3].Trim();
                    this.UserName = userName;
                    this.WhenUpdated = updateTime;
                }
            }

            return errors;
        }

        public string GetCertFileDisplayName()
        {
            if (string.IsNullOrEmpty(Direction))
            {
                return string.Format("{0} - {1}", TradingPartnerName, DocumentType);
            }

            return string.Format("{0} - {1} - {2}", TradingPartnerName, DocumentType, Direction);
        }
    }
}
