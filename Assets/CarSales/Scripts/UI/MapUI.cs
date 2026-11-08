using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Town map (M): top-down render of the town, place icons, GPS and taxi buttons.</summary>
    public class MapUI : UIPanel
    {
        [SerializeField] Vector3 mapCenter = new Vector3(0f, 0f, 85f);
        [SerializeField] float mapHalfSize = 235f;
        const float MapPixels = 800f;

        Camera mapCam;
        RenderTexture rt;
        RawImage mapImage;
        RectTransform iconLayer, list;
        Image playerArrow;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Town Map", new Vector2(1500f, 920f), CloseAll);
            UIFactory.Horizontal(body, 0, 24f, TextAnchor.UpperLeft).childForceExpandHeight = true;

            var frame = UIFactory.Panel(body, "MapFrame", new Color(0.05f, 0.08f, 0.14f), 1.2f);
            UIFactory.LE(frame, w: MapPixels, h: MapPixels, flexW: 0);
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            mapImage = UIFactory.Rect("Map", frame.transform).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(mapImage.rectTransform);
            mapImage.raycastTarget = false;
            iconLayer = UIFactory.Rect("Icons", frame.transform);
            UIFactory.Stretch(iconLayer);
            playerArrow = UIFactory.SpriteImage(frame.transform, "You", UIFactory.Arrow, Color.white, 28f);

            var right = UIFactory.VCol(body, 10f, "Places");
            UIFactory.LE(UIFactory.Text(right, "Places", 24, UIFactory.TextMain, TextAlignmentOptions.Left, true), h: 32);
            UIFactory.LE(UIFactory.Text(right, "GPS shows an arrow on your HUD. Taxi takes you there instantly (costs money and time).", 15, UIFactory.TextDim, TextAlignmentOptions.Left), h: 40);
            list = UIFactory.ScrollList(right, 8f);
            UIFactory.Btn(right, "Clear GPS", UIFactory.Neutral, () => { GpsSystem.Clear(); Rebuild(); }, 44f, 17f);
        }

        public void Show()
        {
            OpenSelf();
            RenderMap();
            Rebuild();
        }

        void RenderMap()
        {
            if (!rt) rt = new RenderTexture(1024, 1024, 16) { name = "TownMap" };
            if (!mapCam)
            {
                var go = new GameObject("TownMapCamera");
                mapCam = go.AddComponent<Camera>();
                mapCam.orthographic = true;
                mapCam.clearFlags = CameraClearFlags.SolidColor;
                mapCam.backgroundColor = new Color(0.08f, 0.12f, 0.2f);
                mapCam.cullingMask = ~((1 << ThumbnailService.StudioLayer) | (1 << 5));
                mapCam.nearClipPlane = 1f;
                mapCam.farClipPlane = 600f;
                mapCam.aspect = 1f;
                mapCam.enabled = false;
            }
            mapCam.orthographicSize = mapHalfSize;
            mapCam.transform.SetPositionAndRotation(new Vector3(mapCenter.x, 300f, mapCenter.z), Quaternion.Euler(90f, 0f, 0f));
            mapCam.targetTexture = rt;
            mapImage.texture = rt;
            StartCoroutine(RenderOnce());
        }

        IEnumerator RenderOnce()
        {
            mapCam.enabled = true;
            yield return new WaitForEndOfFrame();
            mapCam.enabled = false;
        }

        Vector2 ToMap(Vector3 world) =>
            new Vector2((world.x - mapCenter.x) / mapHalfSize, (world.z - mapCenter.z) / mapHalfSize) * (MapPixels / 2f);

        void Rebuild()
        {
            UIFactory.ClearChildren(iconLayer);
            UIFactory.ClearChildren(list);
            var player = FindFirstObjectByType<PlayerInteractor>();
            Vector3 me = Camera.main ? Camera.main.transform.position : player ? player.transform.position : Vector3.zero;

            foreach (var place in MapPlace.All.Where(p => p && p.IsVisible).OrderBy(p => Vector3.Distance(me, p.transform.position)))
            {
                bool gps = GpsSystem.Target == place;
                var icon = UIFactory.Icon(iconLayer, gps ? UIFactory.Gold : place.color, place.glyph, gps ? 34f : 28f);
                icon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ToMap(place.transform.position), new Vector2(gps ? 34f : 28f, gps ? 34f : 28f));
                var label = UIFactory.Text(iconLayer, place.placeName, 13, UIFactory.TextMain, TextAlignmentOptions.Center, true);
                UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), ToMap(place.transform.position) + new Vector2(0f, -18f), new Vector2(160f, 20f));

                var row = UIFactory.Panel(list, place.placeName, gps ? UIFactory.AccentDark : UIFactory.CardBg, 1.8f);
                UIFactory.LE(row, h: 60);
                var h = UIFactory.Horizontal(row, 8, 10f);
                h.padding = new RectOffset(10, 10, 8, 8);
                h.childForceExpandHeight = false;
                UIFactory.Icon(row.transform, place.color, place.glyph, 36f);
                var col = UIFactory.VCol(row.transform, 0f);
                UIFactory.Text(col, place.placeName, 18, UIFactory.TextMain, TextAlignmentOptions.Left, true);
                UIFactory.Text(col, $"{Vector3.Distance(me, place.transform.position):0} m away", 14, UIFactory.TextDim, TextAlignmentOptions.Left);
                var p = place;
                var gpsBtn = UIFactory.Btn(row.transform, gps ? "GPS on" : "GPS", gps ? UIFactory.Gold : UIFactory.Accent, () => { GpsSystem.SetTarget(p); Rebuild(); }, 40f, 15f);
                UIFactory.LE(gpsBtn, h: 40, w: 80, flexW: 0);
                int fare = CarSalesServices.Tow ? CarSalesServices.Tow.TaxiCost(me, p.ArrivalPosition) : 0;
                var taxi = UIFactory.Btn(row.transform, $"Taxi {UIFactory.Money(fare)}", UIFactory.Warn, () => TakeTaxi(p), 40f, 14f);
                UIFactory.LE(taxi, h: 40, w: 110, flexW: 0);
            }

            playerArrow.transform.SetAsLastSibling();
            UIFactory.Place(playerArrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ToMap(me), new Vector2(28f, 28f));
            float yaw = Camera.main ? Camera.main.transform.eulerAngles.y : 0f;
            playerArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -yaw);
        }

        void TakeTaxi(MapPlace place)
        {
            if (CarSalesServices.Tow.Taxi(place, out string msg)) { CloseAll(); CarSalesServices.UI.Success("Taxi", msg); }
            else CarSalesServices.UI.Error(msg);
        }

        void OnDestroy()
        {
            if (mapCam) Destroy(mapCam.gameObject);
            if (rt) { rt.Release(); Destroy(rt); }
        }
    }
}
