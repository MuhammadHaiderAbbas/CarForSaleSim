using CarSales.Foundation;
using NUnit.Framework;

namespace CarSales.Tests
{
    public class LedgerTests
    {
        [SetUp] public void SetUp() => EventBus.Clear();

        [Test]
        public void Balance_StartsAtOpeningBalance()
        {
            var l = new Ledger();
            l.Reset(50000, 1);
            Assert.AreEqual(50000, l.Balance);
            Assert.AreEqual(0, l.Entries.Count);
        }

        [Test]
        public void Post_ChangesBalance_AndAssignsIncreasingIds()
        {
            var l = new Ledger();
            l.Reset(1000, 1);
            var a = l.Post(-300, TxCategory.VehiclePurchase, 1, 9f, "Car");
            var b = l.Post(500, TxCategory.VehicleSale, 1, 10f, "Car");
            Assert.AreEqual(1200, l.Balance);
            Assert.Less(a.id, b.id);
            Assert.AreEqual(TxCategory.VehiclePurchase, a.category);
        }

        [Test]
        public void Post_ZeroAmount_IsIgnored()
        {
            var l = new Ledger();
            Assert.IsNull(l.Post(0, TxCategory.Fuel, 1, 9f));
            Assert.AreEqual(0, l.Entries.Count);
        }

        [Test]
        public void Post_PublishesTransactionPosted()
        {
            var l = new Ledger();
            l.Reset(100, 1);
            int seenBalance = -1;
            EventBus.Subscribe<TransactionPosted>(e => seenBalance = e.BalanceAfter);
            l.Post(-40, TxCategory.Fuel, 1, 9f);
            Assert.AreEqual(60, seenBalance);
        }

        [Test]
        public void Summarize_SeparatesOperatingFromFinancing()
        {
            var l = new Ledger();
            l.Reset(0, 1);
            l.Post(20000, TxCategory.LoanDrawdown, 1, 9f);
            l.Post(-15000, TxCategory.VehiclePurchase, 1, 10f);
            l.Post(18000, TxCategory.VehicleSale, 1, 15f);
            l.Post(-500, TxCategory.Rent, 1, 23f);
            l.Post(-1000, TxCategory.LoanRepayment, 1, 23f);
            l.Post(-999, TxCategory.Rent, 2, 23f);   // other day

            var s = l.Summarize(1, 1);
            Assert.AreEqual(18000, s.income);
            Assert.AreEqual(15500, s.expenses);
            Assert.AreEqual(2500, s.Net);
            Assert.AreEqual(20000, s.financingIn);
            Assert.AreEqual(1000, s.financingOut);
            Assert.AreEqual(-500, s.byCategory[(int)TxCategory.Rent]);
            Assert.AreEqual(TxCategory.LoanDrawdown, s.Lines()[0].Key, "biggest movement first");
        }

        [Test]
        public void Reconcile_PostsAdjustmentOnlyWhenNeeded()
        {
            var l = new Ledger();
            l.Reset(1000, 1);
            Assert.AreEqual(0, l.Reconcile(1000, 1, 8f));
            Assert.AreEqual(250, l.Reconcile(1250, 1, 8f));
            Assert.AreEqual(1250, l.Balance);
            Assert.AreEqual(TxCategory.Adjustment, l.Entries[0].category);
        }

        [Test]
        public void Compact_KeepsBalance_AndLifetimeTotals()
        {
            var l = new Ledger();
            l.Reset(1000, 1);
            for (int day = 1; day <= 10; day++) l.Post(-10, TxCategory.Fuel, day, 12f);
            int removed = l.Compact(10, 3);       // keep days 7..10
            Assert.AreEqual(6, removed);
            Assert.AreEqual(4, l.Entries.Count);
            Assert.AreEqual(900, l.Balance);
            Assert.AreEqual(940, l.OpeningBalance);
            Assert.AreEqual(100, l.LifetimeOut(TxCategory.Fuel));
        }

        [Test]
        public void Load_RebuildsBalance_AndContinuesIds()
        {
            var l = new Ledger();
            l.Reset(500, 1);
            l.Post(-100, TxCategory.Repair, 1, 9f);
            var last = l.Post(300, TxCategory.VehicleSale, 1, 10f);

            var copy = new Ledger(l.State);
            Assert.AreEqual(700, copy.Balance);
            Assert.Greater(copy.Post(-1, TxCategory.Other, 1, 11f).id, last.id);
        }

        [Test]
        public void Load_ToleratesOldStateWithFewerCategories()
        {
            var old = new LedgerState { openingBalance = 10, lifetimeIn = new long[2], lifetimeOut = null };
            var l = new Ledger(old);
            l.Post(-5, TxCategory.Adjustment, 1, 1f);
            Assert.AreEqual(5, l.Balance);
            Assert.AreEqual(5, l.LifetimeOut(TxCategory.Adjustment));
        }

        [Test]
        public void Recent_ReturnsNewestFirst()
        {
            var l = new Ledger();
            l.Post(1, TxCategory.Other, 1, 1f, "a");
            l.Post(2, TxCategory.Other, 1, 2f, "b");
            l.Post(3, TxCategory.Other, 1, 3f, "c");
            var r = l.Recent(2);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual("c", r[0].memo);
            Assert.AreEqual("b", r[1].memo);
        }

        [Test]
        public void EveryCategory_HasADisplayName()
        {
            foreach (TxCategory c in System.Enum.GetValues(typeof(TxCategory)))
                Assert.IsFalse(string.IsNullOrEmpty(TxCategories.DisplayName(c)), c.ToString());
        }
    }
}
