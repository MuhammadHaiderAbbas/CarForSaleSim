# Car Sales Simulator V5.0 — Master Technical Development Specification

**Status:** Draft 1. Phase 1 is merged; Phase 2a code is written (see [Phase 2 status](#phase-2-status)); everything else is still a plan.
**Baseline:** v4.0 "Open Town" (commit `e242293`): 95 runtime scripts, ~7,800 lines runtime C#, 2 editor generators, 1 installer
**Engine:** Unity 6 LTS, URP, C#
**Default region:** United Kingdom (GBP, V5C/MOT/write-off categories). Region rules are data, so other regions can be added later.

This document is the single blueprint for V5.0. It says what we keep, what we change, what we add, in which order,
how each piece is tested, and when a phase counts as finished. Implementation follows it phase by phase; when
reality disagrees with the spec, we update the spec first.

---

## Contents

0. [Ground rules](#0-ground-rules)
1. [Who does what](#1-who-does-what)
2. [Decisions that differ from the audit](#2-decisions-that-differ-from-the-audit)
3. [Target architecture](#3-target-architecture)
4. [Folder structure and assemblies](#4-folder-structure-and-assemblies)
5. [Fate of every v4 script](#5-fate-of-every-v4-script)
6. [Module specifications](#6-module-specifications)
   - 6.1 Core (events, ledger, clock, config, region) · 6.2 Save system · 6.3 Vehicle data model
   - 6.4 Vehicle physics · 6.5 Inspection & knowledge · 6.6 Workshop, parts & repairs · 6.7 Documents & history
   - 6.8 Market model & sourcing · 6.9 Auction engine · 6.10 Customers & sales · 6.11 Staff & jobs
   - 6.12 Business & finance · 6.13 World simulation · 6.14 UI design system · 6.15 Audio
7. [Editor tools](#7-editor-tools) — Asset Studio, Vehicle Studio, World Builder, Simulation Configurator
8. [Data formulas](#8-data-formulas)
9. [Implementation phases](#9-implementation-phases) (with step lists and definition of done)
10. [Testing strategy](#10-testing-strategy)
11. [Performance budgets](#11-performance-budgets)
12. [Asset licensing policy](#12-asset-licensing-policy)
13. [Risks](#13-risks)
14. [Glossary](#14-glossary)

---

## 0. Ground rules

These rules apply to every phase. A change that breaks one of them is not merged.

| # | Rule | Why |
|---|---|---|
| R1 | **The game must stay playable at the end of every phase.** New systems are added next to old ones and switched over only after they work. | You have a working v4. We never trade it for a half-built v5. |
| R2 | **Never rename or move a `MonoBehaviour` or `ScriptableObject` class/file** that exists in v4. Add new ones instead and retire old ones later. | Unity links components by script GUID (stored in `.meta`). The zip-overwrite update workflow gives renamed/moved files new GUIDs and breaks every scene/prefab reference (that is exactly what caused the early `NullReferenceException`). |
| R3 | **Simulation logic lives in plain C# classes**, not in `MonoBehaviour`s. MonoBehaviours are thin adapters (input, visuals, physics). | Plain classes can be unit-tested without the Unity Editor running, and saved/loaded cleanly. |
| R4 | **Gameplay never references UI.** Systems publish events; UI listens. | Lets us rebuild any screen without touching gameplay. |
| R5 | **Every money movement goes through the Ledger** (`Ledger.Post`). No system edits cash directly. | Accounting, VAT, daily reports and tests all read the same records. |
| R6 | **Truth vs. knowledge:** a vehicle's real state and what the player *knows* about it are stored separately. | Inspections, auctions, private sellers and lies only work if the game knows the difference. |
| R7 | **All tunable numbers are data** (ScriptableObject configs), not constants in code. | Balancing without recompiling; Simulation Configurator edits them. |
| R8 | **Every save field has a version and a migration.** Old saves always load or fail with a readable message plus a backup. | Players lose trust after one corrupted save. |
| R9 | **Every imported asset has a licence record.** Release builds refuse assets with unknown or non-commercial licences. | Legal safety for a commercial game. |
| R10 | **Each phase ends with tests passing and a short manual play-test checklist done in the real Unity Editor.** | v4 was only compile-checked. That ends in Phase 0. |

---

## 1. Who does what

I write code, tools, data, docs and tests, and verify them by compiling against Unity's assemblies. I **cannot**
open the Unity Editor, see the game, buy or download store assets, or judge how driving feels. So each phase has
a hand-off:

| Task | Claude | You |
|---|---|---|
| Write scripts, editor tools, configs, tests | ✅ | review |
| Compile-check against Unity assemblies | ✅ | — |
| Run in the Unity Editor, play-test, report Console errors/screenshots | — | ✅ (essential, every phase) |
| Run the Unity Test Runner (EditMode/PlayMode) | write the tests | ✅ click *Run All*, send failures |
| Choose/buy/download 3D assets (Sketchfab, KitBash3D, Asset Store...) | recommend, write importers | ✅ (accounts, purchases, licences) |
| Tune driving feel, visuals, balancing | propose values | ✅ judge, report |
| GitHub repository | commit locally, push once the repo exists and the Claude GitHub App can access it | ✅ create `car-for-sale-sim` (empty) |

---

## 2. Decisions that differ from the audit

I agree with almost all of the audit. These are the places where I'd do it differently, and why.

| Topic | Audit proposal | This spec | Reason |
|---|---|---|---|
| **Order** | Asset Studio first | **Stabilise → Core/Save/Tests → Vehicle Framework → Asset Studio** | Asset Studio's most valuable job ("make this downloaded car drivable") needs the Vehicle Framework to exist first. Without it, an imported car has nothing to be set up *for*. |
| **Folder layout** | Move everything to `Assets/_Project/...` | **Keep `Assets/CarSales/`**, add new module folders beside `Scripts/` | Rule R2. A mass move breaks every reference in your existing project. |
| **"Paste any URL" importer** | Universal downloader | **Provider connectors only where an official API exists (Sketchfab, Poly Haven), plus a watched "Drop" folder and drag-and-drop for everything else** | KitBash3D, CGTrader, Fab, Quixel and the Asset Store have no public download API for this; scraping them breaks their terms. You download those yourself and Asset Studio takes it from there. |
| **Automatic LODs** | Always generated | **Use the model's own LODs, Unity 6.2+ import-time Mesh LOD where available, otherwise the MIT-licensed UnityMeshSimplifier** | Unity has no built-in LOD generator on older 6.x versions. |
| **Competitor dealerships** | Full AI businesses walking around | **Simulated "off-screen" first** (stock, prices, market share), physical competitor lots later | 90% of the gameplay effect for 10% of the cost. |
| **Traffic AI** | Phase 7 full traffic | **Lane graph + simple followers + traffic lights**, no overtaking in V5.0 | Overtaking/parking AI is a project on its own; it can come in V5.x. |
| **Multiplayer** | Optional | **Out of scope for V5.0** | Retrofitting networking into a single-player sim means rewriting most systems. |
| **Real UK legal processes** | Detailed | **Simulated through menus and data**, never presented as real legal/financial advice | As the audit itself notes. |

---

## 3. Target architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ PRESENTATION (CarSales.Runtime)                                              │
│  UI screens · HUD · Phone OS · World objects · Vehicle controllers · Audio   │
│  (MonoBehaviours: read state, send commands, listen to events)               │
├──────────────────────────────────────────────────────────────────────────────┤
│ SIMULATION (CarSales.Sim — plain C#, unit-tested)                            │
│  Vehicles · Inspection · Workshop · Documents · Market · Auction · Customers  │
│  Staff · Business/Finance · World sim (weather, competitors, calendar)       │
├──────────────────────────────────────────────────────────────────────────────┤
│ CORE (CarSales.Core — plain C#)                                              │
│  EventBus · Ledger · GameClock · ServiceRegistry · Config · Region · RNG     │
│  Save (modules, versions, migrations, atomic files, backups)                 │
└──────────────────────────────────────────────────────────────────────────────┘
 EDITOR (CarSales.Editor + CarSales.Studio.Editor)
  Setup Wizard · Asset Studio · Vehicle Studio · World Builder · Sim Configurator
```

Dependencies only point **downward**. `CarSales.Runtime` (the v4 code) references `CarSales.Sim` and `CarSales.Core`,
never the other way round. Over the phases, logic moves *down* out of v4 MonoBehaviours into Sim classes, and
the MonoBehaviours become adapters (the "strangler" approach: the old code keeps working while it is replaced piece by piece).

### 3.1 How systems talk

| Mechanism | Used for | Example |
|---|---|---|
| **Commands** (method calls on a service) | Asking a system to do something | `Auction.PlaceBid(lotId, buyerId, amount)` |
| **Events** (`EventBus.Publish<T>`) | Telling everyone something happened | `LotSold { lotId, buyerId, hammerPrice }` |
| **Queries** (read-only properties) | UI reading state | `Inventory.CarsIn(LocationId)` |
| **ServiceRegistry** | Finding a service | `Services.Get<IAuctionEngine>()` |

`CarSalesServices` (v4) stays as a static façade and forwards to the registry, so old code keeps compiling.

---

## 4. Folder structure and assemblies

```
Assets/CarSales/
├── Scripts/                     CarSales.Runtime.asmdef   (v4 code, stays here — rule R2)
│   └── ... existing folders, plus new adapters:
│       ├── Vehicles/Physics/    WheelVehicleController, VehicleInput, DamageReceiver ...
│       ├── World/Sites/         Site, SiteEntrance, AuctionHall, InspectionBay ...
│       ├── Customers/Behaviour/ BuyerBrain, BuyerActions ...
│       ├── Staff/               EmployeeAgent ...
│       ├── Traffic/             TrafficCar, TrafficLight, PedestrianAgent ...
│       └── UI/Screens/...       new screens (Auction terminal, Workshop board, CRM, Accounts, Phone OS ...)
├── Core/                        CarSales.Core.asmdef      (NEW, no UI/package deps)
│   ├── Events/  Ledger/  Clock/  Config/  Region/  Save/  Services/  Random/
├── Sim/                         CarSales.Sim.asmdef       (NEW, refs Core)
│   ├── Vehicles/  Inspection/  Workshop/  Documents/  Market/  Auction/
│   ├── Customers/  Staff/  Business/  WorldSim/
├── Data/                        ScriptableObject assets (configs, catalogues, region profiles, personas)
│   ├── Config/  Catalogues/  Regions/  Personas/  PhysicsProfiles/  UI/
├── Studio/                      CarSales.Studio.Editor.asmdef (NEW, editor only)
│   ├── AssetStudio/  VehicleStudio/  WorldBuilder/  SimConfigurator/  Licensing/
├── Editor/                      CarSales.Editor.asmdef (v4 wizard + world generator)
│   └── Installer/               CarSales.Installer.asmdef
├── Tests/
│   ├── EditMode/                CarSales.Tests.EditMode.asmdef
│   └── PlayMode/                CarSales.Tests.PlayMode.asmdef
├── Art/                         imported assets (Vehicles, Buildings, Characters, Props, Interiors, Materials, HDRI)
├── Library/                     Asset Studio output: approved prefabs + AssetRecord + LicenceRecord
├── Input/
└── Generated/                   wizard output (placeholders, scenes)
```

### 4.1 Packages added in V5.0

| Package | Id | Used by | Phase |
|---|---|---|---|
| Newtonsoft Json | `com.unity.nuget.newtonsoft-json` | Save v5 (dictionaries, polymorphism, versioning) | 1 |
| Test Framework | `com.unity.test-framework` | All tests | 1 |
| glTFast | `com.unity.cloud.gltfast` | GLB/glTF import (Sketchfab downloads) | 3 |
| Splines | `com.unity.splines` | Roads, traffic lanes, auction lanes | 7 |
| Cinemachine | `com.unity.cinemachine` | Driving camera, cinematic menu, inspection camera | 2 |

The installer (`CarSalesDependencyInstaller`) gains these. We also start shipping **`Packages/manifest.json`**
so a fresh project gets the right packages without the dialog.

---

## 5. Fate of every v4 script

**Keep** = unchanged or trivial edits · **Modify** = same class, new behaviour · **Wrap** = becomes a thin adapter
over a new Sim service · **Retire** = kept compiling for old scenes, unused once its replacement passes its tests.

### Core
| Script | Fate | Notes |
|---|---|---|
| CarSalesServices | Wrap | Static façade forwarding to `ServiceRegistry`; adds new services. |
| CarSalesBootstrap | Modify | Defined init order: Core → Save load → Sim services → World → UI. |
| CarSalesInput | Modify | Driving actions: handbrake, gear up/down, horn, look back, camera switch, headlights. |
| GameSettings | Modify | Graphics quality, driving model (Arcade/Realistic), assists (ABS/TCS/ESC), units (mph/km/h). |
| ThumbnailService | Modify | Multi-angle photography for listings (front ¾, rear ¾, side, interior, engine bay). |
| GpsSystem | Modify | Routes along the road graph instead of straight-line arrow. |
| MapPlace, ServiceZone | Keep | Produced by World Builder `Site`s instead of hand-coded. |
| IInteractable, Billboard, RuntimeNavMeshBaker, ButtonSound, MenuShowcase, MinimapMarker | Keep | |

### Data
| Script | Fate | Notes |
|---|---|---|
| CarDefinition | Modify | Gains make/model/trim/year range, powertrain, physics profile ref, component platform, prefab ref, licence ref. Class name kept (R2). |
| CarDatabase | Modify | Registry of all `CarDefinition`s incl. Asset Studio imports; lookup by make/model/category. |
| CarInstance | Modify | Gains `VehicleIdentity`, `ComponentSet`, `DocumentSet`, `HistoryRecord`, `PlayerKnowledge`. Old fields remain as computed properties for old code. |
| CarParts | Wrap | The 6 v4 parts become *groups* computed from ~30 components (§6.3). Old API keeps working. |
| CustomerProfile | Modify | Built from a `BuyerPersona`; adds credit score, trade-in car, finance wish. |
| DailyReport | Modify | Generated from Ledger entries for the day. |
| SaveData | Retire | Replaced by Save v5 modules; v4 files migrated (§6.2). |
| PropertyInfo | Modify | Utilities, business rates, insurance, capacity per area, dealership level. |
| Enums, GameMessage, GameStats | Modify | New states/enums; stats move into the Ledger/Analytics. |

### Systems
| Script | Fate | Notes |
|---|---|---|
| EconomyManager | Wrap | `Spend/Earn` post to the Ledger. Cash = ledger balance. |
| InventoryManager | Modify | New vehicle states (§6.3.4), location ids, reservations. |
| CarValueCalculator | Wrap | Forwards to `ValuationService` (market index × condition × docs × mileage). |
| MarketplaceManager | Wrap | Becomes the *Online* channel of `SourcingService`. |
| AuctionManager | Retire | Replaced by `AuctionEngine` (§6.9). |
| TowService | Wrap | Becomes `TransportService` (timed jobs, own truck/trailer later). |
| TestDriveManager | Modify | Becomes the player vehicle session (enter/exit/park/fuel) and delegates driving to `IVehicleDriver`. |
| DayCycleManager | Modify | Uses `GameClock`; adds weekday/calendar, weekly wages, VAT quarters. Sleep/pass-out rules unchanged. |
| DayNightLighting | Modify | Weather (clouds, rain, fog, wet roads). |
| SaveManager | Modify | Uses Save v5 internals (atomic, backups, slots, migration). |
| BankManager | Modify | Loan products, overdraft, statements, credit rating. |
| MessageManager | Modify | Inbox for CRM, sellers, auction notices. |
| ProgressManager | Modify | Career levels (dealership levels 1–6), milestones. |
| PropertyManager, VehicleRegistry, UpgradeManager, AudioManager | Modify | Small extensions. |

### Vehicles / Dealership / Customers / Player / World
| Script | Fate | Notes |
|---|---|---|
| ArcadeCarController | Keep | Fallback "Arcade" handling option until the realistic controller is approved, then kept for low-end/accessibility. |
| CarDisplay | Modify | Interaction sockets: doors, bonnet, boot, driver seat, under-car (lift). |
| CarVisual | Modify | Works with imported models: paint material slots, dirt overlay, damage deformation. |
| DealershipManager | Modify | Dealership level, area types (forecourt/showroom/storage/workshop), staff slots. |
| ParkingSpot, DealerComputer, ShopToggleSign | Keep | |
| CustomerAI | Retire | Replaced by `BuyerBrain` + actions (§6.10). |
| CustomerSpawner | Modify | Spawn rate from marketing, reputation, weather, weekday; persona mix. |
| NegotiationSession | Wrap | Core maths moves to `Sim/Customers/Negotiation`; adds trade-in and finance. |
| SimpleFirstPersonController, PlayerInteractor, MenuInputBlocker | Keep | |
| DebugCheats | Modify | Developer console (spawn car, set time, add money, force fault). |
| HomeBed, PropertySign | Keep | |
| ServiceDesk, UsedCarMarket | Modify | Become generic `Site` interactions (§7.3). |

### UI
| Script | Fate | Notes |
|---|---|---|
| UIFactory | Modify | Colours/fonts/sizes move to a `UITheme` asset; new components (Table, Tabs, Modal, Badge, Stepper, Timeline). |
| UIPanel, UIManager | Modify | Screen stack, open/close transitions, back navigation, gamepad focus. |
| AuctionUI | Retire | Replaced by Auction Terminal (online) + Auction Hall HUD (physical). |
| PhoneUI | Retire | Replaced by Phone OS (§6.14.3). |
| InspectionUI, GarageUI | Retire | Replaced by Inspection Mode and Workshop Board. |
| All other screens | Modify | Restyled to the design system; data unchanged. |

### Editor
| Script | Fate | Notes |
|---|---|---|
| CarSalesSetupWizard | Modify | Still generates placeholders; also installs default configs/catalogues and opens the Studio. |
| CarSalesWorldBuilder | Modify | Its town becomes the default **World Layout** asset, editable in World Builder. |
| CarSalesDependencyInstaller | Modify | New packages (§4.1). |

---

## 6. Module specifications

### 6.1 Core

| Type | Responsibility |
|---|---|
| `EventBus` | `Subscribe<T>(Action<T>)`, `Unsubscribe`, `Publish<T>(T)`. Events are small structs. No string-keyed events. |
| `ServiceRegistry` | `Register<T>(T)`, `Get<T>()`, `TryGet<T>()`. Reset on scene unload. |
| `GameClock` | Game time (`Day`, `Hour`, `Weekday`, `Week`, `Quarter`), speed, pause; fires `HourPassed`, `DayStarted`, `WeekEnded`, `QuarterEnded`. Wraps DayCycleManager's time. |
| `Ledger` | Append-only list of `Transaction { id, day, hour, amount, category, accountFrom, accountTo, vatAmount, vatScheme, refType, refId, memo }`. Balances, filters, P&L. |
| `ConfigRegistry` | Loads all `ConfigAsset`s (economy, auction, customers, staff, weather...) and exposes them by type. |
| `RegionProfile` (SO) | Currency (£), number/date format, units, VAT rate, MOT rules (first test at 3 years, yearly), plate format, write-off categories, typical fees. |
| `SimRandom` | Seeded RNG per subsystem (deterministic tests, reproducible bug reports). |

**Ledger categories** (enum `TxCategory`): VehiclePurchase, VehicleSale, AuctionBuyerFee, AuctionSellerFee, StorageFee,
Transport, Parts, Labour, Detailing, Fuel, Documents (history checks, MOT, V5C), Advertising, Wages, Rent, Utilities,
BusinessRates, Insurance, InsuranceClaim, LoanDrawdown, LoanRepayment, LoanInterest, FinanceCommission, VAT,
PropertyPurchase, PropertyIncome, Fines, Upgrades, Other.

**Core events (initial set):** `MoneyChanged`, `TransactionPosted`, `VehicleAcquired`, `VehicleStateChanged`,
`VehicleSold`, `VehicleMoved`, `InspectionCompleted`, `FaultDiscovered`, `JobQueued`, `JobCompleted`, `LotOpened`,
`BidPlaced`, `Outbid`, `LotSold`, `LotUnsold`, `InvoiceIssued`, `InvoiceOverdue`, `CustomerArrived`,
`CustomerLeft`, `ReviewPosted`, `EmployeeHired`, `WeatherChanged`, `DayEnded`, `GameSaved`.

### 6.2 Save system

**File layout** (`Application.persistentDataPath/saves/`):
```
slot_1/
  save.json        ← current
  save.json.bak1   ← previous (rotated, keep 3)
  save.json.bak2
  save.json.bak3
  meta.json        ← small: day, cash, playtime, screenshot path (for the load menu)
  thumb.png
```

**Envelope:**
```json
{ "format": "CSS-SAVE", "version": 5, "gameVersion": "5.0.0", "savedAtUtc": "...",
  "checksum": "sha256 of payload", "modules": { "economy": {...}, "inventory": {...}, "auction": {...} } }
```

| Feature | Spec |
|---|---|
| Modules | Each system implements `ISaveModule { string Key; int Version; JToken Save(); void Load(JToken, int version); }`. A missing module loads defaults (new systems never break old saves). |
| Atomic write | Serialise → write `save.json.tmp` → flush → rotate backups → `File.Replace(tmp, save.json, bak1)`. A crash mid-write never leaves a half file. |
| Validation | Envelope format, checksum, per-module schema checks (e.g. no negative fuel, unique VINs, car ids exist). Invalid values are clamped and logged. |
| Recovery | Load fails → try bak1 → bak2 → bak3 → tell the player which backup loaded and the date. |
| Migration | `ISaveMigration { int From; JObject Migrate(JObject) }` chain. **v4 → v5** reads the old `SaveData` JSON: converts `parts[6]` into components, generates VIN/registration/documents for existing cars, cash → opening ledger balance. |
| Autosave | On sleep (as v4), every 15 game-hours, before entering an auction, on quit. Never while a menu transaction is half-done (`SaveBlocker` scopes). |
| Slots | 3 manual slots + 1 autosave slot. |
| Tests | Round-trip every module; migrate a fixture v4 save; corrupt file → backup loads; interrupted write simulation. |

### 6.3 Vehicle data model

```
CarInstance (existing class, extended)
├── VehicleIdentity    vin, registration, year, firstRegistered, colourName, definitionId, trim
├── ComponentSet       Dictionary<ComponentId, ComponentState>
├── PowertrainState    fuelLevel / batteryCharge, odometer, engineHours, oilLife
├── DocumentSet        v5c, motCertificate, serviceBook, keysCount, manuals (§6.7)
├── HistoryRecord      owners[], mileageReadings[], incidents[], writeOff, financeOutstanding, stolen, imported
├── PlayerKnowledge    known facts per component/document + confidence (§6.5)
├── Commercial         purchasePrice, costs (ledger refs), listedPrice, listing, reservations
└── Location           LocationId (dealership spot / workshop bay / auction yard / world pos / in transit)
```

#### 6.3.1 Component catalogue (V5.0: 30 components, 10 groups)

| Group (v4 part it feeds) | Components |
|---|---|
| Engine (Engine) | Engine block/internals, Cylinder head & gasket, Timing belt/chain, Oil & filter, Cooling system/radiator, Ignition/glow plugs, Intake & turbo |
| Transmission (Engine) | Gearbox, Clutch (manual) / Torque converter (auto), Driveshafts & CV joints |
| Battery & charging (Electronics) | 12V battery, Alternator, Starter motor |
| Electronics (Electronics) | ECU & sensors, Lights, Infotainment & dash, Wiring |
| Brakes (Brakes) | Pads (front/rear), Discs (front/rear), Brake fluid & lines |
| Suspension & steering (Brakes) | Shock absorbers, Springs, Bushes & arms, Steering rack, Alignment |
| Tyres (Tires) | 4 × tyre (tread mm, age, damage), Spare/repair kit |
| Exhaust (Engine) | Exhaust system, Catalytic converter / DPF |
| Body (Body) | Panels (bonnet, boot, roof, 4 doors, 2 wings, 2 quarters, 2 bumpers), Paint, Windscreen & glass, Rust |
| Interior (Interior) | Seats, Dashboard & trim, Carpets & headliner, Odour/cleanliness |

EVs and hybrids swap engine components for: Traction battery (state of health %), Electric motor, Inverter,
Charging port/on-board charger, Thermal management. Each component definition (`ComponentDefinition` SO) has:
`id, group, weightInValue, applicablePowertrains, wearPerKm, wearPerYear, faultTable, repairJobs`.

#### 6.3.2 Component state
```
ComponentState { health 0–100, wear 0–100, faults: List<FaultInstance>, lastServicedKm, replacedOnDay, partQuality }
FaultInstance  { faultId, severity 0–1, discovered=false (truth only), progressPerDay }
```

#### 6.3.3 Fault catalogue (`FaultDefinition` SO), first 40 faults

Each fault: `id, component, symptom text, DTC code (optional), visibility mask, severity range, value impact,
progression, safety-critical (MOT fail), repair jobs that fix it`.

**Visibility mask** decides which inspection finds it: `Visual | Interior | UnderBonnet | Scanner | RoadTest | Lift | History`.
Example rows:

| Fault | Component | Found by | DTC | MOT |
|---|---|---|---|---|
| Worn front pads | Pads F | Lift, RoadTest (squeal) | — | fail < 1.5 mm |
| Misfire cylinder 2 | Ignition | Scanner, RoadTest | P0302 | — |
| Head gasket leak | Cylinder head | UnderBonnet (mayo cap), RoadTest (overheat) | — | — |
| Clutch slipping | Clutch | RoadTest | — | — |
| Clocked mileage | (history) | History, Interior (wear vs. odometer) | — | — |
| Previous Cat N repair | (history) | History, Visual (paint mismatch) | — | — |
| Corroded sill | Rust | Lift | — | fail |
| Battery weak | Battery | Scanner, won't start in cold | — | — |

#### 6.3.4 Vehicle states
`Listed (marketplace/auction, not owned)` → `Purchased/AwaitingPayment` → `AwaitingCollection` → `InTransit` →
`InStock` → `InWorkshop` / `InDetailing` / `InMOT` → `ReadyForSale` → `Listed (forecourt/online)` →
`Reserved (deposit)` → `Sold/AwaitingHandover` → `Delivered`. Also `Personal`, `Scrapped`.

### 6.4 Vehicle physics (Vehicle Framework)

| Part | Spec |
|---|---|
| `IVehicleDriver` | Interface both controllers implement: `SetInput(throttle, brake, steer, handbrake)`, `Speed`, `Rpm`, `Gear`, `IsGrounded`, `OnImpact`. TestDriveManager talks only to this. |
| `WheelVehicleController` | `Rigidbody` + 4× `WheelCollider` (extensible to 6). Centre of mass from profile. Visual wheels follow `GetWorldPose`. |
| `EngineModel` | Torque curve (AnimationCurve, Nm vs rpm), idle/redline, inertia, engine braking; health scales torque (misfire = torque dips). |
| `GearboxModel` | Automatic (default): ratios, final drive, up/down shift rpm, shift time, kick-down, reverse/neutral/park. Manual mode optional. Clutch health → slip. |
| `ElectricDriveModel` | Flat torque to base speed, power limit, single ratio, regen braking, battery kWh → range. |
| Brakes | Front/rear bias, brake torque from health; **ABS** (release when wheel slip > threshold), handbrake on rear axle. |
| Assists | **TCS** (cut torque on drive-wheel slip), **ESC** (light yaw correction brake), speed-sensitive steering, anti-roll bars per axle. All toggleable. |
| Tyres | Forward/sideways friction curves from profile × tread × weather wetness × surface. |
| Suspension | Spring/damper/travel from profile × shock health (worn shocks = bouncy). |
| Damage | `DamageReceiver`: collision impulse + contact point → zone (front/rear/left/right/roof) → panel health, paint, and chance to damage radiator/suspension/steering alignment. Optional mesh deformation (vertex push, capped). |
| Fuel/energy | Consumption from engine load (BSFC-lite): L/100km at cruise from the definition × load factor. EV kWh/100km. |
| Profiles | `VehiclePhysicsProfile` SO per class: City hatch, Family hatch, Saloon, Estate, SUV, Pickup, Van, Sports, Luxury, EV hatch. |
| Camera | Cinemachine chase + bonnet + interior camera; look-back. |
| Audio | Engine loop pitched by rpm/load (2–4 layers), tyre skid, impact, indicator, horn. |

**Arcade fallback:** `GameSettings.drivingModel = Arcade | Realistic`. If a car has no wheel setup (e.g. an
unprocessed import), it falls back to Arcade automatically.

### 6.5 Inspection and knowledge

`PlayerKnowledge` stores, per component: `knownHealthBand (unknown / estimate ±x / exact)`, `knownFaults`, and per
document `known/unknown/verified`. UI always shows the **knowledge**, never the truth.

| Method | Where | Time | Cost | Reveals |
|---|---|---|---|---|
| Walk-around (visual) | anywhere | 2 min | free | Visual faults, panel/paint damage, tyre tread estimate |
| Interior check | sitting in car | 2 min | free | Interior, warning lights, odometer vs. wear |
| Under-bonnet | bonnet open | 3 min | free | Leaks, belts, fluid levels, mayonnaise oil cap |
| OBD scanner | needs scanner tool (upgrade) | 1 min | free after purchase | Stored DTCs → Scanner faults |
| Road test | drive it | as long as you drive | fuel | RoadTest faults (noises, vibration, slipping, pulling) — **only noticed when the condition happens** (e.g. clutch slip under hard throttle in high gear) |
| Lift inspection | workshop | 15 min | free in own workshop | Brakes, suspension, exhaust, rust, underbody |
| History check (HPI-style) | phone/computer | instant | £15 | Write-off cat., outstanding finance, stolen, mileage anomalies, plate changes |
| Full inspection | workshop staff | 1 h | £120 (or staff time) | Everything except intermittent faults |

**Accuracy:** the player's/employee's *inspection skill* sets how exact the estimate is and the chance to
notice a marginal fault. Auction condition reports are another knowledge source with their own reliability (§6.9).

**Inspection Mode (UI/gameplay):** in first person, the car shows interaction sockets (door, bonnet, boot, wheels,
dash). Each completed check ticks a checklist and writes knowledge. Mouse-over on a revealed fault shows
symptom + estimated repair cost.

### 6.6 Workshop, parts and repairs

| Type | Spec |
|---|---|
| `PartDefinition` SO | id, name, category, fits (component id + platform/category), quality tiers: **Used** (cheap, 50–80% health), **Aftermarket**, **OEM** (best); price per tier; supplier lead time. |
| `PartsInventory` | Stock per part/tier, location (workshop storage), reorder. |
| Suppliers | Local motor factor (same day, normal price), online (next morning, −15%), breaker's yard (used parts, random stock). |
| `RepairJobDefinition` SO | id, fixes (component/faults), required parts, labour hours, station, skill, result health per part tier. E.g. *Replace front pads & discs*: 2 pads, 2 discs, 1.2 h, Mechanical bay. |
| Stations | Mechanical bay (lift), Diagnostic bay, Tyre machine + balancer, Alignment rig, Body bay, Paint booth, Detailing bay, MOT bay. Each is a `WorkStation` in a Site; owned via dealership upgrades. |
| `WorkshopService` | Job queue per station; jobs advance with `GameClock` × worker skill; parts reserved on start; ledger posts Parts + Labour (labour = staff wage share, or external garage rate £65/h). |
| Doing it yourself | Player can perform a job at a station: short interaction (hold to unbolt/fit sequence) instead of waiting — faster, free labour, small fail chance at low skill. |
| External garage | Town Mechanic Workshop (v4) remains: pay retail rate, no parts needed. |
| Results | Component health/faults updated → value recalculated → `JobCompleted` event; service book entry written. |

### 6.7 Documents and history (UK profile)

| Document | Data | Effect |
|---|---|---|
| **V5C logbook** | keeper count, registered keeper, present yes/no | Missing V5C: −25% value, some buyers refuse. Apply for replacement: £25, arrives in 5 days. |
| **MOT** | expiry date, mileage at test, advisories, fails | Cars > 3 years need MOT. Expired MOT: can't be test-driven on road by customers; −10% value. MOT test at MOT bay or town garage (£54.85 cap in UK; configurable). |
| **MOT history** | list of past tests with mileage | Reveals mileage anomalies and recurring problems. |
| **Service history** | stamps (full/partial/none), last service km | Full history +5–8% value, persona-dependent. |
| **Keys** | count (0–2) | 1 key −2%; 0 keys: can't drive; locksmith job. |
| **Vehicle tax** | simulated status only | Must be taxed to drive on public roads (simple on/off fee per day of use). |
| **History check flags** | Write-off **Cat A/B/S/N**, outstanding finance, stolen, imported, plate change | Cat S −30–40%, Cat N −15–25%. Outstanding finance: car can be repossessed (loss!) unless settled. Stolen: police seize (loss). |
| **Paperwork on sale** | invoice, V5C transfer, warranty (optional), history pack | Generated as in-game documents; completeness affects review score. |

Plates and VINs: generated with the region's format (UK `AB12 CDE` age identifier from first-registration date;
17-char VIN with valid check digit style). Unique across the save.

### 6.8 Market model and sourcing

**MarketModel** (one per save):
- Price index per `CarDefinition` (and per category) starting at its base value, random walk ±0.5%/day,
  mean-reverting, plus **seasonal** factors (convertibles +8% in May–Aug, 4×4/SUV +5% in Nov–Feb), fuel-price effect
  (EV/hybrid demand), and **supply/demand**: every sale in town lowers demand briefly; low supply raises prices.
- Competitor dealers (off-screen) buy from the same pool and list at their own margin, so player prices are
  compared against them.

**Sourcing channels** (`SourcingService`):

| Channel | Price vs. market | Risk | Process |
|---|---|---|---|
| Private sellers (classifieds) | −10 to −25% | Medium (lies, hidden faults, outstanding finance) | Ad → message/call → appointment at a house in town (seller NPC + car spawned there) → inspect/test drive → haggle → pay → V5C handover → drive/tow it away |
| Online marketplace (v4) | −5 to −15% | Medium | Listing → seller report → buy → delivery fee/collection |
| Dealer trade / wholesale | −15 to −30% | Low–medium | Weekly offers of 3–8 cars, bulk discount, must take the batch |
| Trade-ins | negotiated | Low (you inspect it on the forecourt) | Customer offers their car as part payment (§6.10) |
| Dealer auctions | −15 to −35% | Medium | §6.9 |
| Salvage auctions | −40 to −80% | High (Cat S/N, non-runners, flood/fire) | §6.9 |
| Fleet/lease returns | −10 to −20% | Low (full history, high mileage) | Auction lots in batches |
| Repossessions | −20 to −35% | Medium (no keys/V5C sometimes) | Auction lots |

### 6.9 Auction engine

One engine (`AuctionEngine`, Sim layer) drives both the **physical auction hall** and the **online terminal**.
UI layers only display it and send bids.

**Entities**
```
AuctionVenue    id, siteId (physical yard) or online, buyer fee table, storage fee/day, collection days
AuctionEvent    id, venue, type (Dealer|Salvage|Fleet|Online timed), date, start hour, lots[]
Lot             lotNo, vehicle (CarInstance in Listed state), reserve (hidden), startPrice, conditionReport,
                keys, starts, drives, category, damage text, MOT, V5C present, buyNow (optional), status
ConditionReport grade 1–5, listed damage, mileage (claimed), notes; reliability depends on venue type
BuyerAccount    buyerNo, registered venues, deposit held, limit, invoices, watchlist, stats
Invoice         lot, hammer, buyer premium, fixed fees, VAT on fees, total, dueDay, paid, collection deadline
Bidder (AI)     archetype, budget, target categories, margin, risk appetite, max lots/day
```

**Event flow**
1. Calendar: venues publish events 3–7 days ahead (phone/computer **Auction calendar**). Physical yard opens viewing
   the day before and on the morning.
2. **Register** (once per venue): pay a refundable deposit (e.g. £500), receive a **buyer number**.
3. **Browse catalogue / watchlist**; walk the yard: each lot is a physical car on a numbered bay with its
   condition report on the windscreen. Normal inspection methods apply (scanner, under-bonnet...), **no road test** (yard only lets you start the engine if `starts`).
4. **Set a max bid** (proxy bidding for online/absent) or bid live.
5. **Live sale** (physical): lots run through the hall in order; auctioneer calls "starting at...", increments,
   "going once / going twice / sold" with a short timer (e.g. 4 s per call, resets on each bid). Player bids
   by raising the paddle (E) in the hall. Reserve not met → "provisional bid" → seller accepts/declines within the day.
6. **Online timed**: lots end at staggered times; a bid in the last 2 minutes extends the lot by 2 minutes (soft close).
7. **Win** → `Invoice` issued. Payment due in 2 days (ledger posts VehiclePurchase + AuctionBuyerFee + VAT on fee).
   Overdue → £50/day + reputation loss; 5 days → sale cancelled, deposit kept.
8. **Collection**: free for 3 days, then storage £15/day. Collect by driving it (if drives + taxed), tow truck,
   or your own transporter (staff driver). Vehicle state: `AwaitingCollection` at the venue.

**Bid increments (configurable):** <£1,000: £50 · £1,000–4,999: £100 · £5,000–9,999: £250 · £10,000+: £500.

**Buyer premium (example dealer auction):** tiered table, e.g. £0–999 → £150; £1,000–2,999 → £250; £3,000–4,999 → £350;
… plus £20 internet fee for online bids. VAT 20% is charged on fees.

**AI bidder maximum** (§8.3) uses expected resale, estimated repairs (from the condition report, with archetype-specific
optimism), fees, transport and target margin, plus noise. Archetypes: Private buyer, Small trader, Franchise dealer
(only good grades), Exporter (likes Japanese/4×4, high mileage OK), Scrap/breaker (salvage value floor), Mechanic
(likes mechanical faults, avoids bodywork), Large dealer group (bulk, low margins).

**Risks built in:** report omits a fault (lower reliability at salvage auctions), "keys: 1" turns out missing,
non-runner that only needs a battery (opportunity!), mileage "unverified", outstanding finance cleared by the
auction (dealer auctions guarantee title, salvage auctions don't).

### 6.10 Customers and sales

**BuyerPersona** (SO): budget range, category preferences, weights for
`price, condition, mileage, brand, age, history, economy, features, docs`, knowledge level (how well they spot
faults), haggling skill, patience, trade-in chance, finance chance, review harshness.

Initial personas: Budget hunter, First-time buyer, Family buyer, Commuter (economy/EV), Enthusiast (sports, knows
mechanics), Luxury buyer, Mechanically savvy haggler, Impatient buyer, Trader (buys to resell, lowballs).

**Car scoring** (utility, §8.4): each customer scores every listed car they look at; they go to the best ones,
inspect, maybe compare 2–3, then talk to a salesperson (player or staff).

**Behaviour** (`BuyerBrain`, utility-driven state machine replacing the v4 coroutine FSM):
Arrive (parking/walk-in) → Browse (walk between cars, look) → Inspect (open door/bonnet, sit inside, kneel at tyres —
animation slots on `CarDisplay` sockets) → Compare → Ask (dialogue: mileage? history? MOT? last service? "any
problems?" — lying is possible and can come back as a bad review) → Test drive request → Negotiate (with trade-in /
finance options) → Paperwork & deposit → Collection (same day or reserved for a later day) → Review → Leave.

**Customer test drives:** requires valid MOT/tax and insurance (trade plates upgrade). Options: player rides along
(customer drives on a route via `TrafficLane` waypoints) or employee goes. Faults with RoadTest visibility may
be noticed by the customer → negotiation penalty.

**Trade-in:** customer's car is generated (persona-appropriate), you inspect it on the forecourt, the trade-in value
is part of the deal. **Finance:** customer applies; approval chance from credit score and deposit; dealer earns a
commission (ledger: FinanceCommission); the lender pays the dealer.

**After-sale:** warranty claims (optional 3-month warranty sold as add-on), complaints if a hidden fault you knew about
appears (rep hit), returns within a configurable window.

**CRM:** persistent `CustomerRecord` (name, persona, contact, visits, cars viewed, purchases, satisfaction, leads).
Leads come from online listings and walk-ins; follow-ups by phone can bring them back. Repeat customers return
when their car "ages out" (simulated).

### 6.11 Staff and jobs

| Role | Does | Key skill |
|---|---|---|
| Salesperson | Greets customers, answers questions, negotiates within limits you set (min margin %) | Sales, Negotiation |
| Mechanic | Takes jobs from the workshop board | Mechanical, Diagnostics, Bodywork |
| Detailer | Washing, valeting, paint correction | Detailing |
| Driver | Collections, deliveries, test-drive escort | Driving |
| Receptionist | Raises customer spawn conversion, handles calls/CRM | Admin |
| Manager | Bonus to all staff morale/productivity at one site | Management |

Each employee: skills (0–100), experience (grows), wage (£/week), shift pattern, morale, reliability (sick days).
**Job board** (`JobSystem`): every task is a `Job { type, site, requirements, duration, priority }`; staff pick the
highest-priority job they're qualified for. Player can assign manually. Wages paid on `WeekEnded` via the Ledger.
Hiring through a recruitment page with 3–6 candidates per week.

### 6.12 Business and finance

| System | V5.0 scope |
|---|---|
| Accounts | P&L (day/week/quarter), cash flow, per-car profit (purchase + all costs vs. sale), stock value, ageing stock (days in stock). |
| VAT | Configurable: **Margin scheme** for used cars (VAT = margin × 1/6 at 20%), standard VAT on fees/parts/labour. Quarterly VAT return on `QuarterEnded`; late payment fine. |
| Bank | Business current account, overdraft (limit by credit rating), loans (term, APR, monthly repayment), statements. |
| Insurance | Business policy (premises, stock, public liability, trade plates/motor trade). Premium by stock value, claims history, security level. Events: theft, vandalism, forecourt damage → claim minus excess. |
| Running costs | Rent, utilities (per site size), business rates, cleaning, security (CCTV/alarm upgrades lower theft risk). |
| Marketing | Campaigns (local paper, online ads, social, signage, open day): cost per day, effect on footfall and persona mix, diminishing returns. Listing quality (photos, description, price vs. market) affects online leads. |
| Reputation | Review stars (average of recent reviews) + legacy reputation number; affects footfall and prices customers accept. |
| Competitors | 3–5 named off-screen dealers with stock, pricing style and reputation; appear in a **Competitor research** page (their listings vs. yours). |
| Dealership levels | 1 Small forecourt → 2 Small showroom → 3 Medium dealership → 4 Large with workshop → 5 Premium → 6 Multi-site group. Unlocks: capacity, staff slots, stations, personas (luxury buyers need level 5 or the Luxury Lane site). |
| Analytics | Sales per persona/category, average margin, days to sell, lead conversion. |

### 6.13 World simulation

| System | V5.0 scope |
|---|---|
| Calendar | Weekdays (Sunday shorter hours/quieter), seasons, bank holidays (busy). |
| Weather | States: Clear, Cloudy, Overcast, Rain, Heavy rain, Fog; Markov transitions per hour; affects lighting, wet roads (grip, puddle shader), customer footfall (−30% in rain), car wash demand. Snow out of scope for V5.0. |
| Fuel prices | Daily random walk; affects fuel cost and EV demand. |
| Traffic | Spline **lane graph** from World Builder; `TrafficCar` follows lanes with gap keeping, stops at lights/junction give-way, speed limits; density by time of day; despawn out of view. Collision → stops, hazard lights. |
| Pedestrians | NavMesh agents walking between sidewalk points and shop entrances; some become dealership customers. |
| Random events | Police checks (no tax/MOT fine), breakdowns of your stock (if a fault progresses), road works, open days, theft attempts. |

### 6.14 UI design system

#### 6.14.1 Foundations
- `UITheme` SO: colour tokens (bg, surface 1–3, primary, accent, success, warning, danger, text 1–3, outline),
  font assets (heading/body/mono numbers), type scale (12/14/16/20/24/32/48), spacing scale (4/8/12/16/24/32),
  corner radii, shadow, motion durations (fast 120 ms, normal 200 ms, slow 320 ms), light/dark variants.
- Keep **code-built uGUI** (it works and avoids broken references), but every value comes from the theme.
- Component library (in `UIFactory`): Button (primary/secondary/ghost/danger, icon), Card, Table (sortable,
  virtualised rows for big lists), Tabs, Segmented control, Modal, Drawer, Toast, Badge/Chip, Field/Input/Dropdown,
  Slider, Stepper/progress checklist, Timeline, Stat tile, Sparkline/mini chart, Empty state, Skeleton loader.
- Screen stack with transitions (fade+slide), Back (Esc / gamepad B), gamepad/keyboard focus navigation,
  UI scale setting (80–130%), colour-blind-safe status colours (icons + text, never colour alone).

#### 6.14.2 Screens (new or redesigned)
Main menu (cinematic camera over the town, save slots with thumbnails) · HUD (minimal: clock/weather, cash,
GPS strip, speed/gear/fuel when driving, contextual prompts) · Dealer Computer desktop (apps: Stock, Marketplace,
Auctions, Workshop, CRM, Accounts, Bank, Staff, Marketing, Competitors, Real Estate, Settings) · Vehicle file
(tabs: Overview, Condition, Documents, History, Costs, Listing) · Inspection mode overlay · Workshop board
(kanban per station) · Parts shop · Auction terminal (calendar, catalogue, live lot, watchlist, invoices) ·
Auction hall HUD (current lot, bid, increments, paddle prompt, timer) · Listing editor (photos, price helper vs.
market & competitors, description) · Negotiation 2.0 (offer slider, trade-in card, finance card, customer mood) ·
Paperwork/handover · CRM · Accounts/P&L · Staff · Marketing · Map (full screen, filters, routes) · Phone OS.

#### 6.14.3 Phone OS
Lock/home screen with app grid, notifications, status bar (time, battery-style energy). Apps: Messages, Calls,
Auctions, Marketplace, Bank, Map/GPS, Tow & Transport, Taxi, Calendar, Contacts (CRM), Camera (photograph cars for
listings), Settings. Opens with P; usable while walking (not while driving except hands-free calls).

### 6.15 Audio
Mixer groups (Master/Music/SFX/Ambience/Vehicles/UI/Voice), clip library SO with replaceable slots (procedural clips
from v4 stay as fallbacks), vehicle engine layers, ambience by zone (street, showroom, workshop, auction hall with
auctioneer chant loop), rain, UI sounds.

---

## 7. Editor tools

All tools live under **Tools → Car Sales Studio** and share one `StudioDatabase` (asset records, licences, templates).

### 7.1 Aurex Asset Studio

**Sources**

| Source | How | Notes |
|---|---|---|
| Local file / folder | Drag & drop or file picker | FBX, OBJ, GLB/glTF (glTFast), DAE, .blend (needs Blender installed — Unity converts it), textures, HDRI |
| Drop folder | `Assets/CarSales/Art/_Drop/` watched by an `AssetPostprocessor` | For KitBash3D, Fab, CGTrader, Quixel/Megascans, TurboSquid downloads you make yourself |
| Unity Asset Store | Install via Package Manager → **Scan packages** button lists new prefabs/models | |
| Sketchfab | Official Data API (search) + Download API (glTF/GLB zip), with **your** API token (stored in EditorPrefs, never in the project) | Licence and author read from the API; only downloadable models; attribution saved |
| Poly Haven | Public API for HDRIs, textures, models (CC0) | Check its API usage terms before shipping |

**Pipeline** (each step has a result panel and can be re-run):
1. **Ingest** → copies into `Art/<Category>/<AssetName>/Source/`, creates `AssetRecord` + `LicenceRecord` (licence
   step can't be skipped; *Unknown* is allowed for prototyping but flagged).
2. **Analyse** → bounds, likely unit scale (compares bounds with the expected size of the chosen category, e.g. car
   length 3.5–5.5 m, door height ~2.1 m), up/forward axis, mesh/sub-mesh/tri counts, materials, missing textures,
   rig/animations, existing LODs, duplicate check (hash of mesh data).
3. **Normalise** → import settings (scale factor, axis conversion, read/write only when needed), wrapper root
   with pivot at ground centre, +Z forward.
4. **Materials** → remap to URP Lit (base/normal/metal-smoothness/AO/emission channels by name heuristics:
   `_albedo, _basecolor, _n, _normal, _rough, _metal, _ao`), texture max-size and compression presets, flags
   missing textures.
5. **Colliders** → box / compound boxes / convex hulls / mesh collider (static only) by category.
6. **LODs** → existing LODs → LODGroup; else Unity Mesh LOD (if available) or simplifier; screen-size thresholds by category.
7. **Classify & integrate** → category templates:
   - **Vehicle:** wheel detection (name regex `wheel|tyre|tire|rim` + `fl/fr/rl/rr|front|rear|left|right`, then
     geometry heuristic: four similar near-cylindrical meshes near the bottom corners), door/bonnet/boot detection
     by name; **manual correction screen** (click mesh → assign role) when detection is unsure. Generates:
     prefab with `Rigidbody`, `WheelVehicleController`, `WheelCollider`s sized from the wheel meshes, `CarVisual`
     paint slots (choose which materials are paint), interaction sockets, lights, exhaust/audio points, damage
     zones, and a `CarDefinition` draft for Vehicle Studio.
   - **Building:** choose function (Dealership, Workshop, Auction hall, Car wash, Fuel station, Home, Office, Shop,
     Decorative); mark entrances, floors (NavMesh walkable), interior volumes; adds `Site` + `SiteEntrance` +
     NavMesh modifiers; lighting probes.
   - **Character:** humanoid avatar check, assigns shared animator controller (idle/walk/talk/inspect/sit), capsule
     and NavMeshAgent → customer/staff/pedestrian template.
   - **Prop / Interior:** colliders, static flags, optional `IInteractable` template (chair, desk, computer, lift, tool chest).
8. **Preview & validate** → opens a preview scene (turntable, scale reference human + car, driving pad for vehicles,
   walk-through for buildings). Validation checklist must pass: scale, colliders, materials, LODs, licence, tri budget.
9. **Approve** → prefab saved to `Library/<Category>/`, registered in `AssetRegistry`; rollback keeps the previous version.

### 7.2 Vehicle Studio
Edit a `CarDefinition`: identity (make/model/trims/years), price & market data, powertrain (engine curve editor,
gearbox ratios, EV battery), physics profile + overrides, component platform, fault tendencies ("this model's
known weak spots"), paint colours, prefab. **Test track** scene button: drive it immediately with telemetry
(speed, rpm, gear, slip per wheel, g-forces), 0–100 timer, braking-distance test.

### 7.3 World Builder
- **Layouts:** a `WorldLayout` asset lists placed sites, roads and props. v4's generated town becomes `Layout_DefaultTown`.
- **Sites:** templates (Small/Large dealership, Auction warehouse, Auction yard, Mechanic garage, Car wash, Fuel
  station, Transport depot, House, Apartment block, Office, Shop). A Site = building prefab + `Site` component with
  function, slots (parking spots, work stations, auction bays, customer spawns, staff spawns), entrance, map icon.
  Swapping the building model keeps the slots.
- **Validation:** each site has an entrance reachable on the NavMesh, a road connection, required slots, a map
  place, and a service zone → otherwise red warnings.
- **Roads:** spline roads (Splines package) with lane count, width, markings, pavements; junction pieces; automatic
  lane graph for traffic; traffic lights/signs placement.
- **Scatter:** trees, lamps, street furniture along splines/areas.
- **Bake:** NavMesh per area, light probes, minimap render, occlusion.

### 7.4 Simulation Configurator
Inspector-style window over all `ConfigAsset`s (economy, market, auction fees, bidder archetypes, personas, staff,
weather, region) with **presets** (Easy/Normal/Hard/Realistic), diff view vs. defaults, and a **sandbox simulator**
(run 30 game-days headless with a scripted player strategy and print P&L — catches broken balance quickly).

---

## 8. Data formulas

All constants below are defaults stored in config assets.

### 8.1 Valuation
```
base        = MarketModel.Price(definition)                      // market value of an average example
ageFactor   = 0.85 ^ (age years) (floored at classic curve for rare models)
mileFactor  = clamp(1 - (mileage - expectedMileage) * 0.000004, 0.55, 1.15)   // expected = 12k/yr
condFactor  = Σ(group weight × groupHealth/100) mapped 0.35..1.05
docFactor   = (V5C ? 1 : 0.75) × (MOT valid ? 1 : 0.90) × serviceHistory(1.06 full / 1.00 partial / 0.95 none) × keys
historyFactor = writeOff (Cat S 0.65, Cat N 0.80, Cat A/B 0) × (mileage anomaly ? 0.80 : 1)
retailValue = base × ageFactor × mileFactor × condFactor × docFactor × historyFactor
tradeValue  = retailValue × 0.78                                  // what dealers pay
```
`CarValueCalculator.MarketValue` returns `retailValue` (v4 callers unchanged).

### 8.2 Repair estimate
`Σ over known faults: parts(tier) + labourHours × rate`; shown as a range when knowledge is an estimate (±25%).

### 8.3 AI auction max bid
```
expectedResale = retail value the bidder *believes* (condition report × archetype optimism)
repairs        = estimated repairs × (1 + riskAppetite × uncertainty)
fees           = buyerPremium(hammer) + transport
maxBid         = (expectedResale × (1 - targetMargin) - repairs - fees) × noise(0.92..1.08)
stop bidding when nextBid > maxBid or budget exhausted
```

### 8.4 Customer car score
```
priceScore   = 1 - clamp((price - budget*0.9) / (budget*0.3), 0, 1)
valueScore   = clamp(retailValue / price, 0.7, 1.3) remapped 0..1    // a good deal vs. market/competitors
score = Σ weight_i × score_i   (price, value, condition-as-perceived, mileage, brand, age, history, economy, docs)
       × categoryPreference × reputationFactor
buy intent if score > persona threshold; walks away if best score < leave threshold
```

### 8.5 Negotiation (extends v4)
Customer target price = `perceivedValue × (1 - haggleSkill × 0.12)`; first offer below target; patience
decreases with each counter and faster for big gaps; trade-in and finance add value terms; insults (offer >25%
above perceived value) cost 2 patience. Salesperson skill raises the customer's ceiling slightly.

### 8.6 Customer footfall per hour
```
base(site level) × reputationFactor(0.5..1.6) × marketing × weather × weekday × hourCurve × (listedCars>0)
```

---

## 9. Implementation phases

Size: S ≈ 1 session, M ≈ 2–4, L ≈ 5–8, XL ≈ 9+. Each phase ends with: compile check ✅, tests ✅, your Unity
play-test checklist ✅, docs/CHANGELOG updated, commit tagged.

### Phase 0 — Stabilise v4 in the real Unity Editor (S, **needs you**)
1. Create a clean Unity 6 LTS URP project; copy Assets; let the installer add packages; run *Build Everything*.
2. Send me the full Console after import, after Build Everything, and after 10 minutes of play.
3. I fix every compile/runtime error and layout problem you report.
4. I add `Packages/manifest.json` and a minimal project template so the folder opens as a complete project.
5. Play-test checklist v4: walk, drive own car, buy online/used lot/auction, repair, wash, refuel, tow, taxi,
   park on a spot, list, sell to a customer, sleep, buy house, buy dealership, save/load.
6. Push to GitHub once the repo exists.
**Done when:** the checklist passes with no Console errors, and the project is on GitHub.

### Phase 1 — Core, Save v5, tests (M)
1. Create `CarSales.Core`, `CarSales.Sim`, test assemblies; add Newtonsoft + Test Framework to installer.
2. EventBus, ServiceRegistry, GameClock (wrapping DayCycle), SimRandom, ConfigRegistry, RegionProfile (UK).
3. Ledger + `TxCategory`; EconomyManager posts every Spend/Earn to it; DailyReport built from the ledger.
4. Save v5: envelope, modules, atomic write, 3 backups, validation, slots, v4→v5 migration, load menu with slots.
5. EditMode tests: ledger balances, save round-trip, v4 fixture migration, corrupt-file recovery.
**Done when:** all v4 gameplay works unchanged; old v4 save loads; deleting/corrupting `save.json` restores
from backup; ≥ 40 tests green.

#### Phase 1 status
Code complete, compile-checked, **47 EditMode tests passing** (run outside Unity, since the Foundation layer has no
engine dependency). Waiting for the in-Editor play-test below. Changes from the plan above:

| Plan | What was built | Why |
|---|---|---|
| `Assets/CarSales/Core/` + `CarSales.Core` | `Assets/CarSales/Foundation/` + `CarSales.Foundation` (`noEngineReferences: true`) | `Scripts/Core/` already exists; two "Core" folders would be confusing. No engine refs means the tests can run anywhere. |
| Newtonsoft Json | Own small JSON reader for the save envelope (`MiniJson`); module data stays `JsonUtility` | One package fewer to install; the envelope only needs a few fields; module text passes through byte-for-byte. |
| ServiceRegistry, ConfigRegistry, SimRandom, RegionProfile | Deferred | Nothing uses them yet. They arrive with the first system that needs them (Region with documents in Phase 5). |
| `CarSales.Sim` assembly | Deferred to Phase 2 | No simulation logic has moved yet. |
| 3 manual slots + 1 autosave slot | 3 slots, each with 3 rotating backups; nightly autosave goes to the active slot | Simpler to understand; the backups cover the "bad autosave" case. |
| DailyReport built from the ledger | Report keeps its v4 numbers and adds a ledger "In / Out" breakdown | Keeps the v4 report working (rule R1). |

**What Phase 1 added:** `EventBus` + events (`MoneyChanged`, `TransactionPosted`, `DayStarted`, `DayEnded`, `WeekEnded`,
`QuarterEnded`, `GameSaved`, `GameLoaded`) · `Ledger` (21 categories, summaries, reconcile, compaction after 60 days,
lifetime totals) · every money movement in the game tagged with a category · `GameCalendar` (weekday, week, quarter,
year) · Save v5 (`SaveEnvelope`, `SaveStore`, `SaveMigrations`, `SaveValidator`), automatic import of the v4 save
into a slot · main menu **New Game / Load Game** slot picker with delete · Bank page **Account statement** · morning
report money breakdown · weekday on the HUD clock · Test Framework added to the installer.

**Phase 1 play-test checklist (in Unity):**
1. Run *Tools → Car Sales → 2. Build Everything*; Console has no red errors.
2. *Window → General → Test Runner → EditMode → Run All*: 47 tests green.
3. With an old v4 save present: Main Menu → **Continue** loads it (slot 1), your cash and cars are unchanged.
4. Buy, repair, refuel and sell a car; open the computer → **Bank**: each one is a line in the statement.
5. Sleep: the morning report shows the In/Out breakdown; the HUD clock shows the weekday.
6. Main Menu → **New Game** in slot 2, play a day, sleep; **Load Game** shows slots 1 and 2 with day and cash.
7. Quit Play mode, open the save folder (*Tools → Car Sales → Show Save Folder*), corrupt `slot_2/save.json`
   (delete half of it), load slot 2: it loads from a backup with a "Save recovered" message.

### Phase 2 — Vehicle Framework (L)
1. `IVehicleDriver`; ArcadeCarController implements it; TestDriveManager uses only the interface.
2. `WheelVehicleController` + Engine/Gearbox/EV models + ABS/TCS/ESC + physics profiles (10).
3. Placeholder car prefabs get wheel colliders (wizard); settings toggle Arcade/Realistic.
4. Component catalogue (30 components), `ComponentState`, `CarParts` façade, valuation v2 (§8.1).
5. DamageReceiver (zones → panels/components), fuel consumption from load, Cinemachine cameras, engine audio layers.
6. Vehicle Studio (first version) + Test track scene with telemetry.
7. Tests: gearbox shift logic, engine torque lookup, valuation, damage zone mapping, component aggregation.
**Done when:** every placeholder car drives on wheel physics with distinct handling per category (your
judgement), ABS/TCS work, crashes damage specific parts and lower value, Arcade fallback still works.

#### Phase 2 status
**Phase 2a is code complete:** compile-checked, **94 EditMode tests passing** (47 new), waiting for your in-Editor
driving test. Phase 2b (below) is not started.

| Plan | What was built | Notes |
|---|---|---|
| Steps 1-2 | `IVehicleDriver`; `WheelVehicleController` (Rigidbody + 4 WheelColliders, anti-roll bars, downforce, speed-sensitive steering); `CarSales.Sim` assembly with `Powertrain` (engine torque curves, automatic gearbox with kick-down and anti-hunting, reverse, electric drive with regen), `DriveInput` (S brakes then reverses), `DriveAssists` (ABS, TCS, ESC), 10 class presets × 4 powertrains | Every preset is checked by `PerformanceEstimator` in the tests: e.g. city hatch 0-100 in 12.0 s, sports car 5.5 s / 290 km/h, about 39 m to stop from 100 km/h. |
| Step 3 | No prefab changes needed: the controller is built at runtime around any model (wheels found by name, or placed from the body size). Settings: Realistic / Arcade, ABS, TCS, ESC | If a model can't be set up, the game falls back to arcade handling and logs why. |
| Step 4 | ~45 components (`ComponentCatalog`) in the 6 v4 groups; `CarParts` is now a façade over them; old saves convert automatically (game save module v6) | Valuation v2 **deferred** until the market model (Phase 7) so prices don't change before they can be play-tested. |
| Step 5 | Crashes hit the side of the car that was hit (front/rear/left/right/roof/underside); fuel use depends on engine load; wear per km for every part; condition changes power, brakes, grip, damping, steering pull and whether the car starts; code-made engine sound | Mesh deformation and recorded engine sounds: Phase 2b / Phase 10. Cinemachine replaced by a small built-in camera (`VehicleCamera`, C cycles chase / far / bonnet) to avoid another package. |
| Step 6 | **Vehicle Studio** window (Tools → Car Sales → Vehicle Studio): every car's class, mass, 0-100, top speed, braking; "Make editable" creates a profile asset in `Assets/CarSales/Data/Physics/` (survives Build Everything) | Test-track scene: Phase 2b. In-game **F3 telemetry** overlay (speed, rpm, gear, slip per wheel, active assists, condition effects). |
| Step 7 | Tests for torque curve, shifting, anti-hunting, reverse, EV, assists, steering limit, fuel, class performance, component weights, conversion, damage zones, wear, driving effects | |

**Phase 2b (next):** test-track scene; tuning pass from your driving feedback; optional mesh deformation;
component detail view in the car menu; manual gearbox option.

**Phase 2 play-test checklist (in Unity):**
1. Build Everything, Play. Get into your own car (F): it drives on wheels, the HUD shows the gear next to KM/H.
2. Accelerate hard on the main road: it shifts up through the gears; lift off: it slows down (engine braking).
3. Hold S while moving: it brakes; keep holding when stopped: it reverses ("R"). Space: handbrake slide.
4. Press C: chase / far chase / bonnet camera. Press F3: telemetry.
5. Drive the sports car and the pickup: they should feel clearly different (power, weight, body roll).
6. Crash into a wall nose-first: "Front damaged"; the inspection report lists the damaged front parts.
7. Settings → Physics: ARCADE, get in again: the old handling still works.
8. Park on a dealership spot (E), refuel (E at the pumps), get out (F): all as before.
9. Tell me how it feels: too slippery, too grippy, rolls too much, too fast or too slow?
### Phase 3 — Asset Studio (L, needs you to supply assets)
1. Studio window shell, `AssetRecord`, `LicenceRecord`, `AssetRegistry`, credits generator, release-build licence validator.
2. Local/Drop-folder ingest, Analyse, Normalise, Materials (URP Lit), Colliders, LODs.
3. Vehicle integration (wheel/door detection + manual correction) → drivable prefab + CarDefinition.
4. Building, Character, Prop/Interior templates.
5. Sketchfab connector (search + download with your token), Poly Haven connector (HDRI/textures).
6. Preview scene + validation checklist + approve/rollback.
**Done when (audit acceptance):** you import at least 1 vehicle, 1 building, 1 character and 1 prop from external
sources; the vehicle drives, the building works as a site, the character walks as a customer, all with licence records.

### Phase 4 — UI design system & visual pass (L)
1. `UITheme` + component library; screen stack, transitions, focus navigation, UI scale.
2. Redesign HUD, main menu (save slots), dealer computer desktop, vehicle file, marketplace, map.
3. Phone OS with apps (existing tow/taxi/map move in).
4. Lighting pass: URP settings, post-processing profile, HDRI sky, reflection probes, day/night tuning.
5. Replace placeholder cars/characters/buildings with imported assets you choose (via Asset Studio).
**Done when:** every existing screen uses the theme; no hard-coded colours remain (lint test); phone replaces the old phone.

### Phase 5 — Inspection, workshop, documents (L)
1. PlayerKnowledge + inspection methods + Inspection Mode (sockets, checklist).
2. Fault catalogue (40), progression, DTC scanner upgrade.
3. Parts catalogue, suppliers, parts inventory, repair jobs, workstations, Workshop Board, DIY repair interaction.
4. Documents & history (V5C, MOT + MOT bay, service book, keys, history check, write-off categories), VIN/plates.
5. Vehicle file tabs: Condition, Documents, History, Costs.
**Done when:** a car bought "blind" can be inspected step by step, revealing faults per method; repairs consume
parts + time + money and change value; an MOT can pass/fail with advisories; a history check can expose a Cat N.

### Phase 6 — Auction ecosystem (L)
1. AuctionEngine (venues, events, lots, reports, buyer accounts, invoices, fees, storage, collection).
2. AI bidder archetypes with §8.3 maths.
3. Physical yard + hall built as World Builder sites (numbered bays, auctioneer, paddle bidding).
4. Online terminal (calendar, catalogue, proxy bids, soft close), outbid notifications on phone.
5. Salvage/fleet/repossession event types; risks.
**Done when (audit acceptance):** you can register, inspect lots, bid live in the hall and online, win, pay the
invoice (fees + VAT in the ledger), and collect the car (drive/tow), and lose lots to AI bidders who bid sensibly.

### Phase 7 — Dealership operations: sourcing, customers, staff (XL)
1. MarketModel (price index, seasons, supply/demand) + competitors (off-screen) + competitor research page.
2. Private sellers (ads, appointments at houses, seller NPCs), dealer trade offers, trade-ins.
3. Listing editor (multi-angle photos, description, price helper) → online leads.
4. BuyerPersonas + BuyerBrain + inspection animations + Q&A + customer test drives + negotiation 2.0 + paperwork/handover + reviews.
5. CRM, follow-ups, repeat customers, warranties/complaints.
6. Staff: hiring, skills, wages, job board, salespeople/mechanics/detailers/drivers.
**Done when:** a full cycle works without debug tools: source → inspect → transport → repair/MOT/detail →
photograph & list → customer inspects + test drives → negotiate (trade-in/finance) → paperwork → handover → review;
a mechanic and a salesperson can each do their part of this without the player.

### Phase 8 — World Builder, open world, world simulation (XL)
1. World Builder layouts/sites/validation; convert the v4 town to `Layout_DefaultTown`.
2. Spline roads + junctions + lane graph; traffic cars + lights; pedestrians.
3. Weather + wet roads + effects on footfall/grip; calendar & bank holidays; fuel prices; random events.
4. Building interiors for dealership/auction hall/workshop (from imported assets).
**Done when:** you can build a new dealership site from an imported building and it works (spots, customers,
computer); traffic and pedestrians run at the performance budget.

### Phase 9 — Advanced business (L)
Accounts/P&L/cash flow, VAT returns (margin scheme), insurance & claims, business rates/utilities, marketing
campaigns, dealership levels 1–6, multi-site management, analytics, property development.
**Done when:** every transaction appears in the accounts; a 30-day sandbox run reconciles exactly (cash = opening + Σ ledger).

### Phase 10 — Production readiness (L)
Profiling and optimisation (§11), PlayMode test suite, save-migration test matrix, audio pass, animation polish,
localisation-ready strings table, accessibility (rebinding, subtitles, colour-blind), build pipeline (Windows
first), balancing with the Simulation Configurator sandbox, credits screen from licence records.
**Done when:** release-candidate build runs 2 in-game weeks without errors, meets §11 budgets, passes all tests.

---

## 10. Testing strategy

| Level | Tool | What | When |
|---|---|---|---|
| Compile check | dotnet + Unity reference assemblies (my side) | Everything compiles | Every change |
| EditMode unit tests | Unity Test Framework (NUnit) | Pure Sim/Core logic: ledger, VAT, valuation, faults, repairs, auction increments/fees/AI bids, negotiation, loans, save round-trip, migrations, plate/VIN uniqueness | Every phase; you run *Test Runner → EditMode → Run All* |
| PlayMode smoke tests | Unity Test Framework | Load test scene, spawn & drive a car 5 s (speed > 10 km/h), park on a spot, buy a car, sleep → day advances, save → reload → same state | Phase 2 onward |
| Sandbox simulation | Simulation Configurator | 30 game-days headless with scripted strategies → P&L sanity bounds | Phase 7 onward |
| Manual checklist | You | Per-phase play-test list (in each phase's PR/commit notes) | End of each phase |
| CI (optional) | GameCI GitHub Actions (needs a Unity licence secret) | Run EditMode tests on every push | After GitHub repo exists, if you want it |

Testability rule: if a feature can't be tested without the Editor, its logic is in the wrong layer (R3).

---

## 11. Performance budgets

Target hardware: GTX 1660 / RX 5600 class GPU, 6-core CPU, 16 GB RAM, 1080p, Medium preset.

| Metric | Budget |
|---|---|
| Frame rate | 60 fps average, 1% low ≥ 45 fps in the busiest area (dealership + traffic + rain) |
| CPU main thread | ≤ 12 ms |
| Memory | ≤ 4 GB total, ≤ 2.5 GB textures/meshes |
| Draw calls (SRP batches) | ≤ 2,500 in town view |
| Vehicles with full physics | Player car + test-drive car + ≤ 2 others; traffic uses simplified kinematic movement |
| Traffic / pedestrians | ≤ 30 cars / ≤ 40 pedestrians active, LOD'd AI updates (far agents tick at 2–5 Hz) |
| Imported vehicle | LOD0 ≤ 150k tris, ≤ 8 materials; building LOD0 ≤ 300k tris |
| Load times | Town load ≤ 20 s HDD / 8 s SSD; save ≤ 200 ms (off the main thread where possible) |

---

## 12. Asset licensing policy

`LicenceRecord { source, sourceUrl, author, licenceType, licenceUrl, attribution, commercialUse, modificationsAllowed,
redistributionRestricted, purchaseRef, dateAcquired, notes }`

| Licence type | Prototype | Release build |
|---|---|---|
| CC0 / Public domain | ✅ | ✅ |
| CC-BY | ✅ | ✅ with attribution in credits (auto-generated) |
| CC-BY-SA | ✅ | ⚠️ allowed only after review (share-alike applies to the asset) |
| CC-BY-NC / CC-BY-NC-SA / Editorial | ✅ (flagged) | ❌ blocked |
| Asset Store Standard EULA | ✅ | ✅ (asset must not be redistributed as source) |
| Purchased royalty-free (KitBash3D, CGTrader, Fab...) | ✅ | ✅ per the record's terms |
| Unknown | ⚠️ flagged | ❌ blocked |

Real car brands, logos and model names are trademarks: imported cars get **fictional** names/badges in game data
(v4 already uses Kestrel, Orion, Titan, Vortex, Aurelia). Asset Studio has a "remove/replace badges" checklist item.

---

## 13. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Code is written without running Unity on my side | Bugs found late | Phase 0 first; small phases; your Console reports after each phase; PlayMode smoke tests |
| WheelCollider handling feels bad | Core experience | Physics profiles + telemetry test track; Arcade fallback stays |
| Imported models vary wildly | Asset Studio can't auto-detect everything | Manual correction screen is a first-class feature, not an afterthought |
| Scope (10 phases, XL items) | Never finishing | Each phase is shippable; V5.0 can release after Phase 7 with Phases 8–10 as 5.x updates |
| Save format churn | Broken saves | Module versions + migration tests with fixture saves from each phase |
| Licences | Legal | §12 validator blocks release builds |
| Performance with imported high-poly assets | Low fps | Tri budgets enforced at Asset Studio validation; LODs; profiling each phase from Phase 4 |

---

## 14. Glossary

| Term | Meaning |
|---|---|
| V5C | UK vehicle registration certificate ("logbook") |
| MOT | UK annual roadworthiness test for cars older than 3 years |
| Cat A/B/S/N | UK insurance write-off categories: A scrap, B break for parts, S structural damage (repairable), N non-structural damage (repairable) |
| HPI check | Vehicle history check (write-off, finance, stolen) — simulated as "History check" |
| DTC | Diagnostic trouble code read by an OBD scanner (e.g. P0302) |
| Margin scheme | VAT scheme for used goods: VAT is due on the profit margin, not the full price |
| Hammer price | Winning bid at auction, before fees |
| Buyer premium | Fee the auction house charges the buyer on top of the hammer price |
| Reserve | Hidden minimum price the seller accepts |
| Soft close | Online auction extension when a bid arrives near the end |
| Strangler approach | Replacing a system gradually while the old one keeps running |
