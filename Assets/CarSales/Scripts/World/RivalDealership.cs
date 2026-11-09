using System.Collections.Generic;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 14e: a rival dealer's premises in town. Shows its stock on the lot and its hire fleet in the hire bays
    /// (from <see cref="RivalManager"/>), puts a salesperson behind the desk and hides its signs once it stops trading
    /// (you bought the premises). Cars you buy or hire are left where they stand or at the handover bay.
    /// </summary>
    public class RivalDealership : MonoBehaviour
    {
        public static readonly List<RivalDealership> All = new List<RivalDealership>();

        public string rivalId;
        public Transform[] lotSpots;
        public Transform[] fleetSpots;
        public Transform handover;
        [Tooltip("Your cars inside this zone can be sold or handed back without the collection fee.")]
        public ServiceZone yard;
        public Transform staffPoint;
        [Tooltip("Signs, flags and the desk: shown only while the rival trades here.")]
        public GameObject[] openOnly;

        readonly List<CarDisplay> shown = new List<CarDisplay>();
        bool staffSpawned;

        public RivalProfile Profile => RivalSim.Find(rivalId);
        public bool IsOpen => RivalManager.IsOpen(rivalId);

        public static RivalDealership For(string rivalId) => All.Find(r => r && r.rivalId == rivalId);

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        void Start()
        {
            if (CarSalesServices.Rivals) CarSalesServices.Rivals.OnChanged += Rebuild;
            if (CarSalesServices.Inventory) CarSalesServices.Inventory.OnInventoryChanged += Rebuild;   // your car parked on a spot
            Rebuild();
        }

        void OnDestroy()
        {
            if (CarSalesServices.Rivals) CarSalesServices.Rivals.OnChanged -= Rebuild;
            if (CarSalesServices.Inventory) CarSalesServices.Inventory.OnInventoryChanged -= Rebuild;
        }

        public void Rebuild()
        {
            foreach (var d in shown) if (d) Destroy(d.gameObject);
            shown.Clear();
            bool open = IsOpen;
            if (openOnly != null) foreach (var go in openOnly) if (go) go.SetActive(open);
            if (open && !staffSpawned && staffPoint)
            {
                staffSpawned = true;
                CharacterSpawn.Attach(staffPoint, CarSales.Studio.CharacterUse.Staff);
            }
            if (staffPoint) staffPoint.gameObject.SetActive(open);
            var rivals = CarSalesServices.Rivals;
            if (!open || !rivals || !CarSalesServices.Vehicles || !CarSalesServices.Vehicles.DisplayPrefab) return;
            Place(rivals.Stock(rivalId), lotSpots, false);
            Place(rivals.Fleet(rivalId), fleetSpots, true);
        }

        void Place(IEnumerable<RivalCar> cars, Transform[] spots, bool fleet)
        {
            if (spots == null) return;
            var rivals = CarSalesServices.Rivals;
            int i = 0;
            foreach (var c in cars)
            {
                while (i < spots.Length && (!spots[i] || Occupied(spots[i].position))) i++;
                if (i >= spots.Length) break;
                var d = Instantiate(CarSalesServices.Vehicles.DisplayPrefab, spots[i].position, spots[i].rotation);
                d.InitializeForSale(c.car);
                d.MarkRival(rivalId, fleet, fleet ? $"HIRE {UIFactory.Money(rivals.DailyRate(c))}/day" : UIFactory.Money(rivals.PriceOf(c)));
                shown.Add(d);
                i++;
            }
        }

        static bool Occupied(Vector3 pos)
        {
            if (!CarSalesServices.Vehicles) return false;
            foreach (var car in CarSalesServices.Vehicles.All)
                if (car && car.gameObject.activeInHierarchy && Vector3.Distance(car.transform.position, pos) < 2.5f) return true;
            return false;
        }

        public CarDisplay DisplayOf(string instanceId) => shown.Find(d => d && d.Data != null && d.Data.instanceId == instanceId);

        /// <summary>Where a car you just bought or hired is left: on its spot, else a free handover bay.</summary>
        public (Vector3 pos, float yaw) HandoverFor(string instanceId)
        {
            var d = DisplayOf(instanceId);
            if (d) return (d.transform.position, d.transform.eulerAngles.y);
            var bay = yard ? yard.FreeBay() : null;
            var t = bay ? bay : handover ? handover : transform;
            return (t.position + Vector3.up * 0.05f, t.eulerAngles.y);
        }

        public bool InYard(CarDisplay car) =>
            car && (yard ? yard.Contains(car.transform.position) : Vector3.Distance(car.transform.position, transform.position) < 35f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();
    }
}
