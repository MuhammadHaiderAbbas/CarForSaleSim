using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace CarSales
{
    /// <summary>Spawns customers at its dealership automatically while that dealership is open.</summary>
    [RequireComponent(typeof(DealershipManager))]
    public class CustomerSpawner : MonoBehaviour
    {
        public static readonly List<CustomerSpawner> All = new List<CustomerSpawner>();

        [SerializeField] CustomerAI customerPrefab;
        [SerializeField] Vector2 spawnInterval = new Vector2(15f, 30f);
        [SerializeField] int maxCustomers = 4;
        [SerializeField] string[] customerNames =
            { "Alex", "Sam", "Jordan", "Taylor", "Chris", "Morgan", "Riley", "Jamie", "Casey", "Avery", "Drew", "Robin" };

        public DealershipManager Dealer { get; private set; }
        readonly List<CustomerAI> active = new List<CustomerAI>();
        Coroutine loop;
        bool warnedNoCars;

        void Awake()
        {
            Dealer = GetComponent<DealershipManager>();
            if (!All.Contains(this)) All.Add(this);
            if (Dealer.isMain || !CarSalesServices.Spawner) CarSalesServices.Spawner = this;
        }

        void OnDestroy() => All.Remove(this);

        void Update()
        {
            bool open = Dealer.IsOpenNow;
            if (open && loop == null) loop = StartCoroutine(SpawnLoop());
            else if (!open && loop != null) StopSpawning(true);
        }

        public void StartSpawning() { if (loop == null) loop = StartCoroutine(SpawnLoop()); }

        public void StopSpawning(bool sendEveryoneHome)
        {
            if (loop != null) { StopCoroutine(loop); loop = null; }
            if (sendEveryoneHome) foreach (var c in active.ToArray()) if (c) c.Leave();
        }

        public static void StopAll(bool sendHome) { foreach (var s in All) s.StopSpawning(sendHome); }

        public void Unregister(CustomerAI c) => active.Remove(c);

        IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(2f);
            while (true)
            {
                TrySpawn();
                float mult = CarSalesServices.Upgrades ? CarSalesServices.Upgrades.SpawnIntervalMultiplier : 1f;
                yield return new WaitForSeconds(Random.Range(spawnInterval.x, spawnInterval.y) * mult);
            }
        }

        void TrySpawn()
        {
            active.RemoveAll(c => c == null);
            int max = maxCustomers + (CarSalesServices.Upgrades ? CarSalesServices.Upgrades.ExtraCustomers : 0);
            if (active.Count >= max) return;
            if (Dealer.GetListedCars().Count == 0)
            {
                if (!warnedNoCars && Dealer.isMain)
                {
                    warnedNoCars = true;
                    CarSalesServices.UI.Notify($"No cars listed at {Dealer.displayName} - customers won't come!");
                }
                return;
            }
            warnedNoCars = false;
            var points = Dealer.customerSpawnPoints;
            if (points == null || points.Length == 0) return;

            var point = points[Random.Range(0, points.Length)];
            if (!NavMesh.SamplePosition(point.position, out var hit, 3f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[CarSales] Customer spawn point at {Dealer.displayName} is not on the NavMesh.");
                return;
            }
            var customer = Instantiate(customerPrefab, hit.position, point.rotation);
            customer.Init(GenerateProfile(), this);
            active.Add(customer);
            CarSalesServices.PlaySfx(Sfx.DoorChime, 0.5f);
        }

        CustomerProfile GenerateProfile()
        {
            float rep = CarSalesServices.Economy.Reputation / 100f;
            float budgetMult = (CarSalesServices.Upgrades ? CarSalesServices.Upgrades.BudgetMultiplier : 1f) * Dealer.budgetMultiplier;
            return new CustomerProfile
            {
                customerName = customerNames[Random.Range(0, customerNames.Length)],
                budget = Mathf.RoundToInt(Random.Range(8000, 60000) * (0.8f + rep * 0.6f) * budgetMult),
                preferredCategory = (CarCategory)Random.Range(0, Enum.GetValues(typeof(CarCategory)).Length),
                patience = Random.Range(1, 5),
                greed = Random.Range(0.1f, 0.9f),
                minCondition = Random.Range(30f, 80f)
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();
    }
}
