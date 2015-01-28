using System;
using System.Collections.Generic;
using Maarg.Contracts;
using Maarg.Contracts.Commerce;
using Maarg.Fatpipe.Plug.DataModel;
using Maarg.Contracts.GCValidate;
using System.IO;

namespace Maarg.AllAboard
{
    public interface IDalManager
    {
        IBasePropertyBag CreateBasePropertyBag();
        IInboundEdiPropertyBag CreateInboundEdiPropertyBag();
        IOutboundEdiPropertyBag CreateOutboundEdiPropertyBag();
        IInboundAs2PropertyBag CreateInboundAs2PropertyBag();
        IOutboundAs2PropertyBag CreateOutboundAs2PropertyBag();
        ITransformationDefinition CreateTransformationDefinition();
        IConnectorConfiguration CreateConnectorConfiguration();
        IFtpConnectorConfiguration CreateFtpConnectorConfiguration();
        IInboxConnectorConfiguration CreateInboxConnectorConfiguration();
        IBlobConnectorConfiguration CreateBlobConnectorConfiguration();

        #region Tenant
        ITenant CreateTenant();
        ITenant GetTenant(string identifier);
        List<ITenant> ListTenants();
        void SaveTenant(ITenant tenant);
        void DeleteTenant(ITenant tenant);
        List<IFtpConnectorConfiguration> ListTenantFtpConnectorConfigurations(string tenantIdentifier);
        List<IInboxConnectorConfiguration> ListTenantInboxConnectorConfigurations(string tenantIdentifier);
        #endregion

        #region IPartner
        IPartner CreatePartner();
        IPartner GetPartner(string identifier);
        IPartner GetPartnerByLoginName(string loginName);
        IPartner GetPartnerByPartnerName(string partnerName);
        List<IPartner> ListPartners();
        void UpdatePartnerStatus(string loginName, PartnerStatus newStatus);
        void SavePartner(IPartner partner); //used for both create and update
        void DeletePartner(IPartner partner);
        #endregion

        #region IPartnerAccount
        IPartnerAccount CreatePartnerAccount();
        void SavePartnerAccount(IPartnerAccount partnerAccount);
        void DeletePartnerAccount(IPartnerAccount partnerAccount);
        IPartnerAccount GetPartnerAccount(string partnerAccountIdentifier, string partnerIdentifier);
        List<IPartnerAccount> ListPartnerAccountsByPartner(string partnerIdentifier);
        List<IPartnerAccount> ListPartnerAccounts();
        #endregion

        #region Commerce
        Order CreateNewOrder();
        bool SaveOrder(Order order);
        Order GetOrder(Guid orderId);
        IList<Order> GetAllOrders();
        IList<Order> GetOrderByTenantId(string tenantId);

        //Note it is intentional that there is no method to create a subscription
        //Because they are implicitly created during order creation
        //from then on, their lifecycle is managed
        Order GetSubscription(Guid subscriptionId);
        Subscription CreateNewSubscription();

        Offer CreateNewOffer();
        bool SaveOffer(Offer offer);
        Offer GetOffer(Guid offerId);

        IList<BillingCycleSummary> GetBillingCycleSummary(Guid orderId, int howManyCycles);

        //cycleNumber of 0 means current open cycle
        // -1 means previous cycle and so on
        IList<UsageEvent> GetRawUsage(Guid orderId, int cycleNumber);
        IList<UsageEvent> GetRawUsage(DateTime dateFrom, DateTime dateTo, string homeOrg);
        IList<UsageAggregatePerUserByBillingCycle> GetUsageAggregatePerUserByBillingCycle(Guid orderId, int cycleNumber);
        IList<UsageAggregateByBillingCycle> GetUsageAggregateByBillingCycle(Guid orderId, int cycleNumber);

        bool TrackUsage(UsageEvent usageEvent);

        BillingCycle FetchOrCreateCurrentBillingCycle();

        /// <summary>
        /// Fetch a given cycle
        /// where cycleNumber = 0, implies current billing cycle
        ///                    -1, previous cycle
        ///                    -2, previous to previous and so on
        /// </summary>
        /// <param name="cycleNumber"></param>
        /// <returns></returns>
        BillingCycle GetBillingCycle(int cycleNumber);
        BillingCycle GetBillingCycle(int cycleNumber, bool forceCacheRefresh);
        bool UpdateBillingCycleStatus(int year, int month, int status);

        bool SaveOrUpdateBillingCycleSummary(BillingCycleSummary summary);

        DateTime GetCurrentTime();
        DateTime GetCurrentTime(bool refreshTimeJump);
        bool PerformTimeJump(TimeSpan timeSpan);


        //purely for testability
        bool DeleteOrder(Guid orderId);
        bool DeleteSubscription(Guid subscriptionId);
        bool DeleteOffer(Guid subscriptionId);
        #endregion

        #region PlugConfiguration
        void SavePlugConfiguration(PlugConfiguration plugConfiguration, string partnerIdentifier);
        void DeletePlugConfiguration(PlugConfiguration plugConfiguration, string partnerIdentifier);
        PlugConfiguration GetPlugConfiguration(string plugIdentifier, string partnerIdentifier);
        List<PlugConfiguration> ListPlugConfigurationsByPartner(string partnerIdentifier);
        IList<PlugConfiguration> ListAllPlugConfiguration();
        #endregion

        #region SchemaEdit
        void SavePartnerSchema(IDocumentPlug documentPlug, string partnerIdentifier);
        DocumentPlug GetPartnerSchema(string schemaIdentifier, string partnerIdentifier);
        List<DocumentPlug> ListAllSchemasByPartner(string partnerIdentifier);
        #endregion

        #region GCValidate

        List<string> GetHomeOrgList();
        List<string> GetHomeOrgList(bool forSpecCert);

        // -- TradingPartnerSpecCertMetadata
        List<TradingPartnerSpecCertMetadata> GetCertFileList();
        List<TradingPartnerSpecCertMetadata> GetCertFileList(string tradingPartnerName);
        List<TradingPartnerSpecCertMetadata> GetTradingPartnerList(int documentType, string excludeTradingPartner);
        bool SaveTradingPartnerSpecCertMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata);
        bool DeleteTradingPartnerSpecCertMetadata(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata);

        void DeleteTradingPartnerSpecCert(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata);
        void SaveTradingPartnerSpecCert(Stream tradingPartnerSpecCertStream, TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata);
        Stream GetTradingPartnerSpecCert(TradingPartnerSpecCertMetadata tradingPartnerSpecCertMetadata);

        // -- BizRuleCertMetadata
        List<BizRuleCertMetadata> GetBizRuleCertFileList();
        List<BizRuleCertMetadata> GetBizRuleCertFileList(string tradingPartnerName);
        bool SaveBizRuleCertMetadata(BizRuleCertMetadata bizRuleCertMetadata);
        bool DeleteBizRuleCertMetadata(BizRuleCertMetadata bizRuleCertMetadata);

        void DeleteBizRuleCert(BizRuleCertMetadata bizRuleCertMetadata);
        void SaveBizRuleCert(Stream tradingPartnerSpecCertStream, BizRuleCertMetadata bizRuleCertMetadata);
        Stream GetBizRuleCert(BizRuleCertMetadata bizRuleCertMetadata);

        // -- MapFilesMetadata
        List<BtsAssemblyFilesMetadata> GetBtsAssemblyFilesList();
        List<BtsAssemblyFilesMetadata> GetBtsAssemblyFilesList(string mapFileName);
        bool SaveBtsAssemblyFilesMetadata(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata);
        bool DeleteBtsAssemblyFilesMetadata(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata);

        void DeleteBtsAssemblyFiles(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata);
        void SaveBtsAssemblyFiles(Stream tradingPartnerSpecCertStream, BtsAssemblyFilesMetadata btsAssemblyFilesMetadata);
        Stream GetBtsAssemblyFiles(BtsAssemblyFilesMetadata btsAssemblyFilesMetadata);
        #endregion
    }
}
