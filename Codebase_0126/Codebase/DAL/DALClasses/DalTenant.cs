using Maarg.AllAboard.DataEntities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.AllAboard.DALClasses
{
    class DalTenant : DalEntityBase<Tenant>
    {
        public DalTenant(CloudStorageAccount storageAccount, CloudBlobContainer container)
            : base(storageAccount, container)
        {
        }

        public override string BlobDirectoryName
        {
            get { return @"tenant"; }
        }

        protected override Tenant GetExtended(Tenant entity)
        {
            DalPartner partnerDal = new DalPartner(this.storageAccount, this.container);

            entity.TenantPartner = partnerDal.Get(entity.TenantPartnerIdentifier);
            foreach (string tradingPartnerIdentifier in entity.TradingPartnersIdentifiers)
            {
                entity.TradingPartners.Add(partnerDal.Get(tradingPartnerIdentifier));
            }

            return entity;
        }

        protected override void SaveExtended(Tenant entity)
        {
            DalPartner partnerDal = new DalPartner(this.storageAccount, this.container);

            //save tenant partner
            partnerDal.Save(entity.TenantPartner as Partner);
            //save trading partners
            foreach (Partner tradingPartner in entity.TradingPartners)
            {
                partnerDal.Save(tradingPartner);
            }
        }

        protected override void DeleteExtended(Tenant entity)
        {
            DalPartner partnerDal = new DalPartner(this.storageAccount, this.container);

            //Delete all partners
            foreach (Partner tradingPartner in entity.TradingPartners)
            {
                partnerDal.Delete(tradingPartner);
            }

            //Delete tenant partner
            partnerDal.Delete(entity.TenantPartner as Partner);
        }
    }
}
