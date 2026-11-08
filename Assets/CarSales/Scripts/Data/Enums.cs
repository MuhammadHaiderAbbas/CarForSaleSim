namespace CarSales
{
    public enum CarCategory { Hatchback, Sedan, SUV, Sports, Pickup, Luxury }
    public enum CarState { InTransit, NeedsRepair, Ready, Listed, Sold }
    public enum CustomerState { Arriving, Browsing, Interested, Negotiating, Leaving }
    public enum NegotiationResult { Accepted, Countered, WalkedAway }
}

namespace CarSales
{
    public enum UpgradeType { LotExpansion, Marketing, Mechanic, Detailing, Showroom }
    public enum Sfx { Click, Coin, Purchase, Error, Notify, DoorChime, Success, Crash }
}
