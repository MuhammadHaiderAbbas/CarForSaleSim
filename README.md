# Car For Sale Simulator — Unity Project (v3.0 "AUTO WORLD")

A complete car-dealership simulator for **Unity 6 (URP)**:
buy used cars → inspect → repair in the garage & wash → test drive → set a price → customers browse → negotiate →
profit → upgrade your dealership → reach a **$1,000,000** net worth to win (or go bankrupt trying).

**v3.0 adds** a full dark-blue UI redesign (built from code, no Inspector wiring), live-rendered car photos,
a dealer computer with sidebar (Home, Marketplace with search/filters, Inventory, Messages, Upgrades, Bank, Stats),
vehicle details, per-part inspections, a Garage with repair options, a Car Wash, purchase confirmation,
private-seller offers in your inbox, Quick Sell, a HUD with **minimap + speedometer + key hints**, notification cards,
and a much bigger world: glass-front showroom with interior, service garage, car wash bay, props and city backdrop.
See [CHANGELOG.md](CHANGELOG.md).

> **Updating from v1/v2?** Close Unity. Copy this zip's `Assets` folder over your project's `Assets` folder and
> let it **overwrite** files (don't delete folders first — Unity's hidden `.meta` files must survive).
> Reopen Unity, then run **Tools → Car Sales → 2. Build Everything**. Old saves keep working.

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
| WASD / Left stick | Move |
| Mouse / Right stick | Look |
| Shift | Sprint |
| Space | Jump |
| **E** / Gamepad West | Interact (computer, car, customer, sign) |
| **O** / Gamepad Select | Open shop / close shop (ends the day) |
| Esc / Gamepad East | Close menu, or **pause** when no menu is open |
| *Test drive:* W/S, A/D, Space, E | Throttle/reverse, steer, brake, return the car |
| **Tab** / Gamepad North | Inventory (opens the computer anywhere) |
| F1 | +$10,000 (debug) |
| F5 / F9 | Save / Load (debug) |
| F12 | Delete save (debug) |

## 3. How to play

Follow the **GOAL** shown at the top-left. It teaches the game step by step and pays cash rewards.

1. You start inside the showroom with **$50,000** and **2 starter cars** on the lot.
2. **Computer (E):** *Marketplace*, *Upgrades*, *Bank & Loans*, *Statistics*.
   - Marketplace cars cost less than they're worth, but **sellers can lie about condition**. With the
     **Mechanic Workshop** upgrade you see the true condition.
3. **Walk to a car (E):** repair it, clean it, **Test Drive** it (crashes cost condition!), then **Set Price / List for Sale**.
4. **Press O** (or use the OPEN/CLOSED sign) to open. The clock runs from 08:00 to 18:00.
5. Customers walk in and look at **listed** cars. A **"!"** over a customer means they're interested:
   go and press **E** to negotiate.
6. Accept their offer, counter-offer, or refuse. Haggle too hard and they walk away (−reputation).
7. At 18:00 (or when you press O again) you pay rent and loan interest, and see the **daily report**.
   **Start Next Day** refreshes the marketplace. The game auto-saves.
8. **Upgrades:** Lot Expansion (+3 spots/level), Marketing (more and richer customers), Mechanic (cheaper repairs
   plus true inspections), Detailing (cheaper cleaning), Showroom (customers buy more often, +reputation daily).
9. **Win:** reach **$1,000,000 net worth** (cash + car values − loan).
   **Lose:** end **3 days in a row** with negative cash → bankrupt.

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
| Old save after changes | Press **F12** in Play mode, or **Tools → Car Sales → Delete Save File**. |

---

## 7. Verification notes

- All C# was compiled against Unity's real `UnityEngine` (2021.3 modules) and `UnityEditor` reference assemblies,
  with stand-in stubs for the UI / TextMeshPro / Input System / AI Navigation packages, and compiles with 0 errors.
- The project has **not** been opened in the Unity Editor itself (no editor was available when it was made).
  If anything fails on your Unity version, the Console message and the
  [troubleshooting table](#6-troubleshooting) should point straight to it.

Car brands and names (Kestrel, Orion, Titan, Vortex, Aurelia) are fictional placeholders.
