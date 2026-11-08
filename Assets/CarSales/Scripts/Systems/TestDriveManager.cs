using UnityEngine;
using UnityEngine.InputSystem;

namespace CarSales
{
    /// <summary>Lets the player drive one of their cars for a limited time.</summary>
    public class TestDriveManager : MonoBehaviour
    {
        [SerializeField] InputActionReference moveAction, brakeAction, exitAction;
        [SerializeField] float duration = 60f;
        [SerializeField] Vector3 cameraOffset = new Vector3(0f, 2.8f, -7f);
        [SerializeField] float cameraPitch = 12f;

        public bool IsDriving { get; private set; }
        public float TimeLeft { get; private set; }
        public float SpeedKmh => car && vehicleController ? Mathf.Abs(vehicleController.Speed) * 3.6f : 0f;
        /// <summary>True while driving and on the frame the drive ended (so the same E press doesn't also interact).</summary>
        public bool BlocksInteraction => IsDriving || Time.frameCount == endFrame;

        CarDisplay car;
        GameObject vehicle;
        ArcadeCarController vehicleController;
        Transform cam, camParent;
        Vector3 camLocalPos;
        Quaternion camLocalRot;
        MenuInputBlocker blocker;
        InputAction move, brake, exit;
        int endFrame = -1, crashes;

        void Awake() => CarSalesServices.TestDrive = this;

        void OnEnable()
        {
            move = CarSalesInput.Resolve(moveAction, "Move");
            brake = CarSalesInput.Resolve(brakeAction, "Jump");
            exit = CarSalesInput.Resolve(exitAction, "Interact");
            if (exit != null) exit.performed += OnExit;
        }

        void OnDisable() { if (exit != null) exit.performed -= OnExit; }

        public bool CanStart(CarDisplay target, out string reason)
        {
            reason = "";
            if (IsDriving) { reason = "Already on a test drive."; return false; }
            if (target == null || target.Definition == null || !target.Definition.displayPrefab) { reason = "This car can't be driven."; return false; }
            if (!Camera.main) { reason = "No main camera found."; return false; }
            return true;
        }

        public void StartDrive(CarDisplay target)
        {
            if (!CanStart(target, out string reason)) { CarSalesServices.UI.Error(reason); return; }
            CarSalesServices.UI.Close();
            car = target;
            crashes = 0;

            // Build the drivable vehicle
            vehicle = new GameObject("TestDrive_" + car.Definition.displayName);
            vehicle.transform.SetPositionAndRotation(car.transform.position + Vector3.up * 0.2f, car.transform.rotation);
            var cc = vehicle.AddComponent<CharacterController>();
            cc.radius = 1.0f;
            cc.height = 2.0f;
            cc.center = new Vector3(0f, 1.0f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 35f;
            var model = Instantiate(car.Definition.displayPrefab, vehicle.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var visual = model.GetComponent<CarVisual>();
            if (visual) visual.Apply(car.Data.paintColor, 1f - car.Data.cleanliness / 100f);

            vehicleController = vehicle.AddComponent<ArcadeCarController>();
            vehicleController.MoveAction = move;
            vehicleController.BrakeAction = brake;
            vehicleController.OnCrash += HandleCrash;

            car.gameObject.SetActive(false);

            // Camera follows the car
            cam = Camera.main.transform;
            camParent = cam.parent;
            camLocalPos = cam.localPosition;
            camLocalRot = cam.localRotation;
            cam.SetParent(vehicle.transform, false);
            cam.localPosition = cameraOffset;
            cam.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

            // Freeze the walking player
            blocker = FindFirstObjectByType<MenuInputBlocker>();
            if (blocker) blocker.SetBlocked(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            IsDriving = true;
            TimeLeft = duration;
            if (CarSalesServices.Progress) CarSalesServices.Progress.Stats.testDrives++;
            CarSalesServices.UI.Notify("Test drive started! W/S drive, A/D steer, Space brake, E to return the car.");
        }

        void Update()
        {
            if (!IsDriving) return;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) EndDrive();
        }

        void OnExit(InputAction.CallbackContext _)
        {
            if (IsDriving && !CarSalesServices.UI.IsMenuOpen) EndDrive();
        }

        void HandleCrash(float impactSpeed)
        {
            crashes++;
            float damage = Mathf.Round(Mathf.Clamp(impactSpeed * 0.4f, 2f, 12f));
            CarParts.Damage(car.Data, damage);
            if (car.Data.state == CarState.Ready && car.Data.condition < CarValueCalculator.RepairThreshold)
                car.Data.state = CarState.NeedsRepair;
            CarSalesServices.PlaySfx(Sfx.Crash);
            CarSalesServices.UI.Error($"CRASH! Body damaged. Condition now {car.Data.condition:0}%");
        }

        /// <summary>Returns the car to its parking spot and gives control back to the player.</summary>
        public void EndDrive()
        {
            if (!IsDriving) return;
            IsDriving = false;
            endFrame = Time.frameCount;

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
                car.Data.mileage += Random.Range(3, 15);
                car.Data.cleanliness = Mathf.Max(0f, car.Data.cleanliness - 4f);
                car.Refresh();
                CarSalesServices.Inventory.NotifyChanged();
            }
            if (blocker) blocker.SetBlocked(false);

            CarSalesServices.UI.Notify(crashes == 0 ? "Car returned safely to its spot." : $"Car returned with {crashes} crash(es). Check its condition!");
            car = null;
        }
    }
}
