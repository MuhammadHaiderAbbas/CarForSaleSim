# Changelog

## v5.0-dev — Test track fix
- The test track no longer depends on its scene references: the car database now lives in
  `Generated/Resources/CarSalesDatabase.asset` and is found automatically, input falls back to the default keys.
- Build Everything re-opens the saved test track scene and repairs any links that were lost (and logs which).
- The "input action is not linked" warning now points to *Tools → Car Sales → Re-link Input Actions*.

## v5.0-dev — Phase 2b "Test Track"
- **Test track** scene (built by Build Everything; *Tools → Car Sales → 5. Open Test Track*): long straight with
  distance boards, braking zone, slalom, bumps, skid pad, ramp and knock-over cones. Measures 0-100 km/h, braking
  distance, top speed and cornering g, next to the simulated figures. Switch car (N/B), condition (1-4),
  physics (G), gearbox (M), reset (R). Vehicle Studio: **Drive** button for each car.
- **Manual gearbox** option (Settings): Left Shift / Left Ctrl or gamepad bumpers; reverse from 1st at a standstill;
  over-revving downshifts are refused.
- **Dents**: damaged body panels now show as dents at the front, rear, sides or roof - on parked, listed and driven
  cars. Repairing the body removes them.
- **All parts** view (Inspection → All parts): every component with its condition, and how the car's condition
  changes power, brakes, grip and shocks.
- 10 new tests (104 total).

## v5.0-dev — Phase 2a "Vehicle Framework"
**Driving**
- **Realistic driving**: every car now drives on wheel physics (Rigidbody + 4 WheelColliders) with suspension,
  anti-roll bars, weight and grip. Front-, rear- or four-wheel drive depending on the car.
- **Engine and automatic gearbox**: torque curves, up/down shifts, kick-down, rev limiter, engine braking, reverse gear.
  Electric cars: instant torque, one gear, regenerative braking.
- **Driver aids**: ABS, traction control, stability control (each can be switched off in Settings).
- **10 handling classes** (city hatch, family hatch, sedan, sports sedan, SUV, pickup, van, sports, luxury, electric
  hatch) with realistic 0-100 km/h times and top speeds; petrol/diesel/hybrid/electric variants.
- Controls: S brakes and reverses when stopped, **Space = handbrake**, **C = camera** (chase / far / bonnet), **F3 = telemetry**.
- HUD speedometer shows the gear and goes up to 200 km/h. Engine sound pitched by rpm.
- *Settings → Physics: Arcade* brings back the v4 handling. Cars that can't be set up fall back to it automatically.

**Car condition**
- The 6 condition bars are now made of **~45 individual parts** (engine internals, timing belt, oil, each brake
  pad set, shocks, each tyre, each body panel, paint, glass, 12V battery...). Electric cars have a traction battery,
  motor and inverter instead of an engine. Old saves convert automatically.
- **Condition affects driving**: a worn engine is slower, worn pads brake later, bald tyres slide, worn shocks bounce,
  bad alignment pulls to one side, a dead battery means the car won't start.
- **Crashes damage the side that was hit** (front, rear, left, right, roof, underside). Driving wears parts (tyres, pads, oil...).
- Fuel use depends on how hard you drive.
- The inspection report lists the parts in the worst shape.

**Tools**
- **Vehicle Studio** (*Tools → Car Sales → Vehicle Studio*): each car's class, mass, 0-100, top speed and braking;
  "Make editable" creates a physics profile you can tune in the Inspector (kept when you Build Everything).
- 47 new tests (94 total).

## v5.0-dev — Phase 1 "Foundation"
**Saves**
- **3 save slots** (Main Menu → New Game / Load Game), each with **3 automatic backups**.
- Crash-safe writing: a save is written to a temp file and only then replaces the old one.
- Checksums: a damaged save loads the newest good backup instead ("Save recovered" message).
- Damaged values (NaN fuel, duplicate car ids, missing lists) are repaired on load.
- Your v4 save is moved into slot 1 automatically - nothing is lost.
- *Tools → Car Sales → Delete All Saves* replaces *Delete Save File*.

**Money**
- Every money movement is recorded in a **ledger** with a category (car sales, repairs, fuel, rent, loans...).
- Computer → **Bank** now has an **Account statement** (last 40 transactions, today's in/out, balance).
- The morning report shows where yesterday's money came from and went.

**Calendar**
- Weeks and quarters: the HUD clock shows the weekday (Day 1 is a Monday). Weekly/quarterly events are ready for wages and VAT later.

**Under the hood**
- New engine-free `CarSales.Foundation` assembly (EventBus, Ledger, GameCalendar, save store).
- First automated tests: 47 EditMode tests (*Window → General → Test Runner*). The installer now adds the Test Framework package.

## v4.0 "Open Town"
**World**
- A whole town: main road, north road and two avenues, street trees and lamps, skyline.
- New locations: **Fuel Station**, **Mechanic Workshop** (2 bays + mechanic desk), **Car Wash** (tunnel + attendant),
  **Used Car Market** (walk-around lot), **Auction Center**, 3 homes, 3 investment buildings, 3 dealerships.

**Driving & cars**
- Drive **any car you own** (F). Cars stay where you park them; drive up to a free dealership spot and press E to park.
- **Fuel** (100% = 25 km), refuel at the pumps; mileage increases while driving; crashes damage parts; night headlights.
- **Personal car** (your own beat-up hatchback) — can't be sold.
- **Tow trucks** (phone, P): move any car to a dealership, the workshop, car wash, fuel station or home. Price by distance.
- **Taxis** (phone or map): instant travel for money + time.

**Buying**
- **Used Car Market**: physical cars with price tags, free on-site inspection, buy and drive away.
- **Auction Center** (10:00–17:00): live bidding against AI bidders with a hammer timer; condition is only the seller's claim.
- Online purchases now cost a $300 delivery fee.

**Life & business**
- **24-hour day**: time always runs; dealerships open 08:00–20:00 automatically (close early with O or the sign).
- **Energy** drains while awake; **sleep at home** (from 18:00) to end the day; pass out at 02:00 (fine, groggy).
- **Properties** (computer → Real Estate, or FOR SALE signs): Suburban House & Hillside Villa (wake earlier, +reputation),
  **Downtown Motors** & **Luxury Lane** dealerships (own spots & customers; Luxury customers have 60% bigger budgets),
  Rental Apartments / Parking Garage / Office Tower (income every night).
- Morning report with rent, interest and property income.
- **Town map** (M) with GPS arrow on the HUD and minimap; **phone** (P).
- HUD: energy bar, GPS, fuel; new key hints. Goals updated for the town (15 goals).


## v3.0 "AUTO WORLD"
**UI redesign (everything builds itself from code - no prefabs, no Inspector wiring)**
- Dark-blue theme with procedurally generated rounded panels, icons, check marks and arrows (`UIFactory`).
- **Live car photos**: a hidden photo studio renders every car into the UI (`ThumbnailService`).
- **Dealer computer** with sidebar: Home dashboard, Marketplace (search, category chips, sorting, card grid),
  My Inventory (Owned / For Sale tabs), Messages, Upgrades, Bank & Loans, Statistics.
- **Vehicle Details**, **Inspection report** (6 parts with bars and verdict), **Purchase Successful** screen.
- **My Vehicle** screen with Inspect / Garage / Car Wash / Set Price / Test Drive / Quick Sell.
- **Garage**: pick repairs per part (engine, brakes, tires, body, interior, electronics) or a discounted Full Service.
- **Car Wash** with an animated wash.
- **Negotiation** as a chat with bubbles and a customer patience meter.
- **HUD**: minimap (with markers for cars, customers and the computer), speedometer, key hints, goal panel.
- **Notification cards** (success / info / warning) that stack and fade.

**Gameplay**
- Cars now have **per-part condition**; crashes damage body/electronics/tires.
- **Inspections** ($250, free with the Mechanic upgrade) reveal the truth before buying.
- **Messages**: private sellers offer honest cars at 60-80% of value (today only), plus tips and bank reminders.
- **Quick Sell** to a wholesaler for 75% of value (emergency cash).
- **Tab** opens your inventory anywhere. Debug money moved to **F1**.

**World**
- Glass-front showroom with roof, sign band, ceiling lights, display platforms, reception and lounge.
- Service garage with a car on a lift, car wash bay with brushes, curbs, road markings, sidewalks,
  trees, lamp posts, sale banners, flags, benches, planters and a city skyline.


## v2.0
**New game systems**
- **Main menu scene**: Continue / New Game / Settings / Quit, with a spinning showcase car.
- **Pause menu** (Esc): Resume, Save, Settings, Save & Main Menu, Save & Quit. Time stops while paused.
- **Settings**: mouse sensitivity and volume (saved in PlayerPrefs).
- **Dealer computer hub**: Marketplace, Upgrades, Bank & Loans, Statistics.
- **Upgrades**: Lot Expansion, Marketing, Mechanic Workshop, Detailing Bay, Showroom (13 levels in total).
- **Bank**: borrow up to a reputation-based limit, 2% daily interest, repay any time.
- **Goals**: 13 sequential objectives shown on the HUD, with cash rewards; they double as the tutorial.
- **Win** at $1,000,000 net worth; **bankruptcy** after 3 days in a row ending in debt.
- **Hidden condition**: marketplace sellers may exaggerate condition; the Mechanic upgrade reveals the truth.
- **Test drives**: drive any of your cars for 60 s with a chase camera; crashes reduce condition.
- **Day/night**: the sun moves with the shop clock.
- **Sound effects**: generated in code (no audio files), with optional clip overrides.

**Changes**
- Daily report shows loan interest, net worth and a debt warning.
- HUD shows the current goal, loan, and test-drive speed and timer.
- Error messages are red and play an error sound.
- Save format v2 (loads v1 saves).
- Package installer installs packages one by one (works on every Unity version, survives reloads).

## v1.0
- Core loop: marketplace → repair/clean → price → customers → negotiation → daily report → save/load.
- One-click wizard that generates all assets, prefabs, UI and the test scene.
