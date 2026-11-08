using UnityEngine;

namespace CarSales
{
    /// <summary>Moves the sun with the shop clock. Turn off "Drive Sun" if your city has its own day/night system.</summary>
    public class DayNightLighting : MonoBehaviour
    {
        [SerializeField] bool driveSun = true;
        [SerializeField] Light sun;                 // empty = RenderSettings.sun or the first directional light
        [SerializeField] float openHour = 8f, closeHour = 18f;
        [SerializeField] float morningAngle = 20f, eveningAngle = 165f;
        [SerializeField] float maxIntensity = 1.3f, minIntensity = 0.25f;
        [SerializeField] Color noonColor = new Color(1f, 0.96f, 0.9f), eveningColor = new Color(1f, 0.6f, 0.35f);
        float yaw;

        void Start()
        {
            if (!sun) sun = RenderSettings.sun;
            if (!sun)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            if (sun) yaw = sun.transform.eulerAngles.y;
        }

        void Update()
        {
            if (!driveSun || !sun || !CarSalesServices.DayCycle) return;
            var day = CarSalesServices.DayCycle;
            float t = day.IsDayOver ? 1f : Mathf.InverseLerp(openHour, closeHour, day.Hour);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(morningAngle, eveningAngle, t), yaw, 0f);
            float height = Mathf.Sin(t * Mathf.PI);       // 0 at open/close, 1 at midday
            sun.intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.Clamp01(height * 1.4f));
            sun.color = Color.Lerp(eveningColor, noonColor, Mathf.Clamp01(height * 2f));
        }
    }
}
