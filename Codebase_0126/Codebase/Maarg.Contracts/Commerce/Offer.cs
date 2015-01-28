using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts.Commerce
{
    /// <summary>
    /// An offer represents a service that can be purchased
    /// One service can have multiple offers, each with their own rate plan
    /// </summary>
    [Serializable]
    public class Offer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ServiceName { get; set; }
        public BillingModel BillingModel { get; set; }
        public BillingFrequency BillingFrequency { get; set; }
        public List<RateSchedule> RateSchedules 
        {
            get { return rateSchedules; }
        }

        List<RateSchedule> rateSchedules = new List<RateSchedule>();
    }

    /// <summary>
    /// RateSchedules defines the rates (fees) for an offer and the time period when the rates are valid
    /// Currently, the time period is being defined in terms of billing cycle offset, ie one can specify
    /// rateX applied for first 3 billing cycles, while rateY for 4 and onwards
    /// There are 2 types of billing
    /// 
    ///     1. Seat based
    ///     2. Usage based
    /// </summary>
    public class RateSchedule
    {
        public const int DefaultRateScheduleId = -1;

        public int StartBillingCycle { get; set; }
        public int EndBillingCycle { get; set; }


        public List<SeatCountRateTier> SeatCountRateTiers 
        {
            get { return seatCountRateTiers; }
        }
        List<SeatCountRateTier> seatCountRateTiers = new List<SeatCountRateTier>();

        public List<ConsumptionRateTier> ConsumptionRateTiers 
        {
            get { return consumptionRateTiers; }
        }
        List<ConsumptionRateTier> consumptionRateTiers = new List<ConsumptionRateTier>();
    }

    /// <summary>
    /// Seat based rate tier
    /// You can break seat count into multiple tiers like
    /// Count 1-5 => rate X
    /// Count 6-10 => rate Y
    /// </summary>
    public class SeatCountRateTier
    {
        public int StartSeatCount { get; set; }
        public int EndSeatCount { get; set; }
        public double Rate { get; set; }
    }

    /// <summary>
    /// Consumption Based rate Tier
    /// IncludedQuantity - how much is already included as free in the offer, because the offer already has a commitment fees
    /// Rate for overage, ie (Consumed-Included) units
    /// </summary>
    public class ConsumptionRateTier
    {
        public double IncludedQuantity { get; set; }
        public string ResourceId { get; set; }
        public double Rate { get; set; }
    }

    public class ConsumptionResource
    {
        string FriendlyId { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}
