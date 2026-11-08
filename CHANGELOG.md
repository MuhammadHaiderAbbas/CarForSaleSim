# Changelog

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
