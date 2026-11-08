using UnityEngine;
using UnityEngine.InputSystem;

namespace CarSales
{
    /// <summary>
    /// Driving any car you own. F = get in / get out (the car stays where you leave it).
    /// While driving, E does the context action: park in a free dealership spot, refuel at a fuel station,
    /// wash at the car wash, repair at the workshop. Driving uses fuel and adds mileage.
    /// (Named TestDriveManager for compatibility with older versions.)
    /// </summary>
    public class TestDriveManager : MonoBehaviour
    {
        [SerializeField] InputActionReference moveAction, brakeAction, exitAction, vehicleAction;
        [SerializeField] Vector3 cameraOffset = new Vector3(0f, 2.8f, -7f);
        [SerializeField] float cameraPitch = 12f;
        [SerializeField] float fuelPercentPerKm = 4f;      // 100% tank = 25 km

        public bool IsDriving { get; private set; }
        public CarDisplay CurrentCar => car;
        public float SpeedKmh => car && vehicleController ? Mathf.Abs(vehicleController.Speed) * 3.6f : 0f;
        public float Fuel => car ? car.Data.fuel : 0f;
        public string ContextPrompt { get; private set; } = "";
        public bool BlocksInteraction => IsDriving || Time.frameCount == endFrame;

        enum Context { None, Park, Refuel, Wash, Repair }
        Context context;
        DealershipManager contextDealer;
        ParkingSpot contextSpot;

        CarDisplay car;
        GameObject vehicle;
        ArcadeCarController vehicleController;
        Transform cam, camParent;
        Vector3 camLocalPos, lastPos;
        Quaternion camLocalRot;
        MenuInputBlocker blocker;
        InputAction move, brake, contextKey, vehicleKey;
        int endFrame = -1;
        float distanceAccum, contextTimer;
        bool warnedEmpty;

        void Awake() => CarSalesServices.TestDrive = this;

        void OnEnable()
        {
            move = CarSalesInput.Resolve(moveAction, "Move");
            brake = CarSalesInput.Resolve(brakeAction, "Jump");
            contextKey = CarSalesInput.Resolve(exitAction, "Interact");
            vehicleKey = CarSalesInput.Resolve(vehicleAction, "Vehicle");
            if (contextKey != null) contextKey.performed += OnContextKey;
            if (vehicleKey != null) vehicleKey.performed += OnVehicleKey;
        }

        void OnDisable()
        {
            if (contextKey != null) contextKey.performed -= OnContextKey;
            if (vehicleKey != null) vehicleKey.performed -= OnVehicleKey;
        }

        void Start() { if (CarSalesServices.UI) CarSalesServices.UI.OnMenuStateChanged += HandleMenu; }
        void OnDestroy() { if (CarSalesServices.UI) CarSalesServices.UI.OnMenuStateChanged -= HandleMenu; }
        void HandleMenu(bool open) { if (vehicleController) vehicleController.enabled = !open; }

        // ------------------------------------------------------------------ INPUT
        void OnVehicleKey(InputAction.CallbackContext _)
        {
            if (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen) return;
            if (IsDriving) { EndDrive(); return; }
            var interactor = FindFirstObjectByType<PlayerInteractor>();
            var target = interactor ? interactor.CurrentTarget as CarDisplay : null;
            if (target && !target.IsForSaleListing) StartDrive(target);
        }

        void OnContextKey(InputAction.CallbackContext _)
        {
            if (!IsDriving || (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen)) return;
            var c = car;
            switch (context)
            {
                case Context.Park:
                    var dealer = contextDealer; var spot = contextSpot;
                    EndDrive();
                    if (c && dealer && spot != null && (spot.IsFree || spot.Occupant == c))
                    {
                        CarSalesServices.Vehicles.PlaceAtSpot(c, dealer, spot);
                        CarSalesServices.UI.Success("Parked", $"{c.Definition.displayName} is parked at {dealer.displayName}, spot #{spot.index + 1}.");
                    }
                    break;
                case Context.Refuel: CarSalesServices.Vehicles.Refuel(c); warnedEmpty = false; break;
                case Context.Wash: EndDrive(); CarSalesServices.UI.carWash.Show(c); break;
                case Context.Repair: EndDrive(); CarSalesServices.UI.garage.Show(c); break;
            }
        }

        // ------------------------------------------------------------------ DRIVING
        public bool CanStart(CarDisplay target, out string reason)
        {
            reason = "";
            if (IsDriving) { reason = "You're already driving."; return false; }
            if (target == null || target.Definition == null || !target.Definition.displayPrefab) { reason = "This car can't be driven."; return false; }
            if (target.IsForSaleListing) { reason = "Buy the car first."; return false; }
            if (!Camera.main) { reason = "No main camera found."; return false; }
            if (target.Data.fuel <= 0.5f) { reason = "The tank is empty! Tow it to a fuel station (phone, P)."; return false; }
            return true;
        }

        public void StartDrive(CarDisplay target)
        {
            if (!CanStart(target, out string reason)) { CarSalesServices.UI.Error(reason); return; }
            CarSalesServices.UI.Close();
            car = target;
            warnedEmpty = false;

            vehicle = new GameObject("Driving_" + car.Definition.displayName) { layer = 2 };   // Ignore Raycast
            vehicle.transform.SetPositionAndRotation(car.transform.position + Vector3.up * 0.2f, car.transform.rotation);
            var cc = vehicle.AddComponent<CharacterController>();
            cc.radius = 1.0f;
            cc.height = 2.0f;
            cc.center = new Vector3(0f, 1.0f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 35f;
            var model = Instantiate(car.Definition.displayPrefab, vehicle.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var visual = model.GetComponent<CarVisual>();
            if (visual) visual.Apply(car.Data.paintColor, 1f - car.Data.cleanliness / 100f);
            MinimapMarker.Attach(vehicle, new Color(0.2f, 0.95f, 0.95f), 3.5f);
            var lightGo = new GameObject("Headlights");
            lightGo.transform.SetParent(vehicle.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.8f, 2.4f);
            lightGo.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            var headlights = lightGo.AddComponent<Light>();
            headlights.type = LightType.Spot;
            headlights.range = 35f;
            headlights.spotAngle = 70f;
            headlights.intensity = 4f;
            headlights.color = new Color(1f, 0.95f, 0.85f);

            vehicleController = vehicle.AddComponent<ArcadeCarController>();
            vehicleController.MoveAction = move;
            vehicleController.BrakeAction = brake;
            vehicleController.OnCrash += HandleCrash;
            car.gameObject.SetActive(false);

            cam = Camera.main.transform;
            camParent = cam.parent;
            camLocalPos = cam.localPosition;
            camLocalRot = cam.localRotation;
            cam.SetParent(vehicle.transform, false);
            cam.localPosition = cameraOffset;
            cam.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

            blocker = FindFirstObjectByType<MenuInputBlocker>();
            if (blocker) blocker.SetBlocked(true);
            var player = FindFirstObjectByType<PlayerInteractor>();
            if (player) foreach (var r in player.GetComponentsInChildren<Renderer>()) r.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            IsDriving = true;
            lastPos = vehicle.transform.position;
            distanceAccum = 0f;
            if (CarSalesServices.Progress) CarSalesServices.Progress.Stats.testDrives++;
            CarSalesServices.UI.Notify("W/S drive, A/D steer, Space brake. F = get out. E = park / refuel / wash / repair when prompted.");
        }

        void Update()
        {
            if (!IsDriving || !vehicle) return;

            // Fuel & mileage
            Vector3 p = vehicle.transform.position;
            float meters = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(lastPos.x, lastPos.z));
            lastPos = p;
            if (meters > 0f && meters < 20f)
            {
                car.Data.fuel = Mathf.Max(0f, car.Data.fuel - meters / 1000f * fuelPercentPerKm);
                distanceAccum += meters;
                if (distanceAccum >= 1000f) { car.Data.mileage += 1; distanceAccum -= 1000f; }
            }
            vehicleController.OutOfFuel = car.Data.fuel <= 0f;
            if (vehicleController.OutOfFuel && !warnedEmpty)
            {
                warnedEmpty = true;
                CarSalesServices.UI.Error("Out of fuel! Get out (F) and call a tow truck from your phone (P).");
            }

            contextTimer -= Time.deltaTime;
            if (contextTimer <= 0f) { contextTimer = 0.2f; UpdateContext(p); }
        }

        void UpdateContext(Vector3 pos)
        {
            context = Context.None;
            contextDealer = null;
            contextSpot = null;
            var zone = ServiceZone.At(pos);
            if (zone)
            {
                switch (zone.type)
                {
                    case ServiceType.FuelStation:
                        context = Context.Refuel;
                        ContextPrompt = car.Data.fuel >= 99.5f ? "Tank is full" : $"[E] Refuel ({UIFactory.Money(CarSalesServices.Vehicles.RefuelCost(car.Data))})";
                        if (car.Data.fuel >= 99.5f) context = Context.None;
                        return;
                    case ServiceType.CarWash:
                        context = Context.Wash;
                        ContextPrompt = $"[E] Car wash ({UIFactory.Money(CarValueCalculator.CleanCost())})";
                        return;
                    case ServiceType.Workshop:
                        context = Context.Repair;
                        ContextPrompt = "[E] Leave the car with the mechanic (repairs)";
                        return;
                }
            }
            foreach (var d in DealershipManager.Owned)
            {
                var spot = d.NearestFreeSpot(pos, 4.5f);
                if (spot == null) continue;
                context = Context.Park;
                contextDealer = d;
                contextSpot = spot;
                ContextPrompt = $"[E] Park in {d.displayName} spot #{spot.index + 1}";
                return;
            }
            ContextPrompt = "";
        }

        void HandleCrash(float impactSpeed)
        {
            float damage = Mathf.Round(Mathf.Clamp(impactSpeed * 0.4f, 2f, 12f));
            CarParts.Damage(car.Data, damage);
            if (car.Data.state == CarState.Ready && car.Data.condition < CarValueCalculator.RepairThreshold)
                car.Data.state = CarState.NeedsRepair;
            CarSalesServices.PlaySfx(Sfx.Crash);
            CarSalesServices.UI.Error($"CRASH! Body damaged. Condition now {car.Data.condition:0}%");
        }

        /// <summary>Gets out: the car is parked exactly where it is, the player stands next to it.</summary>
        public void EndDrive()
        {
            if (!IsDriving) return;
            IsDriving = false;
            endFrame = Time.frameCount;
            ContextPrompt = "";

            Vector3 parkPos = vehicle ? vehicle.transform.position : car.transform.position;
            float yaw = vehicle ? vehicle.transform.eulerAngles.y : car.transform.eulerAngles.y;
            int groundMask = Physics.DefaultRaycastLayers & ~(1 << 20) & ~(1 << 2);
            if (Physics.Raycast(parkPos + Vector3.up * 2f, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
                parkPos.y = hit.point.y;
            Vector3 exitPos = parkPos - Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 2.3f;

            if (cam)
            {
                cam.SetParent(camParent, false);
                cam.localPosition = camLocalPos;
                cam.localRotation = camLocalRot;
            }
            if (vehicle) Destroy(vehicle);

            if (car)
            {
                car.gameObject.SetActive(true);
                CarSalesServices.Vehicles.PlaceInWorld(car, parkPos, yaw);
                CarSalesServices.Inventory.NotifyChanged();
            }
            var player = FindFirstObjectByType<PlayerInteractor>();
            if (player) foreach (var r in player.GetComponentsInChildren<Renderer>()) r.enabled = true;
            TowService.TeleportPlayer(exitPos, yaw);
            if (blocker) blocker.SetBlocked(false);
            car = null;
        }
    }
}
