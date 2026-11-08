using UnityEngine;

namespace CarSales
{
    /// <summary>A place where a car can be parked. The blue (Z) axis = car front.</summary>
    public class ParkingSpot : MonoBehaviour
    {
        public int index;
        [Tooltip("Lot Expansion upgrade level needed before this spot can be used (0 = always available).")]
        public int requiredLotLevel;
        public CarDisplay Occupant { get; set; }
        public bool IsFree => Occupant == null;
        public bool IsUnlocked => !CarSalesServices.Upgrades || CarSalesServices.Upgrades.LotLevel >= requiredLotLevel;

        void OnDrawGizmos()
        {
            Gizmos.color = requiredLotLevel > 0 && !Application.isPlaying ? new Color(1f, 0.6f, 0f) : IsFree ? Color.green : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * 0.75f, new Vector3(2.4f, 1.5f, 5f));
            Gizmos.DrawLine(Vector3.up * 0.1f, Vector3.forward * 3f + Vector3.up * 0.1f);
        }
    }
}
