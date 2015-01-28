using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Maarg.Fatpipe.Plug.Authoring.BtmFileHandler
{
    public static class OrganizationMetadata
    {
        private static object lockObj = new object();
        private static Dictionary<string, OrganizationDetails> OrgDomainNameToDetailsMapping;

        public static OrganizationDetails GetOrganizationDetails(string domainName)
        {
            if (OrgDomainNameToDetailsMapping == null)
            {
                lock (lockObj)
                {
                    LoadOrganizationDetails();
                }
            }

            OrganizationDetails orgDetails = null;
            OrgDomainNameToDetailsMapping.TryGetValue(domainName, out orgDetails);

            return orgDetails;
        }

        private static void LoadOrganizationDetails()
        {
            OrgDomainNameToDetailsMapping = new Dictionary<string, OrganizationDetails>();
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"BtmFileHandler\OrganizationMetadata.csv");

            string serializedOrgDetails;
            string[] orgDetailsArr;
            int id;
            string name, domainName, specCertName;

            string[] allDetails = File.ReadAllLines(fullPath);
            foreach (string line in allDetails)
            {
                orgDetailsArr = line.Split('\t');
                if (orgDetailsArr.Length >= 2)
                {
                    domainName = orgDetailsArr[0];//.Trim()
                    name = orgDetailsArr[1];

                    try
                    {
                        OrgDomainNameToDetailsMapping.Add(domainName, new OrganizationDetails()
                        {
                            DomainName = domainName,
                            Name = name,
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error adding domain name: {0} and org name: {1}", domainName, name);
                        Console.WriteLine("Error: {0}", ex);
                        throw;
                    }
                }
            }
        }
        
        internal static string GetOrganizationName(string domainName)
        {
            string orgName = null;
            OrganizationDetails orgDetails = GetOrganizationDetails(domainName);

            if (orgDetails != null)
                orgName = orgDetails.Name;

            return orgName;
        }
    }
}