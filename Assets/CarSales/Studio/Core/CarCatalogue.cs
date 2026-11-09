using System.Collections.Generic;
using System.Linq;

namespace CarSales.Studio
{
    /// <summary>A car the game sells (fictional make and model; its 3D model comes from the default content pack).</summary>
    public sealed class CatalogueCar
    {
        public readonly string Id, Name, Brand, Category, Fuel, Gearbox, Description;
        public readonly int Year, Price;
        public readonly float Popularity;

        public CatalogueCar(string id, string name, string brand, string category, int year, int price, float popularity, string fuel, string gearbox, string description)
        { Id = id; Name = name; Brand = brand; Category = category; Year = year; Price = price; Popularity = popularity; Fuel = fuel; Gearbox = gearbox; Description = description; }
    }

    /// <summary>
    /// Phase 14a: the game's 48 cars, 8 per class (Hatchback, Sedan, SUV, Sports, Pickup, Luxury). The first eight ids are
    /// the original cars (saves keep working). Makes and models are fictional; the real car each one is modelled on is
    /// only in the credits (DefaultContent).
    /// </summary>
    public static class CarCatalogue
    {
        public static readonly string[] Categories = { "Hatchback", "Sedan", "SUV", "Sports", "Pickup", "Luxury" };

        public static readonly IReadOnlyList<CatalogueCar> All = new List<CatalogueCar>
        {
            new CatalogueCar("hatch_velo", "Velo", "Kestrel", "Hatchback", 2016, 12000, 1.0f, "Petrol", "Manual", "Nimble city hatchback. Cheap to run and easy to park."),
            new CatalogueCar("hatch_metro", "Metro S", "Kestrel", "Hatchback", 2019, 15500, 1.1f, "Petrol", "Automatic", "Sporty hatch with a modern infotainment system."),
            new CatalogueCar("hatch_saeta", "Saeta R", "Kestrel", "Hatchback", 1998, 9500, 1.2f, "Petrol", "Manual", "A cult 90s hot hatch with a screaming high-revving 1.8 twin-cam. Enthusiasts pay over the odds for clean, unmodified ones."),
            new CatalogueCar("hatch_dash", "Dash S", "Pellar", "Hatchback", 2011, 8900, 1.1f, "Petrol", "Manual", "Supercharged three-door with go-kart handling and a premium cabin. Check the timing chain and the clutch."),
            new CatalogueCar("hatch_pip", "Pip", "Novaro", "Hatchback", 2001, 2400, 0.9f, "Petrol", "Manual", "Tiny, frugal and almost unbreakable. A favourite first car for new drivers and students."),
            new CatalogueCar("hatch_lune", "Lune", "Calder", "Hatchback", 2020, 13500, 1.0f, "Petrol", "Manual", "Stylish modern supermini with a big touchscreen, lane assist and low insurance."),
            new CatalogueCar("hatch_fresca", "Fresca GT", "Merrow", "Hatchback", 1995, 3200, 0.9f, "Petrol", "Manual", "Turbocharged 90s hatch. Quick and fun, but rust in the sills and tired turbos are common."),
            new CatalogueCar("hatch_s26", "26", "Sorrell", "Hatchback", 2004, 2900, 0.9f, "Diesel", "Manual", "Comfortable small hatch with a thrifty diesel. Sells quickly to commuters on a budget."),
            new CatalogueCar("sedan_avenue", "Avenue", "Orion", "Sedan", 2014, 18000, 1.0f, "Diesel", "Automatic", "Comfortable family sedan with a big boot."),
            new CatalogueCar("sedan_avenuegt", "Avenue GT", "Orion", "Sedan", 2020, 26000, 1.1f, "Petrol", "Automatic", "The sporty version of the Avenue. Popular with young professionals."),
            new CatalogueCar("sedan_e5sport", "E5 Sport", "Halden", "Sedan", 2005, 11500, 1.1f, "Petrol", "Automatic", "A supercharged V8 executive saloon that looks like a taxi and goes like a sports car. Expensive to keep, fun to drive."),
            new CatalogueCar("sedan_regalia", "Regalia Athlete", "Ardent", "Sedan", 2014, 14000, 1.0f, "Hybrid", "Automatic", "Quiet, beautifully built Japanese-market executive saloon. Imported examples come with full service books."),
            new CatalogueCar("sedan_h1900", "1900 Classic", "Halden", "Sedan", 1987, 6500, 1.0f, "Petrol", "Manual", "Compact 80s executive saloon built like a bank vault. Collectors want low-mileage, rust-free cars."),
            new CatalogueCar("sedan_b320", "3-Twenty", "Brennan", "Sedan", 1996, 4800, 1.0f, "Petrol", "Manual", "Rear-wheel-drive 90s saloon with a smooth straight-six. A favourite for tuners and track days."),
            new CatalogueCar("sedan_steppe", "Steppe", "Novaro", "Sedan", 2004, 1600, 0.8f, "Petrol", "Manual", "Basic budget saloon. Nothing fancy, but parts are cheap and it starts every morning."),
            new CatalogueCar("sedan_marshal", "Marshal", "Lumen", "Sedan", 2003, 4200, 0.9f, "Petrol", "Automatic", "Big, soft American V8 cruiser with a front bench and a boot you could live in."),
            new CatalogueCar("suv_ridge", "Ridge", "Titan", "SUV", 2017, 32000, 1.2f, "Diesel", "Automatic", "Rugged 4x4 SUV. Families love it."),
            new CatalogueCar("suv_trekker", "Trekker 110", "Titan", "SUV", 2016, 34000, 1.2f, "Diesel", "Manual", "Boxy, go-anywhere off-roader with a ladder chassis. Holds its value better than almost anything."),
            new CatalogueCar("suv_prism", "Prism Coupe", "Calder", "SUV", 2015, 21000, 1.1f, "Diesel", "Automatic", "Fashionable compact SUV with a coupe roofline and a premium badge. Check the infotainment and the gearbox."),
            new CatalogueCar("suv_bison", "Bison Track", "Titan", "SUV", 2019, 58000, 1.2f, "Petrol", "Automatic", "A family SUV with a 700 bhp supercharged V8. Ridiculous, thirsty and very, very fast."),
            new CatalogueCar("suv_summit", "Summit Sport", "Brennan", "SUV", 2023, 41000, 1.1f, "Diesel", "Automatic", "Seven-seat ladder-frame SUV that tows anything. Farmers and big families both want one."),
            new CatalogueCar("suv_crest", "Crest 6", "Aurelia", "SUV", 2023, 52000, 1.0f, "Petrol", "Automatic", "Plush three-row luxury SUV with massaging seats and a whisper-quiet ride."),
            new CatalogueCar("suv_majesty", "Majesty X", "Aurelia", "SUV", 2021, 135000, 1.3f, "Petrol", "Automatic", "The ultimate luxury SUV: reclining rear seats, champagne fridge and a V8. For buyers who want to be seen."),
            new CatalogueCar("suv_tempesta", "Tempesta", "Vortex", "SUV", 2024, 195000, 1.4f, "Hybrid", "Automatic", "A supercar disguised as an SUV. Twin-turbo V8 hybrid, 0-60 in 3.3 seconds, and five seats."),
            new CatalogueCar("sports_stallion", "Stallion R", "Vortex", "Sports", 2018, 55000, 1.3f, "Petrol", "Manual", "Two-door sports coupe. Fast, loud and very desirable."),
            new CatalogueCar("sports_halcyon", "Halcyon Turbo", "Vortex", "Sports", 1975, 85000, 1.4f, "Petrol", "Manual", "The original widow-maker: a rear-engined 70s turbo icon. Values keep climbing, so originality is everything."),
            new CatalogueCar("sports_s3evo", "Sport 3 Evo", "Brennan", "Sports", 1990, 38000, 1.3f, "Petrol", "Manual", "Homologation special with box arches and a high-revving four. Genuine cars are rare and highly collectable."),
            new CatalogueCar("sports_s3v8", "Sport 3 V8", "Brennan", "Sports", 2011, 24000, 1.2f, "Petrol", "Automatic", "High-revving V8 coupe, often lowered and modified. Ask about rod bearings and look for a full history."),
            new CatalogueCar("sports_s4", "Sport 4", "Brennan", "Sports", 2017, 36000, 1.2f, "Petrol", "Automatic", "Twin-turbo straight-six coupe with huge grip and a carbon roof. Quick on any road."),
            new CatalogueCar("sports_zenith", "Zenith", "Kestrel", "Sports", 1991, 62000, 1.3f, "Petrol", "Manual", "The everyday supercar: mid-engined V6, aluminium body and legendary reliability."),
            new CatalogueCar("sports_kite", "Kite 18", "Kaito", "Sports", 1995, 18000, 1.2f, "Petrol", "Manual", "Pop-up headlights and a rear-wheel-drive turbo. Drifters hunt for them, so unmodified cars are getting rare."),
            new CatalogueCar("sports_meridian", "Meridian 73", "Kaito", "Sports", 1973, 55000, 1.2f, "Petrol", "Manual", "A 70s Japanese grand tourer with a straight-six and four round tail lights. A true collector's piece."),
            new CatalogueCar("pickup_hauler", "Hauler", "Titan", "Pickup", 2015, 28000, 1.0f, "Diesel", "Manual", "Workhorse pickup truck. Tradespeople buy these all day."),
            new CatalogueCar("pickup_outrider", "Outrider 23", "Titan", "Pickup", 2023, 36000, 1.1f, "Diesel", "Automatic", "Modern double-cab pickup with a one-tonne payload and a car-like cabin."),
            new CatalogueCar("pickup_trailchief", "Trail Chief", "Titan", "Pickup", 2019, 39000, 1.1f, "Petrol", "Automatic", "Lifted full-size pickup with off-road tyres, locking diff and a big V8."),
            new CatalogueCar("pickup_h1500", "Hauler 1500 Sport", "Titan", "Pickup", 2020, 42000, 1.1f, "Petrol", "Automatic", "Full-size crew-cab pickup in sport trim. Tows a horsebox without noticing."),
            new CatalogueCar("pickup_predator", "Predator", "Brennan", "Pickup", 2018, 55000, 1.3f, "Petrol", "Automatic", "High-speed desert pickup with long-travel suspension. A showroom magnet."),
            new CatalogueCar("pickup_mule", "Mule", "Novaro", "Pickup", 2012, 5500, 0.9f, "Petrol", "Manual", "Tiny cab-over work truck. Gardeners and market traders love how cheap it is to run."),
            new CatalogueCar("pickup_ridgeback", "Ridgeback", "Titan", "Pickup", 2012, 15000, 1.0f, "Petrol", "Automatic", "Part SUV, part pickup, with a fold-through cab wall for long loads."),
            new CatalogueCar("pickup_ehauler", "E-Hauler", "Titan", "Pickup", 2024, 68000, 1.1f, "Electric", "Automatic", "Electric full-size pickup with 400 miles of range and a lockable front boot."),
            new CatalogueCar("lux_regent", "Regent", "Aurelia", "Luxury", 2019, 70000, 1.2f, "Hybrid", "Automatic", "Hand-built luxury limousine with a hybrid V12 and every option ticked. The ultimate status symbol."),
            new CatalogueCar("lux_paragon", "Paragon V12", "Halden", "Luxury", 2015, 48000, 1.2f, "Petrol", "Automatic", "Flagship twin-turbo V12 limousine. Every gadget imaginable, and depreciation to match."),
            new CatalogueCar("lux_paragoncoupe", "Paragon Coupe 800", "Halden", "Luxury", 2017, 72000, 1.3f, "Petrol", "Automatic", "Tuner-built 800 bhp luxury coupe with a carbon body kit. Rare, loud and unmistakable."),
            new CatalogueCar("lux_paragonclassic", "Paragon Classic", "Halden", "Luxury", 1996, 9500, 1.1f, "Petrol", "Automatic", "The 90s flagship saloon: double glazing, soft-close doors and tank-like build. Bargain luxury."),
            new CatalogueCar("lux_vanta", "Vanta GT", "Merrow", "Luxury", 2017, 98000, 1.3f, "Petrol", "Automatic", "Elegant British-style grand tourer with a twin-turbo V12 and hand-stitched leather."),
            new CatalogueCar("lux_solenne", "Solenne", "Vortex", "Luxury", 2020, 165000, 1.4f, "Petrol", "Automatic", "Front-engined Italian-style GT with a twin-turbo V8 and timeless lines."),
            new CatalogueCar("lux_grand8", "Grand 8", "Brennan", "Luxury", 2020, 88000, 1.2f, "Petrol", "Automatic", "Big V8 luxury coupe that is as happy on track as on the motorway."),
            new CatalogueCar("lux_apex", "GT Apex", "Halden", "Luxury", 2015, 64000, 1.2f, "Petrol", "Automatic", "Long-bonnet front-mid-engined GT with a hand-built twin-turbo V8."),
        };

        public static CatalogueCar Find(string id) => All.FirstOrDefault(c => c.Id == id);
        public static IEnumerable<CatalogueCar> InCategory(string category) => All.Where(c => c.Category == category);
    }
}
