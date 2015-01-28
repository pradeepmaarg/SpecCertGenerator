using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.Contracts.Commerce
{
    /// <summary>
    /// This represents a trading parner order. Trading partner is a customer who owns commerce relationship with Maarg
    /// An order is a collection of subscriptions, each has its own lifecycle determined by properties of Offer
    /// </summary>
    public class Order : TableServiceEntity
    {
        public Guid Id { get; set; }

        //Making a simplification to include Subscription in the Order itself
        //Main assumption: Order contains a single subscription
        public int SeatCount { get; set; } //number of units being purchased
        public Guid OfferId { get; set; }
        public int State { get; set; }

        //timestamps
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }

        //who is purchasing this order
        public string TenantId { get; set; }
        public string TradingPartnerId { get; set; }
        public string UserId { get; set; }

        //constant declarations for state
        public const int Active = 1;
        public const int Grace = 2;
        public const int Suspend = 3;
        public const int Lockout = 4;
        public const int Deprov = 5;
    }

    /// <summary>
    /// This represents an actual offer that a TradingPartner is subscribing to. Eg. EDIActive, 277CA, OrderToCash
    /// Offer defines the service, with terms and rate plans
    /// Each subscription has a lifecycle in which it goes through different states, represented by SubscriptionState
    /// </summary>
    public class Subscription
    {
        public Guid Id { get; set; }
        public Order Order { get; set; }
        public int SeatCount { get; set; } //number of units being purchased
        public Guid OfferId { get; set; }
        public SubscriptionState State { get; set; }
    }

    /// <summary>
    /// This represents the various states involved in the lifecycle of a subscription
    /// </summary>
    public enum SubscriptionState : int
    {
        Active,
        Grace,
        Suspend,
        Lockout,
        Deprov
    }
}
