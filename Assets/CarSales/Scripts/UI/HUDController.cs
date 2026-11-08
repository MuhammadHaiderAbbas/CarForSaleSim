using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>In-game HUD: day/clock/goal, money, minimap, speedometer, key hints, interaction prompt.</summary>
    public class HUDController : UIPanel
    {
        TextMeshProUGUI unitText, clockText, shopText, goalText, moneyText, repText, loanText, promptText, speedText, unreadText, energyText, gpsText, fuelText;
        UIFactory.Bar energyBar;
        Image gpsArrow, gpsPanel;
        MinimapMarker gpsMarker;
        Image promptPanel, speedFill, arrow;
        RawImage minimapImage;
        Camera minimapCamera;
        RenderTexture minimapTexture;
        PlayerInteractor interactor;
        CharacterController playerBody;

        const float MaxGaugeKmh = 200f;

        public override void Build()
        {
            // Top-left: day, shop state, goal
            var tl = UIFactory.Panel(Root, "TopLeft", new Color(0.03f, 0.06f, 0.13f, 0.78f), 1.3f);
            UIFactory.Place(tl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(660f, 178f));
            var tlv = UIFactory.Vertical(tl, 16, 4f);
            tlv.padding = new RectOffset(20, 18, 14, 14);
            clockText = UIFactory.Text(tl.transform, "Day 1   08:00", 30, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            shopText = UIFactory.Text(tl.transform, "SHOP CLOSED", 17, UIFactory.Bad, TextAlignmentOptions.Left, true);
            var energyRow = UIFactory.HRow(tl.transform, 18, 10, "Energy", false);
            energyText = UIFactory.Text(energyRow, "Energy", 15, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            UIFactory.LE(energyText, w: 130, flexW: 0);
            var barHolder = UIFactory.VCol(energyRow, 0f, "BarHolder");
            barHolder.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            energyBar = UIFactory.ProgressBar(barHolder, UIFactory.Good, 10f);
            goalText = UIFactory.Text(tl.transform, "", 16, new Color(0.55f, 0.85f, 1f), TextAlignmentOptions.Left);

            // Top-right: money
            var tr = UIFactory.Panel(Root, "TopRight", new Color(0.03f, 0.06f, 0.13f, 0.78f), 1.3f);
            UIFactory.Place(tr.rectTransform, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(330f, 116f));
            var trv = UIFactory.Vertical(tr, 14, 2f, TextAnchor.UpperRight);
            trv.padding = new RectOffset(18, 20, 12, 12);
            moneyText = UIFactory.Text(tr.transform, "$0", 40, UIFactory.Good, TextAlignmentOptions.Right, true);
            repText = UIFactory.Text(tr.transform, "Reputation 50/100", 16, UIFactory.TextDim, TextAlignmentOptions.Right);
            loanText = UIFactory.Text(tr.transform, "", 16, UIFactory.Warn, TextAlignmentOptions.Right);

            // Top-centre: GPS
            gpsPanel = UIFactory.Panel(Root, "GPS", new Color(0.03f, 0.06f, 0.13f, 0.85f), 3f);
            UIFactory.Place(gpsPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(460f, 54f));
            gpsArrow = UIFactory.SpriteImage(gpsPanel.transform, "Arrow", UIFactory.Arrow, UIFactory.Gold, 30f);
            UIFactory.Place(gpsArrow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 0f), new Vector2(30f, 30f));
            gpsText = UIFactory.Text(gpsPanel.transform, "", 19, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.Stretch(gpsText.rectTransform);
            gpsText.rectTransform.offsetMin = new Vector2(62f, 0f);
            gpsPanel.gameObject.SetActive(false);

            // Bottom-left: minimap
            var frame = UIFactory.SpriteImage(Root, "MinimapFrame", UIFactory.Circle, UIFactory.Accent, 244f);
            UIFactory.Place(frame.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(244f, 244f));
            var mask = UIFactory.SpriteImage(frame.transform, "MinimapMask", UIFactory.Circle, Color.white, 232f);
            mask.rectTransform.anchoredPosition = Vector2.zero;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            minimapImage = UIFactory.Rect("Map", mask.transform).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(minimapImage.rectTransform);
            minimapImage.raycastTarget = false;
            arrow = UIFactory.SpriteImage(mask.transform, "PlayerArrow", UIFactory.Arrow, Color.white, 26f);
            arrow.rectTransform.anchoredPosition = Vector2.zero;
            var north = UIFactory.Text(frame.transform, "N", 18, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.Place(north.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(30f, 26f));

            // Speedometer
            var gauge = UIFactory.SpriteImage(Root, "Speedometer", UIFactory.Circle, new Color(0.03f, 0.06f, 0.13f, 0.85f), 150f);
            UIFactory.Place(gauge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(282f, 30f), new Vector2(150f, 150f));
            var ringBg = UIFactory.SpriteImage(gauge.transform, "RingBg", UIFactory.Ring, UIFactory.Ghost, 136f);
            ringBg.rectTransform.anchoredPosition = Vector2.zero;
            speedFill = UIFactory.SpriteImage(gauge.transform, "RingFill", UIFactory.Ring, UIFactory.Accent, 136f);
            speedFill.rectTransform.anchoredPosition = Vector2.zero;
            speedFill.type = Image.Type.Filled;
            speedFill.fillMethod = Image.FillMethod.Radial360;
            speedFill.fillOrigin = 0;
            speedFill.fillClockwise = true;
            speedFill.fillAmount = 0f;
            speedText = UIFactory.Text(gauge.transform, "0", 42, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.Place(speedText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(120f, 50f));
            var unit = unitText = UIFactory.Text(gauge.transform, "KM/H", 14, UIFactory.TextDim, TextAlignmentOptions.Center, true);
            UIFactory.Place(unit.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(80f, 20f));
            fuelText = UIFactory.Text(gauge.transform, "", 14, UIFactory.Gold, TextAlignmentOptions.Center, true);
            UIFactory.Place(fuelText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(160f, 22f));

            // Bottom-right: key hints
            var keys = UIFactory.Panel(Root, "KeyHints", new Color(0.03f, 0.06f, 0.13f, 0.7f), 1.5f);
            UIFactory.Place(keys.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(250f, 284f));
            var kv = UIFactory.Vertical(keys, 14, 6f);
            kv.padding = new RectOffset(16, 14, 14, 14);
            UIFactory.KeyHint(keys.transform, "E", "Interact");
            UIFactory.KeyHint(keys.transform, "F", "Enter / exit car");
            unreadText = UIFactory.KeyHint(keys.transform, "Tab", "Inventory");
            UIFactory.KeyHint(keys.transform, "M", "Map & GPS");
            UIFactory.KeyHint(keys.transform, "P", "Phone (tow, taxi)");
            UIFactory.KeyHint(keys.transform, "O", "Close / open shops");
            UIFactory.KeyHint(keys.transform, "Esc", "Menu");

            // Bottom-centre prompt
            promptPanel = UIFactory.Panel(Root, "Prompt", new Color(0.03f, 0.06f, 0.13f, 0.85f), 3f);
            UIFactory.Place(promptPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(980f, 52f));
            promptText = UIFactory.Text(promptPanel.transform, "", 22, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(promptText.rectTransform, 6f);

            // Crosshair
            var cross = UIFactory.SpriteImage(Root, "Crosshair", UIFactory.Circle, new Color(1f, 1f, 1f, 0.85f), 8f);
            UIFactory.Place(cross.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
        }

        void Start()
        {
            interactor = FindFirstObjectByType<PlayerInteractor>();
            if (interactor) playerBody = interactor.GetComponent<CharacterController>();
            CreateMinimapCamera();

            var eco = CarSalesServices.Economy;
            var day = CarSalesServices.DayCycle;
            eco.OnMoneyChanged += HandleMoney;
            eco.OnReputationChanged += HandleRep;
            day.OnTimeChanged += HandleTime;
            day.OnShopStateChanged += HandleShop;
            if (CarSalesServices.Progress) CarSalesServices.Progress.OnObjectiveChanged += HandleObjective;
            if (CarSalesServices.Bank) CarSalesServices.Bank.OnLoanChanged += HandleLoan;
            HandleMoney(eco.Money, 0);
            HandleRep(eco.Reputation);
            HandleTime(day.Hour);
            HandleShop(day.IsOpen);
            HandleObjective();
            HandleLoan();
        }

        void CreateMinimapCamera()
        {
            minimapTexture = new RenderTexture(512, 512, 16) { name = "Minimap" };
            var go = new GameObject("MinimapCamera");
            minimapCamera = go.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 34f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.08f, 0.12f, 0.2f);
            minimapCamera.cullingMask = ~((1 << ThumbnailService.StudioLayer) | (1 << 5));
            minimapCamera.nearClipPlane = 1f;
            minimapCamera.farClipPlane = 200f;
            minimapCamera.targetTexture = minimapTexture;
            minimapCamera.depth = -10;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minimapImage.texture = minimapTexture;
            if (Camera.main) Camera.main.cullingMask &= ~(1 << MinimapMarker.MinimapLayer);
        }

        void OnDestroy()
        {
            if (CarSalesServices.Economy)
            {
                CarSalesServices.Economy.OnMoneyChanged -= HandleMoney;
                CarSalesServices.Economy.OnReputationChanged -= HandleRep;
            }
            if (CarSalesServices.DayCycle)
            {
                CarSalesServices.DayCycle.OnTimeChanged -= HandleTime;
                CarSalesServices.DayCycle.OnShopStateChanged -= HandleShop;
            }
            if (CarSalesServices.Progress) CarSalesServices.Progress.OnObjectiveChanged -= HandleObjective;
            if (CarSalesServices.Bank) CarSalesServices.Bank.OnLoanChanged -= HandleLoan;
            if (minimapCamera) Destroy(minimapCamera.gameObject);
            if (minimapTexture) { minimapTexture.Release(); Destroy(minimapTexture); }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam)
            {
                cam.cullingMask &= ~((1 << MinimapMarker.MinimapLayer) | (1 << ThumbnailService.StudioLayer));
                if (minimapCamera)
                {
                    Vector3 p = cam.transform.position;
                    minimapCamera.transform.position = new Vector3(p.x, p.y + 80f, p.z);
                }
                arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -cam.transform.eulerAngles.y);
            }

            // Speed
            var drive = CarSalesServices.TestDrive;
            if (!interactor) { interactor = FindFirstObjectByType<PlayerInteractor>(); if (interactor) playerBody = interactor.GetComponent<CharacterController>(); }
            UpdateEnergyAndGps();
            fuelText.text = drive && drive.IsDriving ? $"FUEL {drive.Fuel:0}%" : "";
            fuelText.color = drive && drive.IsDriving && drive.Fuel < 15f ? UIFactory.Bad : UIFactory.Gold;
            float kmh = drive && drive.IsDriving ? drive.SpeedKmh
                       : playerBody ? new Vector3(playerBody.velocity.x, 0f, playerBody.velocity.z).magnitude * 3.6f : 0f;
            speedText.text = Mathf.RoundToInt(kmh).ToString();
            unitText.text = drive && drive.IsDriving ? $"<color={UIFactory.Hex(UIFactory.Gold)}>{drive.Gear}</color>  KM/H" : "KM/H";
            speedFill.fillAmount = Mathf.Clamp01(kmh / MaxGaugeKmh) * 0.8f;
            speedFill.color = kmh > 130f ? UIFactory.Warn : UIFactory.Accent;

            // Prompt
            string prompt;
            if (drive && drive.IsDriving)
                prompt = string.IsNullOrEmpty(drive.ContextPrompt) ? "W/S drive   A/D steer   Space brake   F get out" : drive.ContextPrompt + "      (F get out)";
            else
                prompt = interactor ? interactor.CurrentPrompt : "";
            promptText.text = prompt;
            promptPanel.gameObject.SetActive(!string.IsNullOrEmpty(prompt));

            // Unread messages
            var msgs = CarSalesServices.Messages;
            if (unreadText && msgs)
            {
                int unread = msgs.UnreadCount;
                unreadText.text = unread > 0 ? $"Inventory  <color={UIFactory.Hex(UIFactory.Gold)}>({unread} new msg)</color>" : "Inventory";
            }
        }

        void HandleMoney(int money, int delta)
        {
            moneyText.text = UIFactory.Money(money);
            moneyText.color = money < 0 ? UIFactory.Bad : UIFactory.Good;
        }

        void HandleRep(float rep) => repText.text = $"Reputation {rep:0}/100";

        void HandleTime(float hour)
        {
            var day = CarSalesServices.DayCycle;
            float hod = day.HourOfDay;
            int h = Mathf.FloorToInt(hod), m = Mathf.FloorToInt((hod - h) * 60f);
            clockText.text = $"{CarSales.Foundation.GameCalendar.WeekdayShort(day.CurrentDay)}  Day {day.CurrentDay}    {h:00}:{m:00}";
            HandleShop(day.IsOpen);
        }

        void HandleShop(bool open)
        {
            var day = CarSalesServices.DayCycle;
            if (open) { shopText.text = $"DEALERSHIPS OPEN until {day.CloseHour:00}:00"; shopText.color = UIFactory.Good; }
            else if (day.Hour < day.OpenHour) { shopText.text = $"Dealerships open at {day.OpenHour:00}:00"; shopText.color = UIFactory.Warn; }
            else if (day.Hour >= day.CloseHour) { shopText.text = day.Hour >= DayCycleManager.EarliestSleepHour ? "Closed - go home and sleep (bed)" : "Dealerships closed for today"; shopText.color = UIFactory.Bad; }
            else { shopText.text = "Dealerships CLOSED by you (O to reopen)"; shopText.color = UIFactory.Bad; }
        }

        void UpdateEnergyAndGps()
        {
            var day = CarSalesServices.DayCycle;
            if (day && energyBar != null)
            {
                float e = day.Energy;
                energyBar.Set(e / 100f);
                energyBar.SetColor(e > 40f ? UIFactory.Good : e > 15f ? UIFactory.Warn : UIFactory.Bad);
                energyText.text = $"Energy {e:0}%";
            }
            var target = GpsSystem.Target;
            gpsPanel.gameObject.SetActive(target);
            if (!gpsMarker)
            {
                var go = new GameObject("GpsMarker");
                gpsMarker = MinimapMarker.Attach(go, UIFactory.Gold, 9f);
            }
            gpsMarker.SetVisible(target);
            var cam = Camera.main;
            if (!target || !cam) return;
            gpsMarker.transform.position = target.transform.position + Vector3.up * 2f;
            Vector3 to = target.transform.position - cam.transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 15f)
            {
                gpsText.text = $"Arrived: {target.placeName}";
                gpsArrow.rectTransform.localEulerAngles = Vector3.zero;
                return;
            }
            float angle = Vector3.SignedAngle(new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z), to, Vector3.up);
            gpsArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            gpsText.text = $"{target.placeName}   {dist:0} m";
        }

        void HandleObjective()
        {
            if (CarSalesServices.Progress) goalText.text = CarSalesServices.Progress.CurrentObjectiveText;
        }

        void HandleLoan()
        {
            int loan = CarSalesServices.Bank ? CarSalesServices.Bank.Loan : 0;
            loanText.text = loan > 0 ? $"Loan {UIFactory.Money(loan)}" : "";
        }
    }
}
