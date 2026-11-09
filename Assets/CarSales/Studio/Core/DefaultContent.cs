using System;
using System.Collections.Generic;
using System.Linq;

namespace CarSales.Studio
{
    public enum ContentKind { Building = 0, Prop = 1, Construction = 2, Car = 3 }

    /// <summary>One model of the default content pack: what it is for in the game and where it comes from.</summary>
    public sealed class ContentItem
    {
        public readonly ContentKind Kind;
        /// <summary>Buildings: villa, house, apartment, office, shop, industrial. Props: tree, streetlamp, bench, fuelpump...
        /// Construction: crane, excavator, mixer... Cars: the car class (hatchback, sedan, suv, sports, pickup, luxury).</summary>
        public readonly string Style;
        /// <summary>Cars only: the game car (CarCatalogue id) this model is for.</summary>
        public readonly string CarId;
        public readonly string Uid, Title, Author, AuthorName, Licence;
        public readonly int Faces;

        public ContentItem(ContentKind kind, string style, string carId, string uid, string title, string author, string authorName, string licence, int faces)
        { Kind = kind; Style = style; CarId = carId; Uid = uid; Title = title; Author = author; AuthorName = authorName; Licence = licence; Faces = faces; }

        /// <summary>The Asset Studio id the model is approved under (stable, so the game can find it again).</summary>
        public string AssetId => StudioNames.SafeName(Kind == ContentKind.Car ? "dc_car_" + CarId : $"dc_{Kind}_{Style}_{Uid.Substring(0, 8)}");

        /// <summary>The Asset Studio category it is approved as (trees and construction machines have no size limit).</summary>
        public AssetCategory Category =>
            Kind == ContentKind.Car ? AssetCategory.Vehicle :
            Kind == ContentKind.Building ? AssetCategory.Building :
            Kind == ContentKind.Construction || Style == "tree" ? AssetCategory.Environment : AssetCategory.Prop;

        /// <summary>The Asset Studio's licence type for this item.</summary>
        public LicenceType LicenceType => Licence == "CC0 Public Domain" ? LicenceType.CC0 : LicenceType.CCBY;
        public string Url => "https://sketchfab.com/3d-models/" + Uid;
        public string AuthorUrl => "https://sketchfab.com/" + Author;
        /// <summary>Credit line, as CC Attribution asks: title, author, licence, link.</summary>
        public string Credit => $"\"{Title}\" by {AuthorName} ({Url}) - {Licence}";
    }

    /// <summary>
    /// Phase 14a: the default content pack. Every model below was checked on Sketchfab (downloadable, licence CC
    /// Attribution or CC0, so it may be used in a paid game with a credit). The Asset Studio's "Default Content" tab
    /// downloads them all with your Sketchfab token and sets them up: buildings fill the town, cars become the game's
    /// 48 cars (8 per class), props and construction machines fill their slots.
    /// Real car makes are shown in game under fictional names (CarCatalogue); badges should be removed before sale.
    /// </summary>
    public static class DefaultContent
    {
        public static readonly IReadOnlyList<ContentItem> All = new List<ContentItem>
        {
            new ContentItem(ContentKind.Building, "villa", "", "4958b4ac23eb445aaa6115278676d5df", "Modern luxury villa house building home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 7742),
            new ContentItem(ContentKind.Building, "villa", "", "6757208506d941da8c6ed1758c7b7c8a", "Modern luxury villa house building with pool", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 44466),
            new ContentItem(ContentKind.Building, "villa", "", "a10e86fa107f476a970b6939bdda77d6", "Modern luxury villa house building with pool", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 37586),
            new ContentItem(ContentKind.Building, "villa", "", "38c9a917bae042edbb54c62a2562c188", "Modern luxury villa house home building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 54982),
            new ContentItem(ContentKind.Building, "villa", "", "3dbc112af44044539c92242fc8107f51", "Modern luxury villa house building home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 140800),
            new ContentItem(ContentKind.Building, "villa", "", "69768508e3ca48c7829157665b56da17", "Modern luxury villa house building home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 50880),
            new ContentItem(ContentKind.Building, "house", "", "73edd9757257460cbc6d6e9f59ffe213", "modern villa apartment house home building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 42768),
            new ContentItem(ContentKind.Building, "house", "", "e33b74cba5e948d489202b37cd97ae8a", "modern apartment house home building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 50868),
            new ContentItem(ContentKind.Building, "house", "", "81daf60658ec4c119ee1a1e870ed9ed5", "Modern luxury apartment  villa building home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 44808),
            new ContentItem(ContentKind.Building, "house", "", "1dff9af9099d4a76ae95bd9d30ef71d2", "Modern luxury villa house home building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 21357),
            new ContentItem(ContentKind.Building, "house", "", "94d8357737ec4334aaea2d8e489e7d4d", "Modern luxury villa house building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 22430),
            new ContentItem(ContentKind.Building, "house", "", "efcc220c62f641d29a1480e8b221c7b3", "Suburban House", "strielecki", "Bogdan Strielecki", "CC Attribution", 146846),
            new ContentItem(ContentKind.Building, "house", "", "798ffaeadacf4e08a2665785422fb45d", "American House", "AleixoAlonso", "AleixoAlonso", "CC Attribution", 41991),
            new ContentItem(ContentKind.Building, "apartment", "", "d4959af16d8f4524844e8b97d3e9772b", "residential complex modern apartment building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 192960),
            new ContentItem(ContentKind.Building, "apartment", "", "5f8d0f79b88e4bafb2ac1ce1c89691d1", "residential complex modern apartment building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 107040),
            new ContentItem(ContentKind.Building, "apartment", "", "f0ed2eeb6edf4b1a97a645d793b8a587", "modern residential complex apartment building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 96272),
            new ContentItem(ContentKind.Building, "apartment", "", "43232ebb8d3841a996a78b2c09a6dc84", "modern apartment house building design", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 476056),
            new ContentItem(ContentKind.Building, "apartment", "", "19ad467634dc419a9e4da3d27a3a3be6", "residential complex modern apartment building", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 69068),
            new ContentItem(ContentKind.Building, "apartment", "", "7685d05e420e42f0889339f292778100", "modern apartment house building design", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 267248),
            new ContentItem(ContentKind.Building, "apartment", "", "36ab5ed4e8ce463b90ddf09e612a8118", "modern apartment building design", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 78196),
            new ContentItem(ContentKind.Building, "apartment", "", "1704ecbc09a24f358332fd358c818d18", "Urban Building", "sousinho", "Sousinho", "CC Attribution", 112577),
            new ContentItem(ContentKind.Building, "office", "", "d96380fb001345cca9a9be121f3e43d5", "[FREE]  Atlanta, Corperate Office Building", "99.Miles", "99.Miles", "CC Attribution", 37290),
            new ContentItem(ContentKind.Building, "office", "", "52b73f6ea18a440cb42734840d5edc72", "[FREE] London Skyscraper", "99.Miles", "99.Miles", "CC Attribution", 142785),
            new ContentItem(ContentKind.Building, "office", "", "2305f0fe03ba44229a1f768dcb48bd8f", "Singapore Office Skyscraper [FREE]", "99.Miles", "99.Miles", "CC Attribution", 26396),
            new ContentItem(ContentKind.Building, "office", "", "4480869188b64658aa8c69ed31a730df", "modern tower office apartment house home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 54110),
            new ContentItem(ContentKind.Building, "office", "", "071bda8efe344177a8216ef2de47a784", "modern tower office apartment house home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 144086),
            new ContentItem(ContentKind.Building, "office", "", "3d5dda112c0646ff8ad1a86d90305e13", "modern tower office apartment house home", "ziguratarchitecturestudio", "zigurat architecture studio", "CC Attribution", 420880),
            new ContentItem(ContentKind.Building, "office", "", "af9782245c134c21ae0c8df7f57077e6", "Office building", "vrcityphoto", "Nikitos & 3130", "CC Attribution", 644690),
            new ContentItem(ContentKind.Building, "office", "", "8d3ea98595b4403cbf21065e8f9b7bb7", "London Office Building", "pukkafilms", "Pukka Films", "CC Attribution", 86001),
            new ContentItem(ContentKind.Building, "shop", "", "760bb4f86d1548f79cf2959d05534277", "Sclater Street Corner Building", "artfletch", "artfletch", "CC Attribution", 971668),
            new ContentItem(ContentKind.Building, "shop", "", "019287f36bf140738ae1651f6b0923ff", "Shop", "local.yany", "local.yany", "CC Attribution", 65308),
            new ContentItem(ContentKind.Building, "industrial", "", "a05aff011ef04f1797013781437c18cf", "80s Warehouse", "DavidAlmanza", "DaveAlmz", "CC Attribution", 74660),
            new ContentItem(ContentKind.Building, "industrial", "", "1a4c399c0fed41119a4bf5ee9837550c", "Warehouse Building", "jimbogies", "jimbogies", "CC Attribution", 89522),
            new ContentItem(ContentKind.Building, "industrial", "", "daa7fd3ff88945298d00045ca40a4c03", "Warehouse FBX Model Free", "Nicholas01", "Nicholas-3D", "CC Attribution", 11106),
            new ContentItem(ContentKind.Prop, "tree", "", "c6c7cebef37544ee8a80780034a03737", "Fast Urban Tree", "rodrigogelmi", "gelmi.com.br", "CC Attribution", 96264),
            new ContentItem(ContentKind.Prop, "tree", "", "3dc59560f2d24345bdbe65c44636453b", "Oak tree", "greg-l", "massive-graphisme", "CC Attribution", 7112),
            new ContentItem(ContentKind.Prop, "tree", "", "6468dd4d3eb240ef902b9057d9913606", "Oak tree", "evolveduk", "evolveduk", "CC Attribution", 8364),
            new ContentItem(ContentKind.Prop, "tree", "", "c0c3aa73cc9945d2a36c01a0d8d6d4bb", "Linden tree", "intice184", "Georgeous", "CC Attribution", 46236),
            new ContentItem(ContentKind.Prop, "tree", "", "003c6ab20e644655ba36fe54cb04765b", "Japanese Maple", "endlessvoidmc", "endlessvoidmc", "CC Attribution", 54469),
            new ContentItem(ContentKind.Prop, "streetlamp", "", "b3534b9033a24f0aad2e1f9527bd4ca8", "Street light Lamp spotlight - 10MB", "ahmagh2e", "Mehdi Shahsavan", "CC Attribution", 14120),
            new ContentItem(ContentKind.Prop, "streetlamp", "", "77b0a1e448f14b47b9dc62881a241850", "Street light", "ahmagh2e", "Mehdi Shahsavan", "CC Attribution", 23272),
            new ContentItem(ContentKind.Prop, "bench", "", "6081a54e64a94edda2c444d81e8aec2c", "Bench_ Park - 14MB", "ahmagh2e", "Mehdi Shahsavan", "CC Attribution", 21940),
            new ContentItem(ContentKind.Prop, "bench", "", "84426d6537ac4cdc837d602ccabe7036", "Park Bench", "dutrabr98", "DutraBR98", "CC Attribution", 101612),
            new ContentItem(ContentKind.Prop, "fuelpump", "", "add0f902b9164718bd0540e88bf9927e", "gas pump", "youoyouyou", "kunkun", "CC Attribution", 13413),
            new ContentItem(ContentKind.Prop, "fuelpump", "", "b5a77ed450b64dac9141da87fc7d4c64", "Gasoline pump", "Artem.Goyko", "Artem Goyko", "CC Attribution", 6858),
            new ContentItem(ContentKind.Prop, "carlift", "", "c64bf7abd37f46c2b26323a59a7aaca9", "Hydraulic car lift", "03HenryV04", "Henry Vu", "CC Attribution", 23780),
            new ContentItem(ContentKind.Prop, "toolcart", "", "16ceecf05f3e453e96c050f4c43f86b5", "Tool Cart", "jimbogies", "jimbogies", "CC Attribution", 80238),
            new ContentItem(ContentKind.Prop, "tyrerack", "", "3e4fdc69fe5f428082242a32a343c28a", "Tire Rack", "roughdominator", "3ddominator", "CC Attribution", 5680),
            new ContentItem(ContentKind.Prop, "shelf", "", "8b3555c8083b4cb9a248feea44d186d6", "Garage Shelf", "yagiz.slz", "Ya\u011f\u0131z Solmaz", "CC Attribution", 149656),
            new ContentItem(ContentKind.Construction, "crane", "", "49851dc7a51b43bda6aea06856c26a85", "Tower Crane", "Chamodp", "Chamod1999", "CC Attribution", 32308),
            new ContentItem(ContentKind.Construction, "excavator", "", "88b3ca0e6bac44ce8cef9865288d2ee0", "Excavator digger 3d model", "Arts7789", "Arale pixel", "CC Attribution", 122792),
            new ContentItem(ContentKind.Construction, "mixer", "", "a8cd2a2cc2e243b59c168c4bb28e489b", "Blue Cement Mixer Truck", "sandisbergvalds21", "sandisbergvalds2008", "CC Attribution", 142282),
            new ContentItem(ContentKind.Construction, "bulldozer", "", "b26ce56081304c1988ec7490e439d882", "D8 Tractor", "artfletch", "artfletch", "CC Attribution", 49367),
            new ContentItem(ContentKind.Construction, "loader", "", "5aab228535c04d2e910be88361ab1f1c", "Wheel Loader", "vishal24", "vishal gawde", "CC Attribution", 65373),
            new ContentItem(ContentKind.Construction, "dumptruck", "", "781c616da8f44982a59cb3fc3fa67f98", "Dump Truck", "ElectroNick", "ElectroNick", "CC Attribution", 34307),
            new ContentItem(ContentKind.Construction, "scaffold", "", "c1f03864ec72449c8968f0630c287c8d", "Realistic Metal Scaffolding", "jungle_jim", "Jungle Jim", "CC Attribution", 28028),
            new ContentItem(ContentKind.Construction, "cabin", "", "e4e3b7fdaba94868803cb4630cd3597b", "container construction site", "simpleandextensivedesign", "simple and extensive design", "CC Attribution", 116439),
            new ContentItem(ContentKind.Construction, "bricks", "", "1f32da1a103b4949963cbe09099eb0ca", "red bricks on pallet", "deathcow", "vicente betoret ferrero", "CC Attribution", 49999),
            new ContentItem(ContentKind.Construction, "pallet", "", "ee1dc525a33d499bbad75c4e110050cd", "Construction Pallet - Photoscan (Optimized)", "GameDevNick", "Coozy", "CC Attribution", 91444),
            new ContentItem(ContentKind.Construction, "fence", "", "cecaa44a38374162b79b0c8b4e64f987", "Barricade fence (Game ready)", "PT34", "PT34", "CC Attribution", 2748),
            new ContentItem(ContentKind.Construction, "barrier", "", "a09622c043724a1b92e7920b22edb6bf", "concrete road barrier photoscanned", "widthRider", "widthRider", "CC Attribution", 5138),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_velo", "218c3c0c6afd4d0eb343bd50d8868fd0", "Suzuki Swift", "CasacadeOperations", "Casacade Models", "CC Attribution", 424172),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_metro", "bab77902c638427bb85e68b6762a481f", "2016 Volkswagen Polo", "BHP3D", "BHP3D", "CC Attribution", 179345),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_saeta", "b9604dd71c6546ec842702682019492a", "Honda Civic Type R -98 (Free Asset)", "tiedtke", "tiedtke", "CC Attribution", 238189),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_dash", "1b628ba826f04ba79823ef62dccaa0df", "2011 Mini John Cooper Works (R56)", "supercarmodels", "DisneyCars", "CC Attribution", 156414),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_pip", "443fd49eb0844557a06854cae0b61267", "2001 Toyota Yaris", "Tyler_Dave", "Dave Love SketchFab", "CC Attribution", 101192),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_lune", "4c528fefa4154004bbc6305faf71836c", "2020 Renault Clio", "tonielpro520", "tonielpro520", "CC Attribution", 328579),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_fresca", "48db6facb4b64e99b60f36b8c01185e1", "(FREE) 1995 Fiat Punto GT", "lionsharp", "Lionsharp Studios", "CC Attribution", 35168),
            new ContentItem(ContentKind.Car, "hatchback", "hatch_s26", "dc25a117378545e7a804c8594aa86938", "Peugeot 206", "alieslami7878", "Alvier", "CC Attribution", 491915),
            new ContentItem(ContentKind.Car, "sedan", "sedan_avenue", "820387a848a8449eb6565ff35e74388a", "Mersedes-Benz E-class w212", "BlackSnow02", "Black Snow", "CC Attribution", 151990),
            new ContentItem(ContentKind.Car, "sedan", "sedan_avenuegt", "3955e5c050f843398b49b2b21ab2f232", "2022 Volkswagen Virtus GT", "BHP3D", "BHP3D", "CC Attribution", 671143),
            new ContentItem(ContentKind.Car, "sedan", "sedan_e5sport", "4952cf97bbc94b07be8aa09f277c0bcb", "Mersedes-Benz E55 W211", "BlackSnow02", "Black Snow", "CC Attribution", 605194),
            new ContentItem(ContentKind.Car, "sedan", "sedan_regalia", "138a753d04dd4d8f8982d8f5583d212d", "Toyota Crown athlete s210", "BlackSnow02", "Black Snow", "CC Attribution", 734700),
            new ContentItem(ContentKind.Car, "sedan", "sedan_h1900", "9b2ea34482654173a7f421aab8f1b287", "1982 Mercedes W201", "Tyler_Dave", "Dave Love SketchFab", "CC Attribution", 185752),
            new ContentItem(ContentKind.Car, "sedan", "sedan_b320", "13d0a12ecda04317b96ce1e618300412", "BMW 3-Series e36 [Street]", "BlackSnow02", "Black Snow", "CC Attribution", 408536),
            new ContentItem(ContentKind.Car, "sedan", "sedan_steppe", "90006f59fd594d2b95f1469f651491f1", "Daewoo - Nexia\\Cielo", "vsevolod_smoki", "sMoKi", "CC Attribution", 44491),
            new ContentItem(ContentKind.Car, "sedan", "sedan_marshal", "0f9e6281fd6b4c7a8c5c9de957ccf5a5", "mercury grand marquis [FREE]", "I95XP", "L95XP", "CC Attribution", 65991),
            new ContentItem(ContentKind.Car, "suv", "suv_ridge", "acca60013e8b4adaaa0823389525b5b2", "Tayota Prado 2010 3D Model", "betier", "Betier Models", "CC Attribution", 595366),
            new ContentItem(ContentKind.Car, "suv", "suv_trekker", "5773ec04488b46148816b5f7a9f3f44f", "Land Rover Defender 110", "meeww", "meeww", "CC Attribution", 688330),
            new ContentItem(ContentKind.Car, "suv", "suv_prism", "e0bb09118ab84fe0a0530bd0df2fd74a", "Range Rover Evoque", "minghau", "minghauLoh", "CC Attribution", 38912),
            new ContentItem(ContentKind.Car, "suv", "suv_bison", "f8024595e11c48119877c9c7cc554abc", "Jeep Grand Cherokee Trackhawk", "David_Holiday", "David_Holiday", "CC Attribution", 363057),
            new ContentItem(ContentKind.Car, "suv", "suv_summit", "c37bec0353a94f90a3acbe09ceb3aecf", "Ford Everest Sport 2023", "Asadawut.Kaewma", "Asadawut.Kaewma", "CC Attribution", 483900),
            new ContentItem(ContentKind.Car, "suv", "suv_crest", "b6a0be0c32e343b4812ec47660142824", "Cadillac XT6 Luxury 2023 (free)", "3dshowroom", "3DShowroom", "CC Attribution", 200747),
            new ContentItem(ContentKind.Car, "suv", "suv_majesty", "34c4687ba8204efaa7551afea9f5689f", "Mersedes- Benz Maybach GLS 600", "BlackSnow02", "Black Snow", "CC Attribution", 474467),
            new ContentItem(ContentKind.Car, "suv", "suv_tempesta", "cc113385b7e34481af5e50a2fb8b6b2e", "2025 Lamborghini Urus SE", "ddiaz-design", "Ddiaz Design", "CC Attribution", 250098),
            new ContentItem(ContentKind.Car, "sports", "sports_stallion", "0eaa7a16796540f29461ddae05ecdeb3", "Ford Mustang Shelby GT500", "saitoyang", "Jiaxing", "CC Attribution", 349291),
            new ContentItem(ContentKind.Car, "sports", "sports_halcyon", "8568d9d14a994b9cae59499f0dbed21e", "FREE 1975 Porsche 911 (930) Turbo", "lionsharp", "Lionsharp Studios", "CC Attribution", 241265),
            new ContentItem(ContentKind.Car, "sports", "sports_s3evo", "ac3c7013434e403e8faff87948caf422", "[FREE] BMW M3 E30", "TinoD2", "Martin Trafas", "CC Attribution", 117261),
            new ContentItem(ContentKind.Car, "sports", "sports_s3v8", "c35a14d811b042d792a6da69381f7f80", "BMW M3 e92 [stance]", "BlackSnow02", "Black Snow", "CC Attribution", 208998),
            new ContentItem(ContentKind.Car, "sports", "sports_s4", "8e87379f40fd40dcac0a751e22c1a188", "BMW M4 f82", "BlackSnow02", "Black Snow", "CC Attribution", 506888),
            new ContentItem(ContentKind.Car, "sports", "sports_zenith", "1cc15628a00a4739a6b6c01128927c8d", "Honda NSX 1990", "Lexyc16", "Lexyc16", "CC Attribution", 101006),
            new ContentItem(ContentKind.Car, "sports", "sports_kite", "eb3b4310304948ec8eaa4447080d2380", "Nissan Silvia 180sx [Free]", "BlackSnow02", "Black Snow", "CC Attribution", 554952),
            new ContentItem(ContentKind.Car, "sports", "sports_meridian", "1a950b81ee274a0eb8014bd84ba047f5", "Nissan Skyline GT-R C110 Kenmeri '73", "TinoD2", "Martin Trafas", "CC Attribution", 290990),
            new ContentItem(ContentKind.Car, "pickup", "pickup_hauler", "2c2fcccdd96948aaa655f936aa325f0d", "Chevrolet S10", "speedmodel", "Luquita", "CC Attribution", 645282),
            new ContentItem(ContentKind.Car, "pickup", "pickup_outrider", "e93ac097ee9a46a4a9decca9aceed03c", "Ford Ranger Next-Gen 2023 Sport", "Asadawut.Kaewma", "Asadawut.Kaewma", "CC Attribution", 793156),
            new ContentItem(ContentKind.Car, "pickup", "pickup_trailchief", "652324cc8a974d3a9869ce2b0f3beaaa", "2019 Chevrolet Silverado Trail Boss Z71", "ddiaz-design", "Ddiaz Design", "CC Attribution", 479112),
            new ContentItem(ContentKind.Car, "pickup", "pickup_h1500", "6a0be94fec824513bffe48df57d0b17a", "2020 Chevrolet Silverado 1500 RST", "ddiaz-design", "Ddiaz Design", "CC Attribution", 472293),
            new ContentItem(ContentKind.Car, "pickup", "pickup_predator", "c640e4e7c68545e09a9348494c2c13a1", "Ford F150 Raptor", "David_Holiday", "David_Holiday", "CC Attribution", 856238),
            new ContentItem(ContentKind.Car, "pickup", "pickup_mule", "e1654a399b9d448a9a7ce200410ac91d", "Carry Pickup | Suzuki | Game ready", "by__Rx", "by__Rx", "CC Attribution", 23104),
            new ContentItem(ContentKind.Car, "pickup", "pickup_ridgeback", "59807d505e9a47f295d756e27e4002a1", "Chevolet Avalanche 2012", "Doz3", "Doz3", "CC Attribution", 317428),
            new ContentItem(ContentKind.Car, "pickup", "pickup_ehauler", "73a1f1a86dc54b97b1f658daf070460c", "2024 Chevrolet Silverado EV RST", "ddiaz-design", "Ddiaz Design", "CC Attribution", 301607),
            new ContentItem(ContentKind.Car, "luxury", "lux_regent", "4a590f4afa094fa8b407a14db77a63a8", "Rolls-Royce Ghost", "BlackSnow02", "Black Snow", "CC Attribution", 765829),
            new ContentItem(ContentKind.Car, "luxury", "lux_paragon", "f42d9d13b0404b5b9a2302a4c590a94d", "Mersedes-Benz s65 w222", "BlackSnow02", "Black Snow", "CC Attribution", 558936),
            new ContentItem(ContentKind.Car, "luxury", "lux_paragoncoupe", "1b79ffa597de49d188537b3f40aa8b50", "Mercedes-Benz s63 Coupe Brabus 800", "BlackSnow02", "Black Snow", "CC Attribution", 451832),
            new ContentItem(ContentKind.Car, "luxury", "lux_paragonclassic", "ad9650076f4c491eb90d65ccb1378b9a", "Mersedes-Benz S-class W140", "BlackSnow02", "Black Snow", "CC Attribution", 515087),
            new ContentItem(ContentKind.Car, "luxury", "lux_vanta", "936854ba7dde46f09a47d355d433808c", "2017 Aston Martin DB11 [5.2L Twin-Turbo V12]", "Haris3D", "Hari Prasath R", "CC Attribution", 296094),
            new ContentItem(ContentKind.Car, "luxury", "lux_solenne", "cbc0c13c0fb54fc8bbfa79a9ff99e3aa", "2020 Ferrari Roma", "ddiaz-design", "Ddiaz Design", "CC Attribution", 241354),
            new ContentItem(ContentKind.Car, "luxury", "lux_grand8", "25d5b4f6d13e4217afa09bbf89f8d993", "BMW M8 F92 Coup\u00e9 Competition", "sohyalebret", "kevin (\u30b1\u30d3\u30f3)", "CC Attribution", 350608),
            new ContentItem(ContentKind.Car, "luxury", "lux_apex", "4d9e0a8cf9a844149bf6630da83eb97b", "Mercedes-Benz AMG-GT 2015", "mmcworks", "MMC Works", "CC Attribution", 661904),
        };

        public static IEnumerable<ContentItem> OfKind(ContentKind kind) => All.Where(i => i.Kind == kind);
        public static IEnumerable<ContentItem> OfStyle(ContentKind kind, string style) => All.Where(i => i.Kind == kind && i.Style == style);
        public static ContentItem ForCar(string carId) => All.FirstOrDefault(i => i.Kind == ContentKind.Car && i.CarId == carId);
        public static ContentItem ByAssetId(string assetId) => All.FirstOrDefault(i => i.AssetId == assetId);

        /// <summary>Licences a paid game may use (with credit for CC Attribution).</summary>
        public static bool LicenceAllowed(string label) => label == "CC Attribution" || label == "CC0 Public Domain";
    }
}
