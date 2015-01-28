using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;
using System.IO;

namespace Maarg.Contracts.GCValidate
{
    public class BizRuleCertMetadata : TableServiceEntity
    {
        public string TradingPartnerName { get; set; }
        public string RuleCertFileName { get; set; }
        public string UserName { get; set; }
        public DateTime WhenUpdated { get; set; }

        /// <summary>Initialize BizRuleCertMetadata from cert file name. It returns list of errors.</summary>
        /// <param name="certFileFullName"></param>
        /// <returns>Errors</returns>
        public List<string> Initialize(string certFileFullName)
        {
            return this.Initialize(certFileFullName, null, DateTime.MinValue);
        }

        /// <summary>Initialize BizRuleCertMetadata from cert file name. It returns list of errors.</summary>
        /// <param name="certFileFullName"></param>
        /// <param name="userName"></param>
        /// <param name="updateTime"></param>
        /// <returns>Errors</returns>
        public List<string> Initialize(string certFileFullName, string userName, DateTime updateTime)
        {
            List<string> errors = new List<string>();

            string fileExtension = Path.GetExtension(certFileFullName).ToLowerInvariant();
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
                errors.Add("Invalid file type");
            else
            {
                string certFileName = Path.GetFileNameWithoutExtension(certFileFullName);

                string[] fileNameParts = certFileName.Split('-');

                if (fileNameParts == null || fileNameParts.Length < 2)
                {
                    errors.Add("Biz rule cert file name does not adhere to format '<Partner Name>|Global - Biz Rule [- <text>]'");
                }
                else
                {
                    this.RuleCertFileName = Path.GetFileName(certFileFullName);
                    this.TradingPartnerName = fileNameParts[0].Trim();

                    if (fileNameParts[1].Trim().ToLowerInvariant() != "biz rule")
                        errors.Add("Second part in biz rule cert file name should be 'Biz Rule'.");

                    if (this.TradingPartnerName.Length == 0)
                        errors.Add("Partner name cannont be empty in biz rule cert file name.");

                    this.UserName = userName;
                    this.WhenUpdated = updateTime;
                }
            }

            return errors;
        }

        public string GetCertFileDisplayName()
        {
            return Path.GetFileNameWithoutExtension(RuleCertFileName);
        }
    }
}
