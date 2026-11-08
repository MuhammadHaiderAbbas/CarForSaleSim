using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace CarSales
{
    /// <summary>Customer state machine: Browse -> Interested -> Negotiate -> Leave.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour, IInteractable
    {
        [SerializeField] Vector2 browseTime = new Vector2(3f, 6f);
        [SerializeField] float interestTimeout = 45f;
        [SerializeField] TMP_Text bubble;      // world-space text above head
        [SerializeField] Animator animator;    // optional, expects a float parameter "Speed"
        [SerializeField] Renderer bodyRenderer; // optional, gets a random shirt colour

        public CustomerProfile Profile { get; private set; }
        public CustomerState State { get; private set; } = CustomerState.Arriving;
        public CarDisplay TargetCar { get; private set; }

        NavMeshAgent agent;
        CustomerSpawner spawner;
        int maxCarsToBrowse;
        readonly HashSet<CarDisplay> seen = new HashSet<CarDisplay>();

        void Awake() => agent = GetComponent<NavMeshAgent>();

        public void Init(CustomerProfile profile, CustomerSpawner owner)
        {
            Profile = profile;
            spawner = owner;
            name = "Customer_" + profile.customerName;
            maxCarsToBrowse = Random.Range(2, 5);
            if (bodyRenderer)
            {
                var mpb = new MaterialPropertyBlock();
                var c = Random.ColorHSV(0f, 1f, 0.4f, 0.8f, 0.5f, 0.9f);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                bodyRenderer.SetPropertyBlock(mpb);
            }
            marker = MinimapMarker.Attach(gameObject, Color.white, 1.8f);
            StartCoroutine(BrowseRoutine());
        }

        MinimapMarker marker;

        void Update()
        {
            if (animator) animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        // ---------------- STATES ----------------
        IEnumerator BrowseRoutine()
        {
            State = CustomerState.Browsing;
            SetBubble("");
            while (seen.Count < maxCarsToBrowse)
            {
                TargetCar = PickCar();
                if (TargetCar == null) break;

                agent.SetDestination(TargetCar.BrowsePoint.position);
                yield return WaitUntilArrived();
                if (!IsTargetValid()) { TargetCar = null; continue; }

                Face(TargetCar.transform.position);
                SetBubble("...");
                yield return new WaitForSeconds(Random.Range(browseTime.x, browseTime.y));

                if (IsTargetValid() && WantsCar(TargetCar))
                {
                    StartCoroutine(InterestedRoutine());
                    yield break;
                }
                seen.Add(TargetCar);
                SetBubble("");
            }
            Leave();
        }

        IEnumerator InterestedRoutine()
        {
            State = CustomerState.Interested;
            agent.ResetPath();
            SetBubble("!");
            CarSalesServices.PlaySfx(Sfx.Notify);
            if (marker) marker.SetColor(UIFactory.Gold);
            CarSalesServices.UI.Toast(ToastType.Info, "Customer interested!",
                $"{Profile.customerName} wants to talk about the {TargetCar.Definition.displayName}. Look for the '!'.");

            float t = 0f;
            while (t < interestTimeout)
            {
                if (!IsTargetValid()) { seen.Add(TargetCar); StartCoroutine(BrowseRoutine()); yield break; }
                t += Time.deltaTime;
                yield return null;
            }
            CarSalesServices.Economy.ChangeReputation(-1f); // ignored customer
            SetBubble(":(");
            Leave();
        }

        public void Leave()
        {
            if (State == CustomerState.Leaving) return;
            StopAllCoroutines();
            StartCoroutine(LeaveRoutine());
        }

        IEnumerator LeaveRoutine()
        {
            State = CustomerState.Leaving;
            if (marker) marker.SetColor(new Color(1f, 1f, 1f, 0.35f));
            TargetCar = null;
            var exit = CarSalesServices.Dealership ? CarSalesServices.Dealership.exitPoint : null;
            if (exit)
            {
                agent.SetDestination(exit.position);
                yield return WaitUntilArrived(60f);
            }
            if (spawner) spawner.Unregister(this);
            Destroy(gameObject);
        }

        // ---------------- INTERACTION ----------------
        public bool CanInteract() => State == CustomerState.Interested && IsTargetValid();

        public string GetInteractionText() =>
            $"[E] Talk to {Profile.customerName} about the {TargetCar.Definition.displayName}";

        public void Interact(GameObject interactor)
        {
            StopAllCoroutines();
            State = CustomerState.Negotiating;
            Face(interactor.transform.position);
            CarSalesServices.UI.negotiation.Show(this);
        }

        public void EndNegotiation(bool bought)
        {
            SetBubble(bought ? ":)" : ">:(");
            Leave();
        }

        // ---------------- DECISIONS ----------------
        CarDisplay PickCar()
        {
            var options = CarSalesServices.Dealership.GetListedCars().Where(c => !seen.Contains(c)).ToList();
            if (options.Count == 0) return null;
            var preferred = options.Where(c => c.Definition.category == Profile.preferredCategory).ToList();
            if (preferred.Count > 0 && Random.value < 0.7f) options = preferred;
            return options[Random.Range(0, options.Count)];
        }

        bool WantsCar(CarDisplay car)
        {
            var d = car.Data;
            if (d.listedPrice > Profile.budget * 1.15f) return false;

            int value = Mathf.Max(1, CarValueCalculator.MarketValue(d));
            float ratio = d.listedPrice / (float)value;

            float interest = 0.7f;
            interest -= Mathf.Max(0f, ratio - 1f) * 2f;                               // overpriced
            if (car.Definition.category == Profile.preferredCategory) interest += 0.25f;
            interest += (CarSalesServices.Economy.Reputation - 50f) / 200f;           // dealer reputation
            if (d.condition < Profile.minCondition) interest -= 0.5f;
            if (d.cleanliness < 40f) interest -= 0.15f;
            if (CarSalesServices.Upgrades) interest += CarSalesServices.Upgrades.ShowroomInterestBonus;

            return Random.value < Mathf.Clamp01(interest);
        }

        // ---------------- HELPERS ----------------
        bool IsTargetValid() => TargetCar != null && TargetCar.Data.state == CarState.Listed;

        IEnumerator WaitUntilArrived(float timeout = 30f)
        {
            float t = 0f;
            yield return null; // let the path calculate
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.2f)
            {
                t += Time.deltaTime;
                if (t > timeout) yield break;
                yield return null;
            }
        }

        void Face(Vector3 point)
        {
            var dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }

        void SetBubble(string text)
        {
            if (!bubble) return;
            bubble.text = text;
            bubble.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}
