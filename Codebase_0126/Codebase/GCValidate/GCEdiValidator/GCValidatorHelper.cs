using Maarg.AllAboard;
using Maarg.Contracts.Commerce;
using Maarg.Contracts.GCValidate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maarg.Fatpipe.EDIPlug.GCEdiValidator
{
    public static class GCValidatorHelper
    {
        public static bool AddDummyDataForCertValidation = false;
        public static bool AddDummyDataForInstanceValidation = false;

        // BizRule cert file name pattern - <Partner Name> - Biz Rule - <anything>.xlsx
        // Spec cert file name pattern - <Partner Name> - Spec Cert - <version> - <Message domain id> - <direction>.xlsx
        public static CertFileType GetCertFileType(string certFileName)
        {
            CertFileType certFileType = CertFileType.Invalid;

            if (string.IsNullOrWhiteSpace(certFileName) == false)
            {
                certFileName = Path.GetFileNameWithoutExtension(certFileName);
                string[] fileNameParts = certFileName.Split('-');

                if (fileNameParts.Length > 2)
                {
                    fileNameParts[1] = fileNameParts[1].Trim();
                    if (string.Equals(fileNameParts[1], "Biz Rule", StringComparison.OrdinalIgnoreCase) == true)
                        certFileType = CertFileType.BizRuleCert;
                    else
                        if (string.Equals(fileNameParts[1], "Spec Cert", StringComparison.OrdinalIgnoreCase) == true)
                            certFileType = CertFileType.SpecCert;
                }
            }

            return certFileType;
        }

        public static void AddUsageEvent(string userName, string homeOrgName, string partnerName, string certFile, EDIValidationResult result,
            TimeSpan executionTime, string testFileName, string service, IDalManager dal)
        {
            string validationStatus = "Success";
            if (result != null)
            {
                if (!result.IsValid)
                    validationStatus = "Error";
                else
                    if (result.SegmentValidationResults != null && result.SegmentValidationResults.Count > 0)
                        validationStatus = "Warning";
            }

            List<string> transactionNumbers = result.TransactionNumbers;
            if (transactionNumbers == null)
                transactionNumbers = new List<string>();
            if (transactionNumbers.Count == 0)
                transactionNumbers.Add(string.Empty);

            string instanceFileName;
            foreach (string transactionNumber in transactionNumbers)
            {
                instanceFileName = testFileName;

                if (string.IsNullOrWhiteSpace(transactionNumber) == false)
                    instanceFileName += " - " + transactionNumber;

                AddUsageEvent(userName, homeOrgName, partnerName, certFile, validationStatus, executionTime, instanceFileName, service, dal);
            }
        }

        public static void AddUsageEvent(string userName, string homeOrgName, string partnerName, string certFile, string validationStatus,
            TimeSpan executionTime, string testFileName, string service, IDalManager dal)
        {
            // Hardcoded order since we don't create order during sign up yet.
            Order order = new Order()
                {
                    Id = Guid.Parse("75654e42-c3fe-482b-b52a-024d073e1ea7"),
                    OfferId = Guid.Parse("5ddd32f3-1783-4bbe-854f-4c5d5955df7f"),
                    SeatCount = 1,
                    TenantId = "GCom",
                    UserId = userName,
                };

            UsageEvent usageEvent = new UsageEvent()
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ResourceId = "EdiValidation",
                    TenantId = order.TenantId,
                    Timestamp = DateTime.UtcNow,
                    WhenConsumed = DateTime.UtcNow,
                    UserId = order.UserId,
                    AmountConsumed = 1.0d,

                    HomeOrgName = homeOrgName,
                    PartnerName = partnerName,
                    SpecCertName = certFile,
                    InstanceFileName = testFileName,
                    ValidationStatus = validationStatus,
                    TimeOfValidationInMs = (int)executionTime.TotalMilliseconds,

                    Service = service,
                };

            // For unit tests dal will be null
            if(dal != null)
                dal.TrackUsage(usageEvent);
        }
    }
}
