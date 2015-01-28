using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Maarg.AllAboard.DALClasses;
using Maarg.AllAboard.DataEntities;
using Maarg.Contracts;
using Maarg.Fatpipe.LoggingService;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Maarg.Contracts.Commerce;
using Maarg.Dal.Commerce;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Contracts.GCValidate;
using Maarg.Dal.GCValidate;
using System.IO;
using System.Linq;

namespace Maarg.AllAboard
{
    /// <summary>
    /// Windows Azure Storage based XML DAL Manager
    /// </summary>
    class AzureDalManager : IDalManager
    {
        /// <summary>
        /// Windows Azure Storage Account
        /// </summary>
        private CloudStorageAccount storageAccount = null;

        /// <summary>
        /// Windows Azure Storage Blob Container name used to store Xml files 
        /// for all partners
        /// </summary>
        private CloudBlobContainer container = null;

        /// <summary>
        /// Constructs Windows Azure Storage based XML DAL Manager
        /// </summary>
        /// <param name="storageAccountConnectionString">The storage account connection string.</param>
        /// <param name="containerName">The Windows Azure Storage Blob Container used to store Xml files.</param>
        public AzureDalManager(string storageAccountConnectionString, string containerName)
        {
            if (string.IsNullOrWhiteSpace(storageAccountConnectionString))
            {
                throw new ArgumentException("storageAccountConnectionString");
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("containerName");
            }

            // Parse storage account connection string
            this.storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            // Set and create container if needed
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExist();
        }

        public IBasePropertyBag CreateBasePropertyBag()
        {
            return new BasePropertyBag();
        }

        public IConnectorConfiguration CreateConnectorConfiguration()
        {
            return new ConnectorConfiguration();
        }

        public ITransformationDefinition CreateTransformationDefinition()
        {
            return new TransformationDefinition();
        }

        public IInboundEdiPropertyBag CreateInboundEdiPropertyBag()
        {
            return new InboundEdiPropertyBag();
        }


        public IOutboundEdiPropertyBag CreateOutboundEdiPropertyBag()
        {
            return new OutboundEdiPropertyBag();
        }


        public IInboundAs2PropertyBag CreateInboundAs2PropertyBag()
        {
            return new InboundAs2PropertyBag();
        }

        public IOutboundAs2PropertyBag CreateOutboundAs2PropertyBag()
        {
            return new OutboundAs2PropertyBag();
        }

        public IFtpConnectorConfiguration CreateFtpConnectorConfiguration()
        {
            return new FtpConnectorConfiguration();
        }

        public IInboxConnectorConfiguration CreateInboxConnectorConfiguration()
        {
            return new InboxConnectorConfiguration();
        }

        public IBlobConnectorConfiguration CreateBlobConnectorConfiguration()
        {
            return new BlobConnectorConfiguration();
        }

        #region ITenant
        public ITenant CreateTenant()
        {
            return new Tenant();
        }

        public ITenant GetTenant(string identifier)
        {
            DalTenant dal = new DalTenant(this.storageAccount, this.container);
            return dal.Get(identifier);
        }

        public List<ITenant> ListTenants()
        {
            DalTenant dal = new DalTenant(this.storageAccount, this.container);
            return dal.List().ConvertAll<ITenant>(tenant => tenant);
        }

        public void SaveTenant(ITenant tenant)
        {
            DalTenant dal = new DalTenant(this.storageAccount, this.container);
            dal.Save(tenant as Tenant);
        }

        public void DeleteTenant(ITenant tenant)
        {
            DalTenant dal = new DalTenant(this.storageAccount, this.container);
            dal.Delete(tenant as Tenant);
        }

        public List<IFtpConnectorConfiguration> ListTenantFtpConnectorConfigurations(string tenantIdentifier)
        {
            if (tenantIdentifier == null)
            {
                throw new ArgumentNullException("tenantIdentifier");
            }

            ITenant tenant = this.GetTenant(tenantIdentifier);
            return tenant.ListTenantFtpConnectorConfigurations();
        }

        public List<IInboxConnectorConfiguration> ListTenantInboxConnectorConfigurations(string tenantIdentifier)
        {
            if (tenantIdentifier == null)
            {
                throw new ArgumentNullException("tenantIdentifier");
            }

            ITenant tenant = this.GetTenant(tenantIdentifier);
            return tenant.ListTenantInboxConnectorConfigurations();
        }
        #endregion

        #region IPartner
        public IPartner CreatePartner()
        {
            return new Partner();
        }

        public IPartner GetPartner(string identifier)
        {
            DalPartner dal = new DalPartner(this.storageAccount, this.container);
            return dal.Get(identifier);
        }

        public IPartner GetPartnerByLoginName(string loginName)
        {
            IPartner result = null;

            string location = @"AzureDalManager.GetPartnerByLoginName";
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                CloudBlobClient client = this.storageAccount.CreateCloudBlobClient();
                
                BlobRequestOptions requestOption = new BlobRequestOptions() { UseFlatBlobListing = true };
                string prefix = string.Format(CultureInfo.InvariantCulture, "{0}/partneraccount/", this.container.Name);
                foreach (IListBlobItem partnetAccountBlobItem in client.ListBlobsWithPrefix(prefix, requestOption))
                {
                    string[] segments = partnetAccountBlobItem.Uri.Segments;
                    if (segments != null && segments.Length > 1)
                    {
                        string lastSegment = segments[segments.Length - 1];
                        if (!string.IsNullOrWhiteSpace(lastSegment))
                        {
                            if (loginName.Equals(lastSegment, StringComparison.OrdinalIgnoreCase))
                            {
                                string partnerIdentifier = segments[segments.Length - 2].Trim('/');
                                if (!string.IsNullOrWhiteSpace(partnerIdentifier))
                                {
                                    DalPartner dal = new DalPartner(this.storageAccount, this.container);
                                    result = dal.Get(partnerIdentifier);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LoggerFactory.Logger.Warning(location, EventId.DALGetPartnerByLoginName,
                    "Error executing AzureDalManager.GetPartnerByLoginName({0}) in container {1}: {2}."
                    , loginName, this.container.Name, exception.ToString());
            }
            finally
            {
                watch.Stop();
                LoggerFactory.Logger.Debug(location, "AzureDalManager.GetPartnerByLoginName() finished in {0} ms.", watch.ElapsedMilliseconds);
            }

            return result;
        }

        public IPartner GetPartnerByPartnerName(string partnerName)
        {
            List<IPartner> allPartners = this.ListPartners();
            if (allPartners != null && allPartners.Count > 0)
            {
                foreach (IPartner partner in allPartners)
                {
                    if (string.Compare(partner.Name, partnerName, true) == 0)
                    {
                        return partner;
                    }
                }
            }

            return null;
        }

        public List<IPartner> ListPartners()
        {
            DalPartner dal = new DalPartner(this.storageAccount, this.container);
            return dal.List().ConvertAll<IPartner>(partner => partner);
        }

        public void UpdatePartnerStatus(string loginName, PartnerStatus newStatus)
        {
            IPartner partner = GetPartnerByLoginName(loginName);

            DalPartner dal = new DalPartner(this.storageAccount, this.container);
            partner.Status = newStatus;
            dal.Save(partner as Partner);
        }

        public void SavePartner(IPartner partner)
        {
            DalPartner dal = new DalPartner(this.storageAccount, this.container);
            dal.Save(partner as Partner);
        }

        public void DeletePartner(IPartner partner)
        {
            DalPartner dal = new DalPartner(this.storageAccount, this.container);
            dal.Delete(partner as Partner);
        }
        #endregion

        #region IPartnerAccount
        public IPartnerAccount CreatePartnerAccount()
        {
            return new PartnerAccount();
        }

        public void SavePartnerAccount(IPartnerAccount partnerAccount)
        {
            DalPartnerAccount dal = new DalPartnerAccount(this.storageAccount, this.container, partnerAccount.PartnerIdentifier);
            dal.Save(partnerAccount as PartnerAccount);
        }

        public void DeletePartnerAccount(IPartnerAccount partnerAccount)
        {
            if (partnerAccount == null)
            {
                throw new ArgumentNullException("partnerAccount");
            }

            if (string.IsNullOrEmpty(partnerAccount.PartnerIdentifier))
            {
                throw new ArgumentException("IPartnerAccount must have a valid PartnerIdentifier.", "partnerAccount");
            }

            DalPartnerAccount dal = new DalPartnerAccount(this.storageAccount, this.container, partnerAccount.PartnerIdentifier);
            dal.Delete(partnerAccount as PartnerAccount);
        }

        public IPartnerAccount GetPartnerAccount(string partnerAccountIdentifier, string partnerIdentifier)
        {
            if (partnerAccountIdentifier == null)
            {
                throw new ArgumentNullException("partnerAccountIdentifier");
            }

            if (partnerIdentifier == null)
            {
                throw new ArgumentNullException("partnerIdentifier");
            }
            
            DalPartnerAccount dal = new DalPartnerAccount(this.storageAccount, this.container, partnerIdentifier);
            return dal.Get(partnerAccountIdentifier);
        }

        public List<IPartnerAccount> ListPartnerAccountsByPartner(string partnerIdentifier)
        {
            if (string.IsNullOrWhiteSpace(partnerIdentifier))
            {
                throw new ArgumentNullException("partnerIdentifier");
            }

            DalPartnerAccount dal = new DalPartnerAccount(this.storageAccount, this.container, partnerIdentifier);
            return dal.List().ConvertAll<IPartnerAccount>(partnerAccount => partnerAccount);
        }

        public List<IPartnerAccount> ListPartnerAccounts()
        {
            List<IPartnerAccount> result = new List<IPartnerAccount>();

            DalPartner dalPartner = new DalPartner(this.storageAccount, this.container);
            foreach (IPartner partner in dalPartner.List())
            {
                DalPartnerAccount dal = new DalPartnerAccount(this.storageAccount, this.container, partner.Identifier);
                result.AddRange(dal.List().ConvertAll<IPartnerAccount>(partnerAccount => partnerAccount));
            }

            return result;
        }

        #endregion

        #region Commerce
        public Order CreateNewOrder()
        {
            return new Order();
        }

        public bool SaveOrder(Order order)
        {
            DalOrder dalOrder = new DalOrder(this, this.storageAccount);
            return dalOrder.SaveOrder(order);
        }

        public Order GetOrder(Guid orderId)
        {
            DalOrder dalOrder = new DalOrder(this, this.storageAccount);
            return dalOrder.GetOrder(orderId);
        }

        public IList<Order> GetOrderByTenantId(string tenantId)
        {
            DalOrder dalOrder = new DalOrder(this, this.storageAccount);
            return dalOrder.GetOrderByTenantId(tenantId);
        }

        public IList<Order> GetAllOrders()
        {
            DalOrder dalOrder = new DalOrder(this, this.storageAccount);
            return dalOrder.GetAllOrders();
        }

        public bool SaveOrUpdateBillingCycleSummary(BillingCycleSummary summary)
        {
            DalBillingCycleSummary dal = new DalBillingCycleSummary(this, this.storageAccount);
            return dal.SaveOrUpdateBillingCycleSummary(summary);
        }

        public Offer CreateNewOffer()
        {
            return new Offer();
        }

        public Subscription CreateNewSubscription()
        {
            return new Subscription();
        }

        //Note it is intentional that there is no method to create a subscription
        //Because they are implicitly created during order creation
        //from then on, their lifecycle is managed
        public Order GetSubscription(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public bool SaveOffer(Offer offer)
        {
            DalOffer dalOffer = new DalOffer(this, this.storageAccount);
            return dalOffer.SaveOffer(offer);
        }

        public Offer GetOffer(Guid offerId)
        {
            DalOffer dalOffer = new DalOffer(this, this.storageAccount);
            return dalOffer.GetOffer(offerId);
        }

        public bool TrackUsage(UsageEvent usageEvent)
        {
            DalUsageEvent dal = new DalUsageEvent(this, this.storageAccount);
            return dal.TrackUsage(usageEvent);
        }

        //cycleNumber of 0 means current open cycle
        // -1 means previous cycle and so on
        public IList<UsageEvent> GetRawUsage(Guid orderId, int cycleNumber)
        {
            DalUsageEvent dal = new DalUsageEvent(this, this.storageAccount);
            return dal.GetRawUsage(orderId, cycleNumber);
        }

        public IList<UsageEvent> GetRawUsage(DateTime dateFrom, DateTime dateTo, string homeOrg)
        {
            DalUsageEvent dal = new DalUsageEvent(this, this.storageAccount);
            return dal.GetRawUsage(dateFrom, dateTo, homeOrg);
        }

        public bool UpdateBillingCycleStatus(int year, int month, int status)
        {
            DalBillingCycle dal = new DalBillingCycle(this, this.storageAccount);
            return dal.UpdateBillingCycleStatus(year, month, status);
        }

        public IList<UsageAggregateByBillingCycle> GetUsageAggregateByBillingCycle(Guid orderId, int cycleNumber)
        {
            DalUsageEvent dal = new DalUsageEvent(this, this.storageAccount);
            return dal.GetUsageAggregateByBillingCycle(orderId, cycleNumber);
        }

        public IList<UsageAggregatePerUserByBillingCycle> GetUsageAggregatePerUserByBillingCycle(Guid orderId, int cycleNumber)
        {
            DalUsageEvent dal = new DalUsageEvent(this, this.storageAccount);
            return dal.GetUsageAggregatePerUserByBillingCycle(orderId, cycleNumber);
        }


        static Cache billingCycleCache = new Cache("BillingCycleCache", 60 * 60 * 24, 100); //cache timeout = 24 hour
        const string CurrentBillingCycleKey = "CurrentBillingCycleKey";
        public BillingCycle FetchOrCreateCurrentBillingCycle()
        {
            BillingCycle cycle = GetBillingCycle(0);

            if (cycle != null)
            {
                //check that cycle is still current
                if (cycle.StartDate.Month != this.GetCurrentTime().Month)
                {
                    cycle = null; //this will invoke cache refresh and creation of new billing cycle
                }
            }

            if (cycle == null) //create a new cycle
            {
                DalBillingCycle dal = new DalBillingCycle(this, this.storageAccount);
                cycle = dal.GetLatestBillingCycle();

                if (cycle == null) //create a new cycle, since persisted storage does not contain any cycle
                {
                    DateTime currentDate = this.GetCurrentTime();
                    DateTime startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                    DateTime endDate = new DateTime(currentDate.Year, currentDate.Month, 28); //using 28 since it exists in every month, leap year etc

                    cycle = new BillingCycle { Id = Guid.NewGuid(), StartDate = startDate, EndDate = endDate };
                    dal.SaveBillingCycle(cycle);
                    string cacheKey = string.Format("{0}#{1}", cycle.StartDate.Year.ToString(), cycle.StartDate.Month.ToString());
                    billingCycleCache.AddObject(cacheKey, cycle); //add to cache
                }
            }

            return cycle;
        }

        /// <summary>
        /// Fetch a given cycle
        /// where cycleNumber = 0, implies current billing cycle
        ///                    -1, previous cycle
        ///                    -2, previous to previous and so on
        /// </summary>
        /// <param name="cycleNumber"></param>
        /// <returns></returns>
        public BillingCycle GetBillingCycle(int cycleNumber, bool forceCacheRefresh)
        {
            if (cycleNumber > 0 || cycleNumber < -11)
            {
                throw new ArgumentException("Cycles from current to last 1 year are supported");
            }

            int currentMonth = this.GetCurrentTime().Month;

            //since cycle number is counted -ve backwards, so need to add to current month
            int requestedCycleMonth = currentMonth + cycleNumber;
            int requestedYear = this.GetCurrentTime().Year;
            if (requestedCycleMonth < 1)
            {
                //wrap around of 1 year
                requestedCycleMonth += 12;
                requestedYear--;
            }

            string cacheKey = string.Format("{0}#{1}", requestedYear, requestedCycleMonth);
            BillingCycle cycle = billingCycleCache.GetObject(cacheKey) as BillingCycle;

            if (cycle == null || forceCacheRefresh)
            {
                //fetch from table storage
                DalBillingCycle dal = new DalBillingCycle(this, this.storageAccount);
                cycle = dal.GetBillingCycle(requestedYear, requestedCycleMonth);
            }

            if (cycle != null) //add to cache
            {
                billingCycleCache.AddObject(cacheKey, cycle);
            }

            else if (forceCacheRefresh)
            {
                //dummy up the cache entry simulating discarding the entry
                billingCycleCache.AddObject(cacheKey, "dummy");
            }

            return cycle;

        }

        public BillingCycle GetBillingCycle(int cycleNumber)
        {
            return GetBillingCycle(cycleNumber, false);
        }

        public DateTime GetCurrentTime()
        {
            return GetCurrentTime(false);
        }

        static TimeOffset offset = null;
        public DateTime GetCurrentTime(bool refreshTimeJump)
        {
            if (refreshTimeJump)
            {
                DalTimeoffset dal = new DalTimeoffset(this, this.storageAccount);
                offset = dal.GetTimeOffset();
            }

            DateTime baseTime = DateTime.UtcNow;
            DateTime returnTime = offset != null ? 
                baseTime.Add(new TimeSpan(offset.Days, offset.Hours, offset.Minutes, 0)) : baseTime;
            return returnTime;
        }

        public bool PerformTimeJump(TimeSpan timeSpan)
        {
            offset = new TimeOffset { Days = timeSpan.Days, Hours = timeSpan.Hours, Minutes = timeSpan.Minutes };
            DalTimeoffset dal = new DalTimeoffset(this, this.storageAccount);
            return dal.SaveTimeoffset(offset);
        }

        //purely for testability
        public bool DeleteOrder(Guid orderId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSubscription(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public bool DeleteOffer(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public static BillingCycleSummary CreateNewBillingCycleSummary()
        {
            return new BillingCycleSummary();
        }

        public IList<BillingCycleSummary> GetBillingCycleSummary(Guid orderId, int howManyCycles)
        {
            IList<BillingCycleSummary> summaryList = new List<BillingCycleSummary>();
            DalBillingCycleSummary dal = new DalBillingCycleSummary(this, this.storageAccount);
            
            for (int i = 0; i < howManyCycles; i++)
            {
                BillingCycle cycle = GetBillingCycle(-1 * i);
                if (cycle == null)
                {
                    break;
                }

                BillingCycleSummary summary = dal.GetBillingCycleSummary(orderId, cycle.StartDate.Year, cycle.StartDate.Month);
                summaryList.Add(summary);
            }

            return summaryList;
        }

        

        #endregion

        #region PlugConfiguration

        public void SavePlugConfiguration(PlugConfiguration plugConfiguration, string partnerIdentifier)
        {
            if (plugConfiguration == null)
            {
                throw new ArgumentNullException("plugConfiguration");
            }

            if (string.IsNullOrEmpty(partnerIdentifier))
            {
                throw new ArgumentException("Plug Configuration must have a valid PartnerIdentifier.", "partnerIdentifier");
            }

            DalPlugConfiguration dal = new DalPlugConfiguration(this.storageAccount, this.container, partnerIdentifier);
            dal.Save(plugConfiguration);
        }

        public void DeletePlugConfiguration(PlugConfiguration plugConfiguration, string partnerIdentifier)
        {
            if (plugConfiguration == null)
            {
                throw new ArgumentNullException("plugConfiguration");
            }

            if (string.IsNullOrEmpty(partnerIdentifier))
            {
                throw new ArgumentException("Plug Configuration must have a valid PartnerIdentifier.", "partnerIdentifier");
            }

            DalPlugConfiguration dal = new DalPlugConfiguration(this.storageAccount, this.container, partnerIdentifier);
            dal.Delete(plugConfiguration);
        }

        public PlugConfiguration GetPlugConfiguration(string plugIdentifier, string partnerIdentifier)
        {
            if (plugIdentifier == null)
            {
                throw new ArgumentNullException("plugIdentifier");
            }

            if (partnerIdentifier == null)
            {
                throw new ArgumentNullException("partnerIdentifier");
            }

            DalPlugConfiguration dal = new DalPlugConfiguration(this.storageAccount, this.container, partnerIdentifier);
            return dal.Get(plugIdentifier);
        }

        public List<PlugConfiguration> ListPlugConfigurationsByPartner(string partnerIdentifier)
        {
            if (string.IsNullOrWhiteSpace(partnerIdentifier))
            {
                throw new ArgumentNullException("partnerIdentifier");
            }

            DalPlugConfiguration dal = new DalPlugConfiguration(this.storageAccount, this.container, partnerIdentifier);
            return dal.List();
        }

        public IList<PlugConfiguration> ListAllPlugConfiguration()
        {
            IList<PlugConfiguration> list = new List<PlugConfiguration>();

            //Start test data population - remove when method is implemented
            PlugConfiguration config1 = new PlugConfiguration()
            {
                SourceName = "SourceABC",
                SourceTransportType = PlugConfiguration.TransportTypeSmtp,
                SourceSmtpAddress = "raj@cloudmaarg.com",
                TargetName = "TargetABC",
                TargetTransportType = PlugConfiguration.TransportTypeSmtp,
                TargetSmtpAddress = "suraj@cloudmaarg.com"
            };

            PlugConfiguration config2 = new PlugConfiguration()
            {
                SourceName = "SourceABC",
                SourceTransportType = PlugConfiguration.TransportTypeSmtp,
                SourceSmtpAddress = "suraj@cloudmaarg.com",
                TargetName = "TargetABC",
                TargetTransportType = PlugConfiguration.TransportTypeSmtp,
                TargetSmtpAddress = "raj@cloudmaarg.com"
            };

            PlugConfiguration config3 = new PlugConfiguration()
            {
                SourceName = "SourceABC",
                SourceTransportType = PlugConfiguration.TransportTypeSmtp,
                SourceSmtpAddress = "aparnam@cloudmaarg.com",
                TargetName = "TargetABC",
                TargetTransportType = PlugConfiguration.TransportTypeSmtp,
                TargetSmtpAddress = "suren@cloudmaarg.com"
            };

            PlugConfiguration config4 = new PlugConfiguration()
            {
                SourceName = "SourceABC",
                SourceTransportType = PlugConfiguration.TransportTypeSmtp,
                SourceSmtpAddress = "suren@cloudmaarg.com",
                TargetName = "TargetABC",
                TargetTransportType = PlugConfiguration.TransportTypeSmtp,
                TargetSmtpAddress = "aparnam@cloudmaarg.com"
            };

            PlugConfiguration config5 = new PlugConfiguration()
            {
                SourceName = "SourceABC",
                SourceTransportType = PlugConfiguration.TransportTypeSmtp,
                SourceSmtpAddress = "zainal@cloudmaarg.com",
                TargetName = "TargetABC",
                TargetTransportType = PlugConfiguration.TransportTypeSmtp,
                TargetSmtpAddress = "zainal@cloudmaarg.com"
            };

            PlugConfiguration config6 = new PlugConfiguration()
            {
                SourceName = "SourceABC",
                SourceTransportType = PlugConfiguration.TransportTypeSmtp,
                SourceSmtpAddress = "rob@cloudmaarg.com",
                TargetName = "TargetABC",
                TargetTransportType = PlugConfiguration.TransportTypeSmtp,
                TargetSmtpAddress = "rob@cloudmaarg.com"
            };

            /*
            list.Add(config1);
            list.Add(config2);
            list.Add(config3);
            list.Add(config4);
            list.Add(config5);
            list.Add(config6);
             */
            //end test data population


            return list;

        }
        #endregion

        #region SchemaEdit

        public void SavePartnerSchema(IDocumentPlug documentPlug, string partnerIdentifier)
        {
            if (documentPlug == null)
            {
                throw new ArgumentNullException("documentPlug");
            }

            if (string.IsNullOrEmpty(partnerIdentifier))
            {
                throw new ArgumentException("To save schema, it must have a valid PartnerIdentifier.", "partnerIdentifier");
            }

            DALSchema dal = new DALSchema(this.storageAccount, this.container, partnerIdentifier);
            dal.Save(documentPlug as DocumentPlug);
        }

        public DocumentPlug GetPartnerSchema(string schemaIdentifier, string partnerIdentifier)
        {
            if (schemaIdentifier == null)
            {
                throw new ArgumentNullException("schemaIdentifier");
            }

            if (partnerIdentifier == null)
            {
                throw new ArgumentNullException("partnerIdentifier");
            }

            DALSchema dal = new DALSchema(this.storageAccount, this.container, partnerIdentifier);
            return dal.Get(schemaIdentifier);
        }

        public List<DocumentPlug> ListAllSchemasByPartner(string partnerIdentifier)
        {
            if (string.IsNullOrWhiteSpace(partnerIdentifier))
            {
                throw new ArgumentNullException("partnerIdentifier");
            }

            DALSchema dal = new DALSchema(this.storageAccount, this.container, partnerIdentifier);
            return dal.List();
        }
        #endregion

        #region GCValidate
        public List<string> GetHomeOrgList()
        {
            // TODO: Uncomment following 2 lines and remove rest of the lines once
            // Ux start using GetHomeOrgList(bool) API
            //DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);
            //return dal.GetHomeOrgList();
            List<string> homeOrgList;

            DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);
            homeOrgList = dal.GetHomeOrgList();

            DALBizRuleCertMetadata bizDal = new DALBizRuleCertMetadata(this.storageAccount);
            homeOrgList.AddRange(bizDal.GetHomeOrgList());

            return homeOrgList.Distinct().ToList();
        }

        public List<string> GetHomeOrgList(bool forSpecCert)
        {
            List<string> homeOrgList;

            if (forSpecCert)
            {
                DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);
                homeOrgList = dal.GetHomeOrgList();
            }
            else
            {
                DALBizRuleCertMetadata dal = new DALBizRuleCertMetadata(this.storageAccount);
                homeOrgList = dal.GetHomeOrgList();
            }

            return homeOrgList.Distinct().ToList();
        }

        #region TradingPartnerSpecCertMetadata
        public List<TradingPartnerSpecCertMetadata> GetCertFileList()
        {
            DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);

            return dal.GetCertFileList();
        }

        public List<TradingPartnerSpecCertMetadata> GetCertFileList(string tradingPartnerName)
        {
            DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);

            return dal.GetCertFileList(tradingPartnerName);
        }

        public List<TradingPartnerSpecCertMetadata> GetTradingPartnerList(int documentType, string excludeTradingPartner)
        {
            DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);

            return dal.GetTradingPartnerList(documentType, excludeTradingPartner);
        }

        public bool SaveTradingPartnerSpecCertMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata)
        {
            DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);

            return dal.SaveTradingPartnerSpecCertMetadata(tradingPartnerSpecCertMetadata);
        }

        public void SaveTradingPartnerSpecCert(Stream tradingPartnerSpecCertStream, TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata)
        {
            DALTradingPartnerSpecCert dal = new DALTradingPartnerSpecCert(this.storageAccount, this.container);

            dal.SaveTradingPartnerSpecCert(tradingPartnerSpecCertStream, tradingPartnerSpecCertMetadata);
        }

        public bool DeleteTradingPartnerSpecCertMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata)
        {
            DALTradingPartnerSpecCertMetadata dal = new DALTradingPartnerSpecCertMetadata(this.storageAccount);

            return dal.DeleteTradingPartnerSpecCertMetadata(tradingPartnerSpecCertMetadata);
        }

        public void DeleteTradingPartnerSpecCert(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata)
        {
            DALTradingPartnerSpecCert dal = new DALTradingPartnerSpecCert(this.storageAccount, this.container);

            dal.DeleteTradingPartnerSpecCert(tradingPartnerSpecCertMetadata);
        }


        public Stream GetTradingPartnerSpecCert(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata)
        {
            DALTradingPartnerSpecCert dal = new DALTradingPartnerSpecCert(this.storageAccount, this.container);

            return dal.GetTradingPartnerSpecCert(tradingPartnerSpecCertMetadata);
        } 
        #endregion

        #region BizRuleCertMetadata
        public List<BizRuleCertMetadata> GetBizRuleCertFileList()
        {
            DALBizRuleCertMetadata dal = new DALBizRuleCertMetadata(this.storageAccount);

            return dal.GetBizRuleCertFileList();
        }

        public List<BizRuleCertMetadata> GetBizRuleCertFileList(string tradingPartnerName)
        {
            DALBizRuleCertMetadata dal = new DALBizRuleCertMetadata(this.storageAccount);

            return dal.GetBizRuleCertFileList(tradingPartnerName);
        }

        public bool SaveBizRuleCertMetadata(BizRuleCertMetadata bizRuleCertMetadata)
        {
            DALBizRuleCertMetadata dal = new DALBizRuleCertMetadata(this.storageAccount);

            return dal.SaveBizRuleCertMetadata(bizRuleCertMetadata);
        }

        public void SaveBizRuleCert(Stream bizRuleCertStream, BizRuleCertMetadata bizRuleCertMetadata)
        {
            DALBizRuleCert dal = new DALBizRuleCert(this.storageAccount, this.container);

            dal.SaveBizRuleCert(bizRuleCertStream, bizRuleCertMetadata);
        }

        public bool DeleteBizRuleCertMetadata(BizRuleCertMetadata bizRuleCertMetadata)
        {
            DALBizRuleCertMetadata dal = new DALBizRuleCertMetadata(this.storageAccount);

            return dal.DeleteBizRuleCertMetadata(bizRuleCertMetadata);
        }

        public void DeleteBizRuleCert(BizRuleCertMetadata bizRuleCertMetadata)
        {
            DALBizRuleCert dal = new DALBizRuleCert(this.storageAccount, this.container);

            dal.DeleteBizRuleCert(bizRuleCertMetadata);
        }


        public Stream GetBizRuleCert(BizRuleCertMetadata bizRuleCertMetadata)
        {
            DALBizRuleCert dal = new DALBizRuleCert(this.storageAccount, this.container);

            return dal.GetBizRuleCert(bizRuleCertMetadata);
        }
        #endregion

        #region MapFilesMetadata
        public List<BtsAssemblyFilesMetadata> GetBtsAssemblyFilesList()
        {
            DALBtsAssemblyFilesMetadata dal = new DALBtsAssemblyFilesMetadata(this.storageAccount);

            return dal.GetBtsAssemblyFilesList();
        }

        public List<BtsAssemblyFilesMetadata> GetBtsAssemblyFilesList(string fileName)
        {
            DALBtsAssemblyFilesMetadata dal = new DALBtsAssemblyFilesMetadata(this.storageAccount);

            return dal.GetBtsAssemblyFilesList(fileName);
        }

        public bool SaveBtsAssemblyFilesMetadata(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            DALBtsAssemblyFilesMetadata dal = new DALBtsAssemblyFilesMetadata(this.storageAccount);

            return dal.SaveBtsAssemblyFilesMetadata(btsAssemblyFilesMetadata);
        }

        public void SaveBtsAssemblyFiles(Stream tradingPartnerSpecCertStream, BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            DALMapFiles dal = new DALMapFiles(this.storageAccount, this.container);

            dal.SaveBtsAssemblyFiles(tradingPartnerSpecCertStream, btsAssemblyFilesMetadata);
        }

        public bool DeleteBtsAssemblyFilesMetadata(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            DALBtsAssemblyFilesMetadata dal = new DALBtsAssemblyFilesMetadata(this.storageAccount);

            return dal.DeleteBtsAssemblyFilesMetadata(btsAssemblyFilesMetadata);
        }

        public void DeleteBtsAssemblyFiles(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            DALMapFiles dal = new DALMapFiles(this.storageAccount, this.container);

            dal.DeleteBtsAssemblyFiles(btsAssemblyFilesMetadata);
        }

        public Stream GetBtsAssemblyFiles(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata)
        {
            DALMapFiles dal = new DALMapFiles(this.storageAccount, this.container);

            return dal.GetBtsAssemblyFiles(btsAssemblyFilesMetadata);
        }
        #endregion
        #endregion
    }
}