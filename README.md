# Car For Sale Simulator — Unity Project (v4.0 "Open Town")

A car-dealership life simulator for **Unity 6 (URP)**. You live in a small trailer, drive your own beat-up car to work,
buy cars online, at the **Used Car Market** or at the **Auction Center**, fix them at the **Mechanic Workshop**,
wash them at the **Car Wash**, fuel up at the **Fuel Station**, sell them at your dealerships, buy **more showrooms,
houses and investment properties** — and go home to **sleep** to end each day.
Reach a **$1,000,000** net worth to win (or go bankrupt trying).

**v4.0 adds** a whole town (roads, 10+ locations), driving any car you own (fuel, mileage, crashes), tow trucks & taxis,
live auctions vs. AI bidders, a walk-around used car lot, 3 dealerships, homes & properties, a real 24-hour day with
energy and sleeping, night lighting, a town map with GPS, and a phone. See [CHANGELOG.md](CHANGELOG.md).

**V5.0 in progress.** The full development blueprint (architecture, script-by-script plan, phases, tests) is in
[docs/V5_MASTER_SPEC.md](docs/V5_MASTER_SPEC.md). Phase 1 is in: 3 save slots with automatic backups, a bank
account statement of every transaction, and automated tests (*Window → General → Test Runner → EditMode → Run All*).

> **Updating from an older version?** Close Unity. Copy this project's `Assets` folder over your project's `Assets`
> folder and let it **overwrite** files (don't delete folders first — Unity's hidden `.meta` files must survive).
> Reopen Unity, then run **Tools → Car Sales → 2. Build Everything**. Older saves still load (you'll wake up at home).

## 1. Quick start (about 5 minutes)

1. **Install Unity 6 LTS** with Unity Hub (https://unity.com/download).
2. In Unity Hub: **New project → "Universal 3D" (URP) template** → name it `CarSalesSim` → Create.
   Then **close Unity**.
3. Copy the **`Assets`** folder from this zip **into your new project folder**, merging it with the
   existing `Assets` folder. You should end up with `CarSalesSim/Assets/CarSales/...`.
4. Open the project again in Unity.
5. If a **"Car Sales Simulator – packages are required"** dialog appears, click **Install**.
   - If Unity asks *"enable the new Input System backends?"*, click **Yes**. The editor restarts.
   - No dialog? The packages are already there. You can always run **Tools → Car Sales → 1. Check & Install Packages**.
6. **Tools → Car Sales → 2. Build Everything**.
   - The first time, it may ask you to import **TextMeshPro Essentials**: click *Import TMP Essentials*,
     then run **Build Everything** again.
7. The scene `Assets/CarSales/Generated/Scenes/DealershipTest.unity` opens. **Press Play** to jump straight in.
   For the full flow (main menu → game), open **Tools → Car Sales → 4. Open Main Menu Scene** and press Play.
   For a build, both scenes are already in Build Settings (MainMenu first).

> You don't build anything by hand: the wizard generates all materials, the 8 car models, the car data,
> the prefabs, every UI panel and the test scene. Run it again any time to rebuild the `Generated` folder.

---

## 2. Controls

| Key | Action |
|---|---|
| WASD / Mouse | Move / look |
| Shift / Space | Sprint / jump |
| **E** | Interact (cars, computer, desks, doors, signs) — while driving: park / refuel / wash / repair when prompted |
| **F** | Get into the car you're looking at / get out (the car stays where you leave it) |
| *Driving:* W/S, A/D, Space | Throttle/reverse, steer, brake |
| **Tab** | Inventory (dealer computer, anywhere) |
| **M** | Town map + GPS + taxi |
| **P** | Phone: tow truck (move any car) and taxi (move yourself) |
| **O** | Close / reopen your dealerships early |
| Esc | Close menu / pause |
| F1 / F5 / F9 / F12 | Debug: +$10,000 / save / load / delete save |

## 3. How to play

Follow the **GOAL** at the top-left — it teaches the game step by step and pays cash rewards.

1. **Morning:** you wake up at home (Starter Trailer). Your own car is on the driveway — stand next to it, press **F**.
   Press **M** and set the GPS to **AUTO WORLD**.
2. **Dealerships open 08:00–20:00** automatically. Customers only come during opening hours, only to dealerships you own,
   and only look at cars **listed for sale on a dealership spot**.
3. **Buy cars** — online at the office computer (+$300 delivery), at the **Used Car Market** (cheaper, free inspection,
   drive it away yourself) or at the **Auction Center** (10:00–17:00, cheapest but condition unknown!).
4. **Fix them** — drive (or tow, **P**) a car into the **Mechanic Workshop** bays and talk to the mechanic, or use the
   car menu → *Repair* (it offers a tow). Same for the **Car Wash**. Fuel up at the **Fuel Station** (E at the pumps).
5. **Sell** — park the car on a dealership spot (drive up to a free spot, press **E**), set a price, haggle with customers.
6. **Evening:** after 18:00 go home and **sleep at your front door**. Rent, loan interest and property income are settled,
   you get the daily report, the game saves. Stay up past **02:00** and you pass out (fine + less energy).
7. **Grow:** upgrades, a second and third dealership (Downtown Motors, Luxury Lane), better homes (wake earlier,
   +reputation), and investment properties that pay every night (computer → **Real Estate**, or the FOR SALE signs).
8. **Win** at $1,000,000 net worth (cash + cars + 80% of properties − loan). **Lose** after 3 nights in debt.

---

## 4. What's inside

```
Assets/CarSales/
├── Scripts/                     CarSales.Runtime.asmdef  (the game – portable)
│   ├── Core/                    Services locator, IInteractable, Bootstrap, Billboard, NavMesh baker
│   ├── Data/                    Enums, CarDefinition, CarDatabase, CarInstance, CustomerProfile, DailyReport, SaveData
│   ├── Systems/                 Economy, Inventory, Marketplace, DayCycle, Save, CarValueCalculator,
│   │                            Upgrades, Bank, Progress (goals/win/lose), Audio, TestDrive, DayNightLighting
│   ├── Vehicles/                ArcadeCarController (test drives)
│   ├── Dealership/              DealershipManager, ParkingSpot, CarDisplay, CarVisual, DealerComputer, ShopToggleSign
│   ├── Player/                  SimpleFirstPersonController, PlayerInteractor, MenuInputBlocker, DebugCheats
│   ├── Customers/               CustomerAI, CustomerSpawner, NegotiationSession
│   └── UI/                      UIManager, HUD, ComputerHub, Marketplace, Upgrades, Bank, Stats, CarMenu, Price,
│                                Negotiation, DailyReport, Pause, Settings, EndScreen, MainMenu
├── Editor/
│   ├── CarSalesSetupWizard.cs   Tools > Car Sales > Build Everything (generates everything)
│   └── Installer/               Package installer (always compiles, even before packages exist)
├── Input/CarSalesControls.inputactions
└── Generated/                   ← created by the wizard (materials, cars, prefabs, scene)
```

Generated prefabs (the parts you move into a city):

| Prefab | What it is |
|---|---|
| `CarSalesSystems.prefab` | All managers + the full UI canvas. One per scene. |
| `Dealership.prefab` | 14 parking spots, computer, open/close sign, customer spawn points + exit |
| `CarDisplay.prefab` | A car on the lot (collider, NavMesh obstacle, price tag, browse point) |
| `Customer.prefab` | NavMesh-driven customer with a speech bubble |
| `CarModels/Model_*.prefab` | 8 placeholder car models (swap for real models later) |
| `UI/MarketListingItem.prefab`, `UI/UpgradeRow.prefab` | Rows in the marketplace and upgrade lists |
| `Scenes/MainMenu.unity` | Main menu with a spinning showcase car |

Read **[docs/HOW_IT_WORKS.md](docs/HOW_IT_WORKS.md)** for the architecture, formulas, and how to add cars
and features. Read **[docs/REFERENCES.md](docs/REFERENCES.md)** for every script/asset in the project
and links to docs, tutorials and free assets.

---

## 5. Moving it into a city project (short version)

Full steps are in [docs/HOW_IT_WORKS.md → Moving into a city](docs/HOW_IT_WORKS.md#8-moving-into-a-city-project).

1. Use the **same Unity version and render pipeline** (URP ↔ URP).
2. Right-click `Assets/CarSales` → **Export Package…** → import the `.unitypackage` into the city project
   (or just copy the folder).
3. In the city project: install the packages (the installer dialog appears), set **Layer 20 = `Interactable`**, then:
   - drag **`CarSalesSystems.prefab`** and **`Dealership.prefab`** into the city scene,
   - add **`PlayerInteractor`** + **`MenuInputBlocker`** to the city's player,
   - bake a **NavMeshSurface** around the dealership,
   - keep only **one EventSystem** (with *Input System UI Input Module*).

---

## 6. Troubleshooting

| Problem | Fix |
|---|---|
| No **Tools → Car Sales** menu | Check the Console for errors. Run *1. Check & Install Packages*. |
| Only "1. Check & Install Packages" in the menu | Packages are missing, so the game scripts are switched off. Install them. |
| Pink materials | You're not on URP (or HDRP). Use the *Universal 3D* template. |
| Customers don't move | NavMesh missing: select `Environment` → *NavMeshSurface* → **Bake**. (The runtime baker also bakes it if empty.) |
| Customers never come | You must **list** at least one car (car menu → Set Price → List for Sale) and **open** the shop (O). |
| No interaction prompt | The object isn't on Layer 20 *Interactable*, or the Player's *PlayerInteractor* mask isn't set. |
| UI buttons don't click | The EventSystem must use **Input System UI Input Module**. |
| Mouse is stuck / visible | Click inside the Game view. Esc closes menus. |
| Old save after changes | Press **F12** in Play mode (deletes the active slot), or **Tools → Car Sales → Delete All Saves**. |
| "Save recovered" message | The latest save file was damaged; the game loaded the newest good backup. Nothing to do. |

---

## 7. Verification notes

- All C# was compiled against Unity's real `UnityEngine` (2021.3 modules) and `UnityEditor` reference assemblies,
  with stand-in stubs for the UI / TextMeshPro / Input System / AI Navigation packages, and compiles with 0 errors.
- The project has **not** been opened in the Unity Editor itself (no editor was available when it was made).
  If anything fails on your Unity version, the Console message and the
  [troubleshooting table](#6-troubleshooting) should point straight to it.

Car brands and names (Kestrel, Orion, Titan, Vortex, Aurelia) are fictional placeholders.
