using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maarg.Contracts.Commerce;
using Maarg.AllAboard;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using System.Xml.Serialization;
using System.IO;

namespace Maarg.Dal.Commerce
{
    public class DalOffer : BaseCommerceDal
    {
        public const string OfferTableName = "OfferTable";
        

        public DalOffer(IDalManager manager, CloudStorageAccount account)
            : base(manager, account, OfferTableName)
        {
        }

        public bool SaveOffer(Offer off)
        {
            OfferTableEntity offer = new OfferTableEntity(off);
            offer.PartitionKey = off.Id.ToString();
            offer.RowKey = off.Id.ToString();
            offer.Timestamp = this.manager.GetCurrentTime();

            
            this.tableContext.AddObject(OfferTableName, offer);
            //this.tableContext.SaveChanges();
            //return true;
            DataServiceResponse response = this.tableContext.SaveChangesWithRetries(SaveChangesOptions.Batch);
            return response.BatchStatusCode == Http200;
        }

        public Offer GetOffer(Guid id)
        {
            OfferTableEntity offer = (from e in this.tableContext.CreateQuery<OfferTableEntity>(OfferTableName)
                           where e.PartitionKey == id.ToString()
                           select e).FirstOrDefault();

            Offer off = null;
            if (offer != null)
            {
                off = OfferTableEntity.RetreiveOfferFromString(offer.OfferAsString);
            }
            return off;
        }
    }

    class OfferTableEntity : TableServiceEntity
    {
        public string OfferAsString { get; set; }

        public OfferTableEntity()
        {
        }

        public OfferTableEntity(Offer offer)
        {
            TextWriter writer = new StringWriter();
            XmlSerializer ser = new XmlSerializer(typeof(Offer));
            ser.Serialize(writer, offer);
            OfferAsString = writer.ToString();

        }

        public static Offer RetreiveOfferFromString(string data)
        {
            Offer offer = null;
            if (!string.IsNullOrEmpty(data))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Offer));
                offer = ser.Deserialize(new StringReader(data)) as Offer;
            }

            return offer;
        }
    }
}
