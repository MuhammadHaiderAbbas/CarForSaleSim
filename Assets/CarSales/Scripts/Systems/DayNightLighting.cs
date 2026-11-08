using UnityEngine;

namespace CarSales
{
    /// <summary>Moves the sun with the 24h clock (sunrise 06:00, sunset 20:30). Untick "Drive Sun" in a city with its own day/night.</summary>
    public class DayNightLighting : MonoBehaviour
    {
        [SerializeField] bool driveSun = true;
        [SerializeField] Light sun;
        [SerializeField] float sunrise = 6f, sunset = 20.5f;
        [SerializeField] float maxIntensity = 1.3f;
        [SerializeField] Color noonColor = new Color(1f, 0.96f, 0.9f), eveningColor = new Color(1f, 0.55f, 0.3f);
        float yaw;
        float baseAmbient = 1f;

        void Start()
        {
            if (!sun) sun = RenderSettings.sun;
            if (!sun)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            if (sun) yaw = sun.transform.eulerAngles.y;
            baseAmbient = RenderSettings.ambientIntensity;
        }

        void Update()
        {
            if (!driveSun || !sun || !CarSalesServices.DayCycle) return;
            float h = CarSalesServices.DayCycle.HourOfDay;
            float t = Mathf.InverseLerp(sunrise, sunset, h);
            bool day = h >= sunrise && h <= sunset;
            float height = day ? Mathf.Sin(t * Mathf.PI) : 0f;
            sun.transform.rotation = Quaternion.Euler(day ? Mathf.Lerp(4f, 176f, t) : -25f, yaw, 0f);
            sun.intensity = day ? Mathf.Lerp(0.15f, maxIntensity, Mathf.Clamp01(height * 1.5f)) : 0.03f;
            sun.color = Color.Lerp(eveningColor, noonColor, Mathf.Clamp01(height * 2f));
            RenderSettings.ambientIntensity = Mathf.Lerp(0.25f, baseAmbient, day ? Mathf.Clamp01(height * 2f + 0.2f) : 0f);
        }
    }
}
