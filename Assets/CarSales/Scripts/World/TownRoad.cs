using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 14f: one road of the town, kept as a Unity Splines spline on the same object (when the Splines package is
    /// installed). Move or add knots, then Tools > Car Sales > Roads > Rebuild Road Meshes: carriageway, kerbs,
    /// pavements, junctions and white lines are made again from the splines.
    /// </summary>
    public class TownRoad : MonoBehaviour
    {
        public string roadName = "Road";
        [Min(4f)] public float width = 9f;
        [Tooltip("Town roads have kerbs and pavements; the country loop has gravel verges and edge lines.")]
        public bool pavements = true;
        public float speedLimit = 13.4f;
    }
}
