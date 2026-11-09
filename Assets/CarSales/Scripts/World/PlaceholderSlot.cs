using UnityEngine;

namespace CarSales
{
    /// <summary>Append only (saved choices use the numbers). 4+ are phase 13 furniture: Interior or Prop assets.</summary>
    public enum PlaceholderKind
    {
        Building = 0, Tree = 1, StreetLamp = 2, Bench = 3,
        Sofa = 4, Chair = 5, Table = 6, Plant = 7, Storage = 8, Bed = 9, Appliance = 10, Decor = 11, WorkshopKit = 12,
        Construction = 13, Equipment = 14,   // phase 14: site machines and materials; fuel pumps, lifts, racks
    }

    /// <summary>
    /// Marks a group of placeholder primitives (a backdrop building, a tree, a lamp, a bench) that the
    /// Asset Studio can swap for an imported model (Tools > Car Sales > Asset Studio > Town).
    /// The replacement is a child called "Replacement"; the placeholder's renderers are switched off.
    /// </summary>
    public class PlaceholderSlot : MonoBehaviour
    {
        public PlaceholderKind kind;
        [Tooltip("Size the replacement is fitted to (metres). Buildings fit the footprint, others the height.")]
        public Vector3 footprint = Vector3.one;
        [Tooltip("Turn applied to the replacement so its front (+Z) faces the street.")]
        public float yaw;
        [Tooltip("Phase 14: which kind of model fits here (villa, house, apartment, office, crane, fuelpump...). Empty = any.")]
        public string style = "";
        [Tooltip("Phase 14: picks a different model for slots in the same place (a building's levels 1, 2 and 3).")]
        public int variant;

        public const string ReplacementName = "Replacement";

        public Transform Replacement => transform.Find(ReplacementName);

        /// <summary>Stable number for picking one of several models (based on where the slot is).</summary>
        public int StableHash()
        {
            var p = transform.position;
            unchecked
            {
                int h = (int)kind * 73856093;
                h ^= Mathf.RoundToInt(p.x * 10f) * 19349663;
                h ^= Mathf.RoundToInt(p.z * 10f) * 83492791;
                h ^= variant * 486187739;
                return h & 0x7fffffff;
            }
        }
    }
}
