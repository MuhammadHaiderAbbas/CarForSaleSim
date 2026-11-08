using System;
using UnityEngine;

namespace CarSales
{
    public class EconomyManager : MonoBehaviour
    {
        [SerializeField] int startingMoney = 50000;
        [SerializeField] float startingReputation = 50f;

        public int Money { get; private set; }          // int, never float, for money
        public float Reputation { get; private set; }

        public event Action<int, int> OnMoneyChanged;    // (newMoney, delta)
        public event Action<float> OnReputationChanged;

        void Awake()
        {
            CarSalesServices.Economy = this;
            Money = startingMoney;
            Reputation = startingReputation;
        }

        public bool CanAfford(int amount) => Money >= amount;

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            Money += amount;
            OnMoneyChanged?.Invoke(Money, amount);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Money < amount) return false;
            Money -= amount;
            OnMoneyChanged?.Invoke(Money, -amount);
            return true;
        }

        /// <summary>Rent and bills: allowed to go negative.</summary>
        public void ForceSpend(int amount)
        {
            Money -= amount;
            OnMoneyChanged?.Invoke(Money, -amount);
        }

        public void ChangeReputation(float delta)
        {
            Reputation = Mathf.Clamp(Reputation + delta, 0f, 100f);
            OnReputationChanged?.Invoke(Reputation);
        }

        public void ResetToStart() => SetState(startingMoney, startingReputation);

        public void SetState(int money, float reputation)
        {
            Money = money;
            Reputation = reputation;
            OnMoneyChanged?.Invoke(Money, 0);
            OnReputationChanged?.Invoke(Reputation);
        }
    }
}
