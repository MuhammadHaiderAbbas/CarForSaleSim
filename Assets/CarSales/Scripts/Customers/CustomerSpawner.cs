using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace CarSales
{
    /// <summary>Spawns customers while the shop is open. Lives on Dealership.prefab.</summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] CustomerAI customerPrefab;
        [SerializeField] Vector2 spawnInterval = new Vector2(15f, 30f);
        [SerializeField] int maxCustomers = 4;
        [SerializeField] string[] customerNames =
            { "Alex", "Sam", "Jordan", "Taylor", "Chris", "Morgan", "Riley", "Jamie", "Casey", "Avery", "Drew", "Robin" };

        readonly List<CustomerAI> active = new List<CustomerAI>();
        Coroutine loop;

        void Awake() => CarSalesServices.Spawner = this;

        public void StartSpawning() { if (loop == null) loop = StartCoroutine(SpawnLoop()); }

        public void StopSpawning(bool sendEveryoneHome)
        {
            if (loop != null) { StopCoroutine(loop); loop = null; }
            if (sendEveryoneHome) foreach (var c in active.ToArray()) if (c) c.Leave();
        }

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
            var dealer = CarSalesServices.Dealership;
            active.RemoveAll(c => c == null);
            int max = maxCustomers + (CarSalesServices.Upgrades ? CarSalesServices.Upgrades.ExtraCustomers : 0);
            if (active.Count >= max) return;
            if (dealer.GetListedCars().Count == 0) { CarSalesServices.UI.Notify("No cars listed for sale - customers won't come!"); return; }
            if (dealer.customerSpawnPoints == null || dealer.customerSpawnPoints.Length == 0) return;

            var point = dealer.customerSpawnPoints[Random.Range(0, dealer.customerSpawnPoints.Length)];
            if (!NavMesh.SamplePosition(point.position, out var hit, 3f, NavMesh.AllAreas))
            {
                Debug.LogWarning("[CarSales] Customer spawn point is not on the NavMesh.");
                return;
            }

            var customer = Instantiate(customerPrefab, hit.position, point.rotation);
            customer.Init(GenerateProfile(), this);
            active.Add(customer);
            CarSalesServices.PlaySfx(Sfx.DoorChime, 0.7f);
        }

        CustomerProfile GenerateProfile()
        {
            float rep = CarSalesServices.Economy.Reputation / 100f;
            float budgetMult = CarSalesServices.Upgrades ? CarSalesServices.Upgrades.BudgetMultiplier : 1f;
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
    }
}
