using UnityEngine;

namespace CarSales
{
    /// <summary>Pure haggling logic (no UI). The customer's max price is hidden from the player.</summary>
    public class NegotiationSession
    {
        public CustomerAI Customer { get; }
        public CarDisplay Car { get; }
        public int MarketValue { get; }
        public int CustomerOffer { get; private set; }
        public int FinalPrice { get; private set; }

        readonly int maxWillingToPay;
        readonly float greed;
        readonly int startPatience;
        int patience;

        /// <summary>1 = calm, 0 = about to walk away.</summary>
        public float PatienceRatio => startPatience > 0 ? Mathf.Clamp01(patience / (float)startPatience) : 0f;

        public NegotiationSession(CustomerAI customer, CarDisplay car)
        {
            Customer = customer;
            Car = car;
            var p = customer.Profile;
            greed = p.greed;
            patience = p.patience;
            startPatience = Mathf.Max(1, p.patience);
            MarketValue = CarValueCalculator.MarketValue(car.Data);
            maxWillingToPay = Mathf.Min(p.budget, Mathf.RoundToInt(MarketValue * Mathf.Lerp(1.25f, 0.95f, greed)));
            CustomerOffer = Mathf.Min(maxWillingToPay, Mathf.RoundToInt(MarketValue * Mathf.Lerp(0.85f, 0.65f, greed)));
        }

        public void AcceptCustomerOffer() => FinalPrice = CustomerOffer;

        public NegotiationResult Propose(int playerOffer, out string reply)
        {
            if (playerOffer <= CustomerOffer)
            {
                FinalPrice = playerOffer;
                reply = "Deal! Pleasure doing business.";
                return NegotiationResult.Accepted;
            }

            if (playerOffer <= maxWillingToPay)
            {
                float gap = maxWillingToPay - CustomerOffer + 1f;
                float acceptChance = 1f - (playerOffer - CustomerOffer) / gap;
                if (Random.value < acceptChance)
                {
                    FinalPrice = playerOffer;
                    reply = "Alright... you've got a deal.";
                    return NegotiationResult.Accepted;
                }
                patience -= 1;
                CustomerOffer += Mathf.RoundToInt((playerOffer - CustomerOffer) * Mathf.Lerp(0.6f, 0.25f, greed));
                CustomerOffer = Mathf.Min(CustomerOffer, maxWillingToPay);
                reply = $"Hmm, how about ${CustomerOffer:N0}?";
            }
            else
            {
                patience -= 2;
                CustomerOffer += Mathf.RoundToInt((maxWillingToPay - CustomerOffer) * 0.15f);
                reply = $"That's way too much! I could do ${CustomerOffer:N0}.";
            }

            if (patience <= 0)
            {
                reply = "Forget it, I'm going elsewhere.";
                return NegotiationResult.WalkedAway;
            }
            return NegotiationResult.Countered;
        }
    }
}
