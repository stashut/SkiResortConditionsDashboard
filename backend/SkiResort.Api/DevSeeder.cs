using SkiResort.Domain.Entities;
using SkiResort.Infrastructure.Data;

namespace SkiResort.Api;

/// <summary>
/// Seeds the local development database with a worldwide resort list,
/// real GPS coordinates (used by WeatherSyncWorker), and placeholder
/// snow / lift / run data. Runs only when the Resorts table is empty.
/// </summary>
internal static class DevSeeder
{
    private record ResortSeed(
        Guid Id, string Name, string Region, string Country,
        int BaseM, int TopM,
        double Lat, double Lon,
        decimal Depth, decimal NewSnow,
        string L1, bool L1Open, string L2, bool L2Open,
        string R1, bool R1Open, string R2, bool R2Open);

    private static readonly ResortSeed[] Seeds =
    [
        // ── Ukraine ──────────────────────────────────────────────────────────
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Bukovel",             "Carpathians",    "UA",  900, 1372,
            48.3592,  24.3850,  95, 18,
            "Main Gondola",        true,  "Express Quad",           true,
            "Black Diamond Run",   true,  "Main Slope",             true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111112"),
            "Dragobrat",           "Carpathians",    "UA", 1300, 1883,
            48.1614,  24.5156, 125, 28,
            "T-Bar East",          true,  "T-Bar West",             true,
            "Main Descent",        true,  "Eastern Bowl",           false),

        new(Guid.Parse("11111111-1111-1111-1111-111111111113"),
            "Slavske",             "Carpathians",    "UA",  620, 1230,
            49.0194,  23.4458,  70, 10,
            "Main Chairlift",      true,  "Poma Lift",              true,
            "Forest Trail",        true,  "Beginner Slope",         true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111114"),
            "Vorokhta",            "Carpathians",    "UA",  840, 1200,
            48.1000,  24.5333,  68,  9,
            "Vorokhta Chair",      true,  "Dragobrat Link",         false,
            "Main Piste",          true,  "Forest Route",         true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111115"),
            "Zakhar Berkut",       "Carpathians",    "UA",  800, 1100,
            48.5800,  23.4200,  72, 11,
            "Main Lift",           true,  "Training Drag",          true,
            "Competition Slope",   true,  "Family Run",           true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111116"),
            "Pylypets",            "Carpathians",    "UA",  750, 1070,
            48.6200,  23.2700,  65,  8,
            "Pylypets Chair",      true,  "T-Bar North",            true,
            "Main Descent",        true,  "Glade Run",             false),

        new(Guid.Parse("11111111-1111-1111-1111-111111111117"),
            "Migovo",              "Bukovyna",       "UA",  600, 1000,
            48.1480,  25.3420,  55,  6,
            "Migovo Gondola",      true,  "Chairlift",              true,
            "Central Piste",       true,  "Beginner Area",         true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111118"),
            "Krasiya",             "Zakarpattia",    "UA",  550,  980,
            48.9720,  22.6500,  50,  5,
            "Krasiya Lift",        true,  "Drag Lift",              true,
            "Main Trail",          true,  "Narrow Run",            true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111119"),
            "Yasinya",             "Carpathians",    "UA",  850, 1150,
            48.2803,  24.3575,  74, 10,
            "Yasinya Chair",       true,  "Surface Lift",           true,
            "Ridge Run",           true,  "Valley Return",         true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111120"),
            "Izky",                "Carpathians",    "UA",  700, 1050,
            48.3890,  23.3110,  62,  7,
            "Izky Lift",           true,  "Poma",                   true,
            "Woodland Trail",      true,  "Open Slope",            true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111121"),
            "Oryavchyk",           "Carpathians",    "UA",  680, 1180,
            48.9850,  23.4250,  66,  8,
            "Oryavchyk Chair",     true,  "T-Bar",                  true,
            "Long Run",            true,  "Shortcut",              true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111122"),
            "Podobovets",          "Carpathians",    "UA",  720, 1120,
            48.6180,  23.3600,  69,  9,
            "Podobovets Gondola",  true,  "Chair",                  true,
            "Main Face",           true,  "Side Bowl",             false),

        new(Guid.Parse("11111111-1111-1111-1111-111111111123"),
            "Verkhovyna",          "Carpathians",    "UA",  900, 1240,
            48.1550,  24.8110,  71, 10,
            "Verkhovyna Lift",     true,  "Drag",                   true,
            "High Trail",          true,  "Lower Loop",            true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111124"),
            "Tysovets",            "Carpathians",    "UA",  650, 1020,
            48.9800,  23.0200,  58,  6,
            "Tysovets Chair",      true,  "Beginner Lift",          true,
            "North Face",          true,  "Sunny Slope",           true),

        new(Guid.Parse("11111111-1111-1111-1111-111111111125"),
            "Svydovets",           "Carpathians",    "UA",  920, 1280,
            48.2520,  24.3280,  73, 11,
            "Base Gondola",        true,  "Upper Chair",            true,
            "Summit Run",          true,  "Tree Line",             false),

        new(Guid.Parse("11111111-1111-1111-1111-111111111126"),
            "Play Slavske",        "Carpathians",    "UA",  640, 1180,
            49.0280,  23.4520,  67,  9,
            "Play Lift",           true,  "Magic Carpet",           true,
            "Stadium Slope",       true,  "Fun Run",               true),

        // ── Poland ───────────────────────────────────────────────────────────
        new(Guid.Parse("22222222-2222-2222-2222-222222222221"),
            "Zakopane",            "Lesser Poland",  "PL", 1000, 1987,
            49.2992,  19.9496,  88, 14,
            "Kasprowy Gondola",    true,  "Goryczkowa Chair",       true,
            "Hala Gasienicowa",    true,  "Goryczkowy Zjazd",       true),

        new(Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Białka Tatrzańska",   "Lesser Poland",  "PL",  700, 1200,
            49.3514,  20.1247,  75,  8,
            "Kotelnica Gondola",   true,  "White Star Chair",       true,
            "Kotelnica Run",       true,  "Beginner Area",          true),

        new(Guid.Parse("22222222-2222-2222-2222-222222222223"),
            "Szczyrk",             "Silesia",        "PL",  450, 1257,
            49.7167,  19.0333,  60,  5,
            "Gondola Szczyrk",     true,  "Mountain Chair",         true,
            "Skrzyczne Descent",   true,  "Blue Route",             true),

        new(Guid.Parse("22222222-2222-2222-2222-222222222224"),
            "Karpacz",             "Lower Silesia",  "PL",  520, 1350,
            50.7800,  15.7500,  65,  7,
            "Kopa Chairlift",      true,  "Wang Drag",              true,
            "Kopa Descent",        true,  "Bialy Jar",              false),

        new(Guid.Parse("22222222-2222-2222-2222-222222222225"),
            "Szklarska Poręba",    "Lower Silesia",  "PL",  630, 1200,
            50.8283,  15.5183,  70,  9,
            "Szrenica Gondola",    true,  "Hala Szrenicka Drag",    true,
            "Puchatek Run",        true,  "Giant Slalom Course",    false),

        new(Guid.Parse("22222222-2222-2222-2222-222222222226"),
            "Wisła",               "Silesia",        "PL",  620, 1345,
            49.6556,  18.8592,  62,  8,
            "Cieńków Gondola",     true,  "Nowa Osada Chair",       true,
            "Malinowa",            true,  "Stok",                   true),

        new(Guid.Parse("22222222-2222-2222-2222-222222222227"),
            "Krynica-Zdrój",       "Lesser Poland",  "PL",  590, 1020,
            49.4225,  20.9594,  58,  7,
            "Jaworzyna Gondola",   true,  "Chairlift",              true,
            "Main Piste",          true,  "Beginner Zone",         true),

        new(Guid.Parse("22222222-2222-2222-2222-222222222228"),
            "Zieleniec",           "Lower Silesia",  "PL",  800,  960,
            50.3500,  16.3833,  55,  6,
            "Zieleniec Express",   true,  "Drag Lift",              true,
            "Arena Run",           true,  "Family Slope",          true),

        // ── Romania ──────────────────────────────────────────────────────────
        new(Guid.Parse("33333333-3333-3333-3333-333333333331"),
            "Poiana Brasov",       "Transylvania",   "RO", 1030, 1775,
            45.5833,  25.5500,  80, 15,
            "Gondola",             true,  "Ruia Chairlift",         true,
            "Valea Dorului",       true,  "Lupului Run",            true),

        new(Guid.Parse("33333333-3333-3333-3333-333333333332"),
            "Sinaia",              "Prahova Valley", "RO",  810, 2000,
            45.3500,  25.5500,  90, 20,
            "Cota 2000 Gondola",   true,  "Stana Chairlift",        true,
            "Vanatoru Run",        true,  "Drumul Rosu",            true),

        new(Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Predeal",             "Prahova Valley", "RO", 1040, 1455,
            45.5064,  25.5736,  55,  6,
            "Clabucet Gondola",    true,  "Turnu Chair",            true,
            "Clabucet Descent",    true,  "Slalom Course",          false),

        new(Guid.Parse("33333333-3333-3333-3333-333333333334"),
            "Azuga",               "Prahova Valley", "RO",  950, 1800,
            45.4444,  25.5789,  60,  8,
            "Azuga Gondola",       true,  "Chairlift",              true,
            "Sorica Run",          true,  "Training Slope",        true),

        new(Guid.Parse("33333333-3333-3333-3333-333333333335"),
            "Straja",              "Hunedoara",      "RO", 1060, 1868,
            45.3214,  23.2081,  52,  5,
            "Straja Gondola",      true,  "Chair",                  true,
            "Straja Main",         true,  "Night Skiing Run",      true),

        // ── Bulgaria ─────────────────────────────────────────────────────────
        new(Guid.Parse("44444444-4444-4444-4444-444444444441"),
            "Bansko",              "Pirin",          "BG",  936, 2560,
            41.8364,  23.4883, 105, 22,
            "Gondola",             true,  "Plato Express",          true,
            "Bunderishka Polyana", true,  "Tomba Run",              true),

        new(Guid.Parse("44444444-4444-4444-4444-444444444442"),
            "Borovets",            "Rila",           "BG", 1350, 1940,
            42.2667,  23.6000,  75, 12,
            "Yastrebets Gondola",  true,  "Sitnyakovo Drag",        true,
            "Yastrebets Descent",  true,  "Martinovi Baraki",       false),

        // ── Slovakia ─────────────────────────────────────────────────────────
        new(Guid.Parse("55555555-5555-5555-5555-555555555551"),
            "Jasná",               "Low Tatras",     "SK",  930, 2024,
            48.9333,  19.6167, 115, 20,
            "Jasná Gondola",       true,  "Lúčky Express",          true,
            "FIS Downhill",        true,  "Záhradky",               true),

        new(Guid.Parse("55555555-5555-5555-5555-555555555552"),
            "Štrbské Pleso",       "High Tatras",    "SK", 1351, 1840,
            49.1200,  20.0600,  95, 15,
            "Solisko Gondola",     true,  "Chairlift 1",            true,
            "FIS Slalom Piste",    true,  "Informácia Run",         true),

        new(Guid.Parse("55555555-5555-5555-5555-555555555553"),
            "Tatranska Lomnica",   "High Tatras",    "SK",  850, 2196,
            49.1631,  20.2778,  98, 16,
            "Skalnate Pleso Cable",true,  "Chairlift Lomnica",      true,
            "Lomnický štít Access",true,  "Red Piste",             true),

        new(Guid.Parse("55555555-5555-5555-5555-555555555554"),
            "Donovaly",            "Low Tatras",     "SK",  900, 1360,
            48.8828,  19.2264,  64,  9,
            "Nová Hoľa Gondola",   true,  "Záhradište Chair",       true,
            "Fun Park",            true,  "Family Run",            true),

        new(Guid.Parse("55555555-5555-5555-5555-555555555571"),
            "Kranjska Gora",       "Julian Alps",    "SI",  810, 1215,
            46.4847,  13.7828,  78, 14,
            "Kekec Chair",         true,  "Podkoren Gondola",       true,
            "Vitranc",             true,  "Podkoren Downhill",     true),

        new(Guid.Parse("55555555-5555-5555-5555-555555555572"),
            "Krvavec",             "Kamnik Alps",    "SI", 1450, 1971,
            46.2833,  14.5333,  88, 17,
            "Krvavec Gondola",     true,  "Zvoh Chair",             true,
            "Zvoh Run",            true,  "Powder Bowl",           true),

        new(Guid.Parse("55555555-5555-5555-5555-555555555573"),
            "Vogel",               "Julian Alps",    "SI", 1050, 1800,
            46.2667,  13.8500,  92, 18,
            "Vogel Cable Car",     true,  "Orlove Glave Chair",     true,
            "Šija Piste",          true,  "Lake View Run",         true),

        // ── Czech Republic ───────────────────────────────────────────────────
        new(Guid.Parse("66666666-6666-6666-6666-666666666661"),
            "Špindlerův Mlýn",    "Krkonoše",       "CZ",  714, 1310,
            50.7267,  15.6111,  65,  8,
            "Medvědín Gondola",   true,  "Sv. Petr Gondola",       true,
            "Medvědín Descent",   true,  "Super Slalom",           false),

        new(Guid.Parse("66666666-6666-6666-6666-666666666662"),
            "Harrachov",           "Krkonoše",       "CZ",  680, 1012,
            50.7700,  15.4300,  55,  6,
            "Čertova Hora Chair",  true,  "Ski Jump Drag",          false,
            "Čertova Hora Run",    true,  "Beginner Slope",         true),

        // ── Austria ──────────────────────────────────────────────────────────
        new(Guid.Parse("77777777-7777-7777-7777-777777777771"),
            "Kitzbühel",           "Tirol",          "AT",  760, 2000,
            47.4469,  12.3914,  55,  5,
            "Hahnenkamm Gondola",  true,  "3S Bahn",                true,
            "Streif",              true,  "Ganslernhang",           true),

        new(Guid.Parse("77777777-7777-7777-7777-777777777772"),
            "St. Anton am Arlberg","Tirol",          "AT", 1304, 2811,
            47.1297,  10.2686, 125, 32,
            "Galzigbahn Gondola",  true,  "Valluga Cable Car",      true,
            "Valluga Descent",     true,  "Schindler Grat",         false),

        new(Guid.Parse("77777777-7777-7777-7777-777777777773"),
            "Solden",              "Tirol",          "AT", 1350, 3340,
            46.9667,  11.0000, 118, 28,
            "Giggijoch Gondola",   true,  "Tiefenbach Glacier",     true,
            "Rettenbach Glacier",  true,  "Schwarze Schneid",      true),

        new(Guid.Parse("77777777-7777-7777-7777-777777777774"),
            "Lech Zurs",           "Vorarlberg",     "AT", 1450, 2450,
            47.2167,  10.1333, 112, 24,
            "Rüfikopf Cable Car",  true,  "Flexenbahn",             true,
            "White Ring",          true,  "Madloch",               true),

        // ── Switzerland ──────────────────────────────────────────────────────
        new(Guid.Parse("88888888-8888-8888-8888-888888888881"),
            "Zermatt",             "Valais",         "CH", 1620, 3883,
            46.0207,   7.7491, 155, 38,
            "Matterhorn Glacier Gondola", true, "Sunnegga Express", true,
            "Schwarzsee–Zermatt",  true,  "Triftji",                true),

        new(Guid.Parse("88888888-8888-8888-8888-888888888882"),
            "Verbier",             "Valais",         "CH", 1500, 3330,
            46.0961,   7.2281, 135, 28,
            "Médran Gondola",      true,  "Les Ruinettes Chair",    true,
            "Attelas Descent",     true,  "Nendaz Piste",           true),

        // ── France ───────────────────────────────────────────────────────────
        new(Guid.Parse("99999999-9999-9999-9999-999999999991"),
            "Val Thorens",         "Savoie",         "FR", 2300, 3200,
            45.2978,   6.5800, 145, 35,
            "Funitel de Péclet",   true,  "Cime de Caron Cable Car",true,
            "Col de l'Audzin",     true,  "Combe de Caron",         true),

        new(Guid.Parse("99999999-9999-9999-9999-999999999992"),
            "Chamonix",            "Haute-Savoie",   "FR", 1035, 3842,
            45.9237,   6.8694, 100, 20,
            "Aiguille du Midi Cable Car", true, "Brévent Gondola",  true,
            "Vallée Blanche",      true,  "Verte Run",              true),

        new(Guid.Parse("99999999-9999-9999-9999-999999999993"),
            "Val d'Isere",         "Savoie",         "FR", 1850, 3456,
            45.4500,   6.9800, 130, 30,
            "Olympique Gondola",   true,  "Funival Funicular",      true,
            "Face de Bellevarde",  true,  "OK Piste",              true),

        new(Guid.Parse("99999999-9999-9999-9999-999999999994"),
            "Alpe d'Huez",         "Isere",          "FR", 1860, 3330,
            45.0928,   6.0694, 105, 22,
            "DMC Gondola",         true,  "Pic Blanc Cable Car",    true,
            "Sarenne",             true,  "Tunnel Run",            true),

        new(Guid.Parse("10101010-1010-1010-1010-101010101001"),
            "Cortina d'Ampezzo",   "Dolomites",      "IT", 1224, 2930,
            46.5369,  12.1356,  95, 19,
            "Faloria Cable Car",   true,  "Tofana Cable Car",       true,
            "Olympia",             true,  "Forcella Staunies",     true),

        new(Guid.Parse("10101010-1010-1010-1010-101010101002"),
            "Livigno",             "Lombardy",       "IT", 1816, 2797,
            46.5367,  10.1333, 102, 21,
            "Carosello 3000",      true,  "Mottolino Gondola",      true,
            "Black Slopes",        true,  "Costaccia",             true),

        new(Guid.Parse("10101010-1010-1010-1010-101010101003"),
            "Madonna di Campiglio","Trentino",       "IT",  850, 2500,
            46.2300,  10.8264,  88, 16,
            "Spinale Gondola",     true,  "Pradalago Gondola",      true,
            "3-Tre Slalom",        true,  "Canalone Miramonti",    true),

        new(Guid.Parse("10101010-1010-1010-1010-101010101010"),
            "Garmisch-Classic",    "Bavaria",        "DE",  740, 2050,
            47.4917,  11.0989,  72, 12,
            "Hausberg Gondola",    true,  "Kreuzeckbahn",           true,
            "Kandahar",            true,  "Olympiaabfahrt",        true),

        new(Guid.Parse("20202020-2020-2020-2020-202020202021"),
            "Sierra Nevada",       "Andalusia",      "ES", 2100, 3300,
            37.0939,  -3.3989,  45,  8,
            "Borreguiles Gondola", true,  "Veleta Cable Car",       true,
            "Loma Dilar",          true,  "Maribel",               true),

        new(Guid.Parse("20202020-2020-2020-2020-202020202022"),
            "Baqueira Beret",      "Catalonia",      "ES", 1500, 2510,
            42.7000,   0.9333,  68, 14,
            "Baqueira Gondola",    true,  "Beret Chair",            true,
            "Tuc de Beret",        true,  "Bonaigua",              true),

        new(Guid.Parse("30303030-3030-3030-3030-303030303031"),
            "Grandvalira",         "Pyrenees",       "AD", 1710, 2640,
            42.5670,   1.6670,  62, 10,
            "Funicamp",            true,  "Pas de la Casa Gondola", true,
            "Avet",                true,  "Tortes",                true),

        new(Guid.Parse("40404040-4040-4040-4040-404040404041"),
            "Sljeme",              "Zagreb County",  "HR",  985, 1035,
            45.9000,  15.9500,  35,  4,
            "Sljeme Cable Car",    true,  "Chairlift",              true,
            "Crveni Spust",        true,  "Family Trail",          true),

        new(Guid.Parse("50505050-5050-5050-5050-505050505051"),
            "Kopaonik",            "Central Serbia", "RS", 1100, 2017,
            43.2800,  20.8100,  58,  9,
            "Pancicev Vrh Gondola",true,  "Marmot Chair",           true,
            "Sun Valley",          true,  "Karaman",                 true),

        new(Guid.Parse("50505050-5050-5050-5050-505050505052"),
            "Zlatibor",            "Western Serbia", "RS", 1000, 1496,
            43.7300,  19.7200,  42,  6,
            "Tornik Gondola",      true,  "Ski Lift",               true,
            "Tornik Piste",        true,  "Beginner Slope",        true),

        new(Guid.Parse("60606060-6060-6060-6060-606060606061"),
            "Trysil",              "Innlandet",      "NO",  415, 1100,
            61.3100,  12.2667,  55, 18,
            "Express Gondola",     true,  "Turisten Chair",         true,
            "Høgegga",             true,  "Fageråsen",             true),

        new(Guid.Parse("60606060-6060-6060-6060-606060606062"),
            "Hemsedal",            "Buskerud",       "NO",  625, 1450,
            60.8667,   8.5500,  78, 22,
            "Hemsedal Express",    true,  "Tottenlift",             true,
            "Totteskogen",         true,  "Ungdommen",             true),

        new(Guid.Parse("70707070-7070-7070-7070-707070707071"),
            "Åre",                 "Jämtland",       "SE",  380, 1420,
            63.3990,  13.0810,  65, 16,
            "VM8 Gondola",         true,  "Kabinbane",              true,
            "VM-Störtloppet",      true,  "Larsslalommen",         true),

        new(Guid.Parse("80808080-8080-8080-8080-808080808081"),
            "Levi",                "Lapland",        "FI",  200,  531,
            67.8050,  24.8080,  48, 12,
            "Levi Gondola",        true,  "Express Chair",          true,
            "Levi Black",          true,  "South Slopes",          true),

        new(Guid.Parse("80808080-8080-8080-8080-808080808082"),
            "Ruka",                "Northern Ostrobothnia","FI",291,492,
            66.1700,  29.1500,  52, 15,
            "Ruka Gondola",        true,  "Vuosseli Chair",         true,
            "Ruka Park",           true,  "Family Slopes",         true),

        new(Guid.Parse("90909090-9090-9090-9090-909090909091"),
            "Parnassos",           "Central Greece", "GR", 1600, 2457,
            38.5660,  22.6170,  35,  5,
            "Kelaria Gondola",     true,  "Fterolakka Chair",       true,
            "Kelifos",             true,  "Vergos",                true),

        // ── Canada ───────────────────────────────────────────────────────────
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Whistler Blackcomb",  "British Columbia","CA",  657, 2182,
            50.1163, -122.9574, 185, 48,
            "Gondola 1",           true,  "Peak 2 Peak Gondola",    true,
            "Dave Murray Downhill",true,  "Franz's Run",            true),

        // ── Japan ────────────────────────────────────────────────────────────
        new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Niseko United",       "Hokkaido",       "JP",  308, 1308,
            42.7753, 140.6875, 255, 65,
            "Niseko Village Gondola",true, "Annupuri Gondola",      true,
            "Yamabuki",            true,  "Ace Family Course",      true),

        // ── United States ────────────────────────────────────────────────────
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "North Peak",          "Colorado",       "US",  420, 1650,
            40.0000, -105.5000,  85, 12,
            "North Gondola",       true,  "Express Quad",           true,
            "Green Trail",         true,  "Blue Line",              true),

        new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "Alpine Ridge",        "Colorado",       "US",  560, 2140,
            40.5000, -106.0000,  72,  9,
            "Ridge Gondola",       true,  "Summit Chair",           true,
            "Ridge Run",           true,  "Powder Bowl",            false),

        new(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            "Vail",                "Colorado",       "US", 2457, 3527,
            39.6433, -106.3753, 125, 26,
            "Eagle Bahn Gondola",  true,  "Gondola 1",              true,
            "Blue Sky Basin",      true,  "Riva Ridge",             true),

        new(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            "Park City",           "Utah",           "US", 2100, 3048,
            40.6514, -111.5081, 110, 22,
            "Quicksilver Gondola", true,  "Orange Bubble Chair",    true,
            "Signature Runs",      true,  "Jupiter Bowl",           false),

        new(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0001"),
            "Jackson Hole",        "Wyoming",        "US", 1924, 3185,
            43.5875, -110.8278, 145, 36,
            "Aerial Tram",         true,  "Bridger Gondola",        true,
            "Corbet's Couloir",    false, "Rendezvous Run",         true),

        new(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0002"),
            "Mammoth Mountain",      "California",     "US", 2424, 3369,
            37.6308, -119.0326,  95, 20,
            "Panorama Gondola",    true,  "Chair 23",               true,
            "Dave's Run",          true,  "Climax",                true),
    ];

    public static void Seed(SkiResortDbContext db)
    {
        if (db.Resorts.Any()) return;

        var now = DateTimeOffset.UtcNow;

        foreach (var s in Seeds)
        {
            db.Resorts.Add(new Resort
            {
                Id = s.Id, Name = s.Name, Region = s.Region, Country = s.Country,
                ElevationBaseMeters = s.BaseM, ElevationTopMeters = s.TopM,
                LatitudeDeg = s.Lat, LongitudeDeg = s.Lon
            });
            db.SnowConditions.Add(new SnowCondition
            {
                Id = Guid.NewGuid(), ResortId = s.Id,
                ObservedAt = now.AddHours(-1), SnowDepthCm = s.Depth, NewSnowCm = s.NewSnow
            });
            db.LiftStatuses.AddRange(
                new LiftStatus { Id = Guid.NewGuid(), ResortId = s.Id, Name = s.L1, IsOpen = s.L1Open, UpdatedAt = now.AddMinutes(-5) },
                new LiftStatus { Id = Guid.NewGuid(), ResortId = s.Id, Name = s.L2, IsOpen = s.L2Open, UpdatedAt = now.AddMinutes(-5) }
            );
            db.RunStatuses.AddRange(
                new RunStatus { Id = Guid.NewGuid(), ResortId = s.Id, Name = s.R1, IsOpen = s.R1Open, UpdatedAt = now.AddMinutes(-3) },
                new RunStatus { Id = Guid.NewGuid(), ResortId = s.Id, Name = s.R2, IsOpen = s.R2Open, UpdatedAt = now.AddMinutes(-3) }
            );
        }

        db.SaveChanges();
    }
}
