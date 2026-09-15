using System.IO;
using UnityEditor;
using UnityEngine;

namespace SchoolDay.Editor
{
    public static class SchoolDayBaker
    {
        const string DataRoot = "Assets/Game/Data";
        const string LookRoot = "Assets/Game/Data/Looks";
        const string ChoiceRoot = "Assets/Game/Data/Choices";
        const string BeatRoot = "Assets/Game/Data/Beats";
        const string PoolRoot = "Assets/Game/Data/Pools";
        const string ArtCharacter = "Assets/Game/Art/Character";
        const string ArtBackgrounds = "Assets/Game/Art/Backgrounds";
        const string ArtIcons = "Assets/Game/Art/Icons";
        const string ArtUi = "Assets/Game/Art/UI";
        const string WeekBoardPath = "Assets/Game/Data/WeekBoard.asset";
        const string PromoRoot = "Assets/Game/Data/Promos";
        const string PromoCatalogPath = "Assets/Game/Data/PromoCatalog.asset";

        [MenuItem("School Day/Seed Content If Missing")]
        public static string SeedContentIfMissing()
        {
            ImportSprites();
            EnsureArtSet();
            EnsureLooks();
            EnsureMetrics();
            DayConfig monday = EnsureMonday();
            EnsureWeekContent(monday);
            WireWeekBoard();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.GetAssetPath(monday);
        }

        static void ImportSprites()
        {
            string[] folders = { ArtCharacter, ArtBackgrounds, ArtIcons, ArtUi };
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                        continue;

                    bool uiSlice = path.Contains("/UI/btn_") || path.Contains("/UI/hud_panel");
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.spritePixelsPerUnit = 100f;
                    if (uiSlice)
                    {
                        importer.spriteBorder = new Vector4(32f, 32f, 32f, 32f);
                    }

                    importer.SaveAndReimport();
                }
            }
        }

        static ArtSet EnsureArtSet()
        {
            ArtSet art = LoadOrCreate<ArtSet>(DataRoot + "/ArtSet.asset");
            art.HudPanel = SpriteAt(ArtUi + "/hud_panel.png");
            art.BadgeBufferKid = SpriteAt(ArtUi + "/badge_buffer_kid.png");
            art.ButtonNormal = SpriteAt(ArtUi + "/btn_normal.png");
            art.ButtonConfirm = SpriteAt(ArtUi + "/btn_confirm.png");
            EditorUtility.SetDirty(art);
            return art;
        }

        static void EnsureLooks()
        {
            CharacterLook boy = LoadOrCreate<CharacterLook>(LookRoot + "/Boy.asset");
            if (string.IsNullOrEmpty(boy.DisplayName))
                boy.DisplayName = "Boy";
            boy.Idle = SpriteAt(ArtCharacter + "/student_idle.png");
            boy.HighEnergyReputation = SpriteAt(ArtCharacter + "/student_high_energy_reputation.png");
            boy.LowEnergy = SpriteAt(ArtCharacter + "/student_low_energy.png");
            boy.LowReputation = SpriteAt(ArtCharacter + "/student_low_reputation.png");
            boy.IdleFrames = Sprites3("student_idle");
            boy.LowEnergyFrames = Sprites3("student_low_energy");
            boy.LowReputationFrames = Sprites3("student_low_reputation");
            boy.HighEnergyReputationFrames = Sprites3("student_high_energy_reputation");
            boy.HudFace = SpriteAt(ArtIcons + "/hud_face.png");
            EditorUtility.SetDirty(boy);

            CharacterLook girl = LoadOrCreate<CharacterLook>(LookRoot + "/Girl.asset");
            if (string.IsNullOrEmpty(girl.DisplayName))
                girl.DisplayName = "Girl";
            girl.Idle = SpriteAt(ArtCharacter + "/student_girl_idle.png");
            girl.HighEnergyReputation = SpriteAt(ArtCharacter + "/student_girl_high_energy_reputation.png");
            girl.LowEnergy = SpriteAt(ArtCharacter + "/student_girl_low_energy.png");
            girl.LowReputation = SpriteAt(ArtCharacter + "/student_girl_low_reputation.png");
            girl.IdleFrames = Sprites3("student_girl_idle");
            girl.LowEnergyFrames = Sprites3("student_girl_low_energy");
            girl.LowReputationFrames = Sprites3("student_girl_low_reputation");
            girl.HighEnergyReputationFrames = Sprites3("student_girl_high_energy_reputation");
            girl.HudFace = SpriteAt(ArtIcons + "/hud_face_girl.png");
            EditorUtility.SetDirty(girl);
        }

        static Sprite[] Sprites3(string prefix)
        {
            return new Sprite[]
            {
                SpriteAt(ArtCharacter + "/" + prefix + "_0.png"),
                SpriteAt(ArtCharacter + "/" + prefix + "_1.png"),
                SpriteAt(ArtCharacter + "/" + prefix + "_2.png")
            };
        }

        static MetricsConfig EnsureMetrics()
        {
            MetricsConfig metrics = LoadOrCreate<MetricsConfig>(DataRoot + "/MetricsConfig.asset");
            if (string.IsNullOrEmpty(metrics.EventsPath))
                metrics.EventsPath = "/events";
            if (metrics.TimeoutSeconds <= 0f)
                metrics.TimeoutSeconds = 3f;
            EditorUtility.SetDirty(metrics);
            return metrics;
        }

        static DayConfig EnsureMonday()
        {
            DayConfig config = LoadOrCreate<DayConfig>(DataRoot + "/Monday.asset");

            ChoiceData skip = Choice("breakfast_skip", "Skip breakfast", "Just teh, I'm late.", 0f, -20f, -4f, 0f, LeakTag.SkipMeal, OverspendRule.Block, "icon_skip_meal.png");
            ChoiceData nasi = Choice("breakfast_nasi", "Canteen nasi lemak", "Auntie's pack, still hot.", 4f, 16f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_nasi.png");
            ChoiceData toast = Choice("breakfast_toastbox", "Toast Box", "Kaya toast set. Treat myself.", 5.5f, 15f, 5f, 0f, LeakTag.None, OverspendRule.Block, "icon_toastbox.png", null, "cafe");
            ChoiceData walk = Choice("transit_walk", "Walk", "I'll make it if I hustle.", 0f, -10f, -4f, 1f, LeakTag.None, OverspendRule.Block, "icon_walk.png");
            ChoiceData bus = Choice("transit_bus", "Bus", "EZ-Link tap. Safe.", 0.7f, 3f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_bus.png");
            ChoiceData grab = Choice("transit_grab", "Grab", "Car's here. I'll make assembly.", 9f, 5f, 2f, 0f, LeakTag.Grab, OverspendRule.AllowBroke, "icon_grab.png", null, "ride");
            ChoiceData drink = Choice("fomo_gongcha", "Gong Cha", "Everyone's already in the queue.", 4.8f, 8f, 10f, 0f, LeakTag.Drinks, OverspendRule.Block, "icon_gongcha.png", null, "bubbletea");
            ChoiceData snack = Choice("fomo_snack", "$1 snack", "School bookshop bun.", 1f, 6f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_snack.png");
            ChoiceData next = Choice("fomo_next", "Next payday", "Maybe after test week.", 0f, 0f, -12f, 0f, LeakTag.None, OverspendRule.Block, "icon_next_payday.png");
            ChoiceData shirt = Choice("shock_cca", "CCA T-shirt", "Miss Tan said sizes close Friday.", 8f, 0f, 7f, 0f, LeakTag.None, OverspendRule.Decline, "icon_cca_shirt.png", "bg_shock_cca.png");
            ChoiceData rice = Choice("shock_rice", "Help buy rice", "Mum: can you get the 2kg?", 9.95f, 0f, 9f, 0f, LeakTag.None, OverspendRule.Decline, "icon_rice.png", "bg_shock_home.png");
            ChoiceData start = Choice("title_start", "Time for school", "Time for school.", 0f, 0f, 0f, 0f, LeakTag.None, OverspendRule.Block, null);

            BeatData title = Beat("title_monday", "School Day", "$8 in pocket.", SpeakerKind.Title, BeatPhase.Title, null, start);
            BeatData breakfast = Beat("breakfast", "Breakfast", "Auntie already packed the trays. What are you eating?", SpeakerKind.Auntie, BeatPhase.Trade, "bg_canteen.png", skip, nasi, toast);
            BeatData transit = Beat("transit", "Transit", "Bell's in twenty. How are you getting in?", SpeakerKind.Teacher, BeatPhase.Trade, "bg_bus.png", walk, bus, grab);
            BeatData fomo = Beat("fomo", "After school", "They're already downstairs. You coming?", SpeakerKind.Friend, BeatPhase.Trade, "bg_gongcha.png", drink, snack, next);
            BeatData shock = Beat("shock", "Then this.", "Something you didn't budget for.", SpeakerKind.Teacher, BeatPhase.Shock, "bg_shock_home.png", shirt, rice);

            if (config.Beats == null || config.Beats.Length == 0)
            {
                config.StartingPocket = 8f;
                config.StartingFuel = 70f;
                config.StartingFace = 55f;
                config.StartingBuffer = 8f;
                config.OvernightFuelDrop = 20f;
                config.FuelMetThreshold = 45f;
                config.BufferWin = 2f;
                config.StatFloor = 0f;
                config.StatCap = 100f;
                config.LowFuelThreshold = 30f;
                config.WalkLowFuelExtraHit = -6f;
                config.DebtReputationPerDollar = 2f;
                config.DebtReputationZeroAt = 8f;
                config.ShockSkipFaceHit = 12f;
                config.ShockPayFaceGain = 6f;
                config.ReputationPunctualWeight = 1f;
                config.ReputationFriendlyWeight = 1f;
                config.ReputationAdaptiveWeight = 1f;
                config.ReputationGenerousWeight = 1f;
                config.ReputationLeakKeepScale = 0.5f;
                config.ReputationGenerousPending = 50f;
                config.ShockSeed = 0;
                config.LeakPriority = new[] { LeakTag.Grab, LeakTag.Drinks, LeakTag.SkipMeal };
                config.Beats = new[] { title, breakfast, transit, fomo, shock };
                config.CoachLines = DefaultCoachLines();
                config.GrabLeakName = "Grab";
                config.DrinksLeakName = "drinks";
                config.SkipMealLeakName = "skip meal";
                config.NoneLeakName = "none";
                config.AuntieName = "Canteen auntie";
                config.FriendName = "Your friend";
                config.MumName = "Mum";
                config.TeacherName = "Form teacher";
                config.TitleSpeakerName = "6:40am";
                config.WakeEarliestMinute = WakeTime.DefaultEarliestMinute;
                config.WakeLatestMinute = WakeTime.DefaultLatestMinute;
                config.BudgetTip = "Budget";
                config.EnergyTip = "Energy";
                config.ReputationTip = "Reputation";
                config.KeepTip = "Keep $2";
            }

            EditorUtility.SetDirty(config);
            return config;
        }

        static CoachLine[] DefaultCoachLines()
        {
            return new[]
            {
                Line(SpeakerKind.Teacher, LeakTag.Grab, false, false, false, "Grab is not a bus. Your pocket noticed."),
                Line(SpeakerKind.Auntie, LeakTag.Drinks, false, false, false, "Gong Cha can wait. Recess already fed you."),
                Line(SpeakerKind.Auntie, LeakTag.SkipMeal, false, false, false, "Canteen food is cheaper than going hungry all morning."),
                Line(SpeakerKind.Teacher, LeakTag.None, true, false, false, "Keep that $2. Something always comes up after school."),
                Line(SpeakerKind.Teacher, LeakTag.None, false, true, false, "Empty pocket before the last bell. Next Monday, lock a buffer first."),
                Line(SpeakerKind.Teacher, LeakTag.None, false, false, true, "You got through, but you were running on fumes."),
                Line(SpeakerKind.Teacher, LeakTag.None, false, false, false, "Energy first, then reputation, then the drink.")
            };
        }

        static CoachLine Line(SpeakerKind speaker, LeakTag leak, bool bufferKid, bool broke, bool fuelMiss, string text)
        {
            return new CoachLine
            {
                Speaker = speaker,
                WhenLeak = leak,
                RequireBufferKid = bufferKid,
                RequireBroke = broke,
                RequireFuelMiss = fuelMiss,
                Line = text
            };
        }

        static void EnsureWeekContent(DayConfig monday)
        {
            ChoiceData skip = Choice("breakfast_skip", "Skip breakfast", "Just teh, I'm late.", 0f, -20f, -4f, 0f, LeakTag.SkipMeal, OverspendRule.Block, "icon_skip_meal.png");
            ChoiceData nasi = Choice("breakfast_nasi", "Canteen nasi lemak", "Auntie's pack, still hot.", 4f, 16f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_nasi.png");
            ChoiceData toast = Choice("breakfast_toastbox", "Toast Box", "Kaya toast set. Treat myself.", 5.5f, 15f, 5f, 0f, LeakTag.None, OverspendRule.Block, "icon_toastbox.png", null, "cafe");
            ChoiceData home = Choice("breakfast_home", "Packed from home", "Mum packed last night.", 0f, 10f, 3f, 0f, LeakTag.None, OverspendRule.Block, "icon_home_packed.png");
            ChoiceData beehoon = Choice("breakfast_beehoon", "Canteen bee hoon", "$3, still got gravy.", 3f, 10f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_beehoon.png");
            ChoiceData caipng = Choice("breakfast_caipng", "Cai png", "Two veg, one meat.", 3.5f, 12f, 3f, 0f, LeakTag.None, OverspendRule.Block, "icon_caipng.png");
            ChoiceData mcd = Choice("breakfast_mcd", "McDonald's breakfast", "McMuffin set. Treat myself.", 6.5f, 16f, 7f, 0f, LeakTag.None, OverspendRule.Block, "icon_mcd.png", "bg_mcd.png", "fastfood");
            ChoiceData milo = Choice("breakfast_milopeng", "Milo peng", "Cold milo. Cheap fuel.", 1.8f, 8f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_milopeng.png");

            ChoiceData walk = Choice("transit_walk", "Walk", "I'll make it if I hustle.", 0f, -10f, -4f, 1f, LeakTag.None, OverspendRule.Block, "icon_walk.png");
            ChoiceData bus = Choice("transit_bus", "Bus", "EZ-Link tap. Safe.", 0.7f, 3f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_bus.png");
            ChoiceData grab = Choice("transit_grab", "Grab", "Car's here. I'll make assembly.", 9f, 5f, 2f, 0f, LeakTag.Grab, OverspendRule.AllowBroke, "icon_grab.png", null, "ride");
            ChoiceData mrt = Choice("transit_mrt", "MRT", "Tap in. No rain.", 0.7f, 3f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_mrt.png", "bg_mrt.png");
            ChoiceData friendCar = Choice("transit_friendcar", "Friend's car", "He can drop you. You pay petrol.", 3f, 4f, 5f, 0f, LeakTag.None, OverspendRule.Block, "icon_friendcar.png");
            ChoiceData grabRide = Choice("transit_grabfood_ride", "GrabFood ride", "Ride plus a pack. Faster than the queue.", 10f, 8f, 3f, 0f, LeakTag.Grab, OverspendRule.AllowBroke, "icon_grab.png", null, "ride");
            ChoiceData lateBus = Choice("transit_latebus", "Express bus", "Faster, $1.20.", 1.2f, 5f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_latebus.png");

            ChoiceData drink = Choice("fomo_gongcha", "Gong Cha", "Everyone's already in the queue.", 4.8f, 8f, 10f, 0f, LeakTag.Drinks, OverspendRule.Block, "icon_gongcha.png", null, "bubbletea");
            ChoiceData snack = Choice("fomo_snack", "$1 snack", "School bookshop bun.", 1f, 6f, 2f, 0f, LeakTag.None, OverspendRule.Block, "icon_snack.png");
            ChoiceData next = Choice("fomo_next", "Next payday", "Maybe after test week.", 0f, 0f, -12f, 0f, LeakTag.None, OverspendRule.Block, "icon_next_payday.png");
            ChoiceData liho = Choice("fomo_liho", "LiHO", "Queue is out the door.", 5.2f, 10f, 8f, 0f, LeakTag.Drinks, OverspendRule.Block, "icon_liho.png", "bg_kopitiam.png", "bubbletea");
            ChoiceData kfc = Choice("fomo_kfc", "Sharing KFC", "Just one piece.", 4.6f, 9f, 6f, 0f, LeakTag.None, OverspendRule.Block, "icon_kfc.png", null, "fastfood");
            ChoiceData arcade = Choice("fomo_arcade", "Arcade", "Two games.", 6f, -5f, 11f, 0f, LeakTag.None, OverspendRule.Block, "icon_arcade.png", "bg_arcade.png", "fun");
            ChoiceData movie = Choice("fomo_movie", "Last-minute movie", "They already bought seats.", 6f, 3f, 6f, 0f, LeakTag.None, OverspendRule.Block, "icon_movie.png", null, "fun");
            ChoiceData stay = Choice("fomo_stayback", "Stay back / study", "Study. Friends notice. Budget safe.", 0f, 2f, -6f, 0f, LeakTag.None, OverspendRule.Block, "icon_stayback.png");

            ChoiceData shirt = Choice("shock_cca", "CCA T-shirt", "Miss Tan said sizes close Friday.", 8f, 0f, 7f, 0f, LeakTag.None, OverspendRule.Decline, "icon_cca_shirt.png", "bg_shock_cca.png");
            ChoiceData rice = Choice("shock_rice", "Help buy rice", "Mum: can you get the 2kg?", 9.95f, 0f, 9f, 0f, LeakTag.None, OverspendRule.Decline, "icon_rice.png", "bg_shock_home.png");
            ChoiceData printer = Choice("shock_printer", "Print notes", "Shop: 20 cents a page, you need 20.", 4f, 0f, 6f, 0f, LeakTag.None, OverspendRule.Decline, "icon_printer.png", "bg_shock_printer.png");
            ChoiceData trip = Choice("shock_fieldtrip", "Field trip form", "Due tomorrow.", 6f, 0f, 7f, 0f, LeakTag.None, OverspendRule.Decline, "icon_fieldtrip.png", "bg_shock_field.png");
            ChoiceData phone = Choice("shock_phone", "Phone cable", "Yours died at recess.", 4f, 0f, 6f, 0f, LeakTag.None, OverspendRule.Decline, "icon_phone.png", "bg_shock_phone.png");
            ChoiceData sibling = Choice("shock_sibling", "Sibling's textbook", "Mum: can you cover first?", 7f, 0f, 8f, 0f, LeakTag.None, OverspendRule.Decline, "icon_sibling.png", "bg_shock_home.png");
            ChoiceData clinic = Choice("shock_clinic", "Polyclinic copay", "Not well after skip breakfast.", 8.5f, 8f, 6f, 0f, LeakTag.None, OverspendRule.Decline, "icon_clinic.png", "bg_shock_clinic.png");
            ChoiceData birthday = Choice("shock_birthday", "Friend's birthday gift", "They all chipped $5.", 5f, 0f, 10f, 0f, LeakTag.None, OverspendRule.Decline, "icon_birthday.png", "bg_shock_birthday.png");
            ChoiceData photo = Choice("shock_photo", "Class photo", "Form teacher: $5 or you are not in the print.", 5f, 0f, 8f, 0f, LeakTag.None, OverspendRule.Decline, "icon_class_photo.png", "bg_shock_photo.png");
            ChoiceData shoes = Choice("shock_peshoes", "PE shoes", "Yours split at the toe. New pair today.", 16.9f, 0f, 8f, 0f, LeakTag.None, OverspendRule.Decline, "icon_pe_shoes.png", "bg_shock_shoes.png");
            ChoiceData drive = Choice("shock_thumbdrive", "Thumb drive", "ICT: save the file or lose the marks.", 8f, 0f, 6f, 0f, LeakTag.None, OverspendRule.Decline, "icon_thumbdrive.png", "bg_shock_thumbdrive.png");
            ChoiceData concession = Choice("shock_concession", "Concession top-up", "Card is empty. Top up $5 or walk.", 5f, 0f, 6f, 0f, LeakTag.None, OverspendRule.Decline, "icon_concession.png", "bg_shock_concession.png");

            ChoicePool breakfastPool = Pool(
                "BreakfastPool",
                true,
                new[] { skip, nasi, toast, home, beehoon, caipng, mcd, milo },
                Slot("Skip-like", true, 0f, 2f, false, false, true, LeakTag.SkipMeal, false, OverspendRule.Block),
                Slot("Canteen", true, 0f, 4.5f, false, false, true, LeakTag.None, false, OverspendRule.Block),
                Slot("Treat", true, 5f, 7.5f, false, false, false, LeakTag.None, false, OverspendRule.Block));

            ChoicePool transitPool = Pool(
                "TransitPool",
                true,
                new[] { walk, bus, grab, mrt, friendCar, grabRide, lateBus },
                Slot("Walk", true, 0f, 0f, true, false, false, LeakTag.None, false, OverspendRule.Block),
                Slot("Public tap", true, 0.7f, 1.5f, false, false, false, LeakTag.None, false, OverspendRule.Block),
                Slot("Paid ride", true, 3f, 12f, false, false, false, LeakTag.None, false, OverspendRule.Block));

            ChoicePool fomoPool = Pool(
                "FomoPool",
                true,
                new[] { drink, snack, next, liho, kfc, arcade, movie, stay },
                Slot("Social", true, 4.5f, 8f, false, false, false, LeakTag.None, false, OverspendRule.Block),
                Slot("Cheap", true, 1f, 2f, false, false, false, LeakTag.None, false, OverspendRule.Block),
                Slot("Decline", true, 0f, 0f, false, true, false, LeakTag.None, false, OverspendRule.Block));

            ChoicePool shockPool = Pool(
                "ShockPool",
                false,
                new[] { shirt, rice, printer, trip, phone, sibling, clinic, birthday, photo, shoes, drive, concession },
                Slot("Shock bill", true, 4f, 17f, false, false, false, LeakTag.None, true, OverspendRule.Decline));
            MergeItems(shockPool, shirt, rice, printer, trip, phone, sibling, clinic, birthday, photo, shoes, drive, concession);

            EnsureAchievements(shirt, rice, printer, trip, phone, sibling, clinic, birthday, photo, shoes, drive, concession);

            WeekBoard board = LoadOrCreate<WeekBoard>(WeekBoardPath);
            if (board.Day == null)
                board.Day = monday;
            if (string.IsNullOrEmpty(board.TitleHeadingFormat) || board.TitleHeadingFormat == "{0}." || board.TitleHeadingFormat == "Monday" || board.TitleHeadingFormat == "Monday.")
                board.TitleHeadingFormat = "School Day";
            if (string.IsNullOrEmpty(board.TitleBodyFormat)
                || board.TitleBodyFormat == "{0}. {1} in pocket."
                || board.TitleBodyFormat == "{1} in pocket. Ready for school."
                || board.TitleBodyFormat == "School Day. {1} in pocket.")
                board.TitleBodyFormat = "{1} in pocket.";
            if (board.Breakfast.Beat == null)
                board.Breakfast = Bind("breakfast", breakfastPool);
            if (board.Transit.Beat == null)
                board.Transit = Bind("transit", transitPool);
            if (board.Fomo.Beat == null)
                board.Fomo = Bind("fomo", fomoPool);
            if (board.Shock.Beat == null)
                board.Shock = Bind("shock", shockPool);
            PromoCatalog catalog = EnsurePromos();
            if (board.Promos == null)
                board.Promos = catalog;
            EditorUtility.SetDirty(board);
        }

        static void WireWeekBoard()
        {
            DayDirector director = UnityEngine.Object.FindFirstObjectByType<DayDirector>();
            if (director == null)
                return;

            WeekBoard board = AssetDatabase.LoadAssetAtPath<WeekBoard>(WeekBoardPath);
            if (board == null)
                return;

            var so = new SerializedObject(director);
            SerializedProperty property = so.FindProperty("weekBoard");
            if (property == null || property.objectReferenceValue != null)
                return;

            property.objectReferenceValue = board;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);
        }

        static BeatBinding Bind(string beatId, ChoicePool pool)
        {
            return new BeatBinding
            {
                Beat = AssetDatabase.LoadAssetAtPath<BeatData>(BeatRoot + "/" + beatId + ".asset"),
                Pool = pool
            };
        }

        static ChoicePool Pool(string fileName, bool shuffle, ChoiceData[] items, params PoolSlotRule[] slots)
        {
            ChoicePool pool = LoadOrCreate<ChoicePool>(PoolRoot + "/" + fileName + ".asset");
            if (pool.Items == null || pool.Items.Length == 0)
                pool.Items = items;
            if (pool.Slots == null || pool.Slots.Length == 0)
                pool.Slots = slots;
            pool.ShuffleOrder = shuffle;
            EditorUtility.SetDirty(pool);
            return pool;
        }

        static void MergeItems(ChoicePool pool, params ChoiceData[] extras)
        {
            if (pool == null)
                return;

            var list = new System.Collections.Generic.List<ChoiceData>();
            var have = new System.Collections.Generic.HashSet<string>();
            if (pool.Items != null)
            {
                for (int i = 0; i < pool.Items.Length; i++)
                {
                    ChoiceData item = pool.Items[i];
                    if (item == null)
                        continue;
                    list.Add(item);
                    if (!string.IsNullOrEmpty(item.Id))
                        have.Add(item.Id);
                }
            }

            bool changed = false;
            for (int i = 0; i < extras.Length; i++)
            {
                ChoiceData extra = extras[i];
                if (extra == null || have.Contains(extra.Id))
                    continue;
                list.Add(extra);
                if (!string.IsNullOrEmpty(extra.Id))
                    have.Add(extra.Id);
                changed = true;
            }

            if (changed)
            {
                pool.Items = list.ToArray();
                EditorUtility.SetDirty(pool);
            }
        }

        static void EnsureAchievements(params ChoiceData[] shocks)
        {
            const string root = DataRoot + "/Achievements";
            EnsureFolder(root);
            var items = new System.Collections.Generic.List<AchievementData>();
            for (int i = 0; i < shocks.Length; i++)
            {
                ChoiceData shock = shocks[i];
                if (shock == null)
                    continue;
                AchievementData badge = Achievement(
                    root + "/achievement_" + shock.Id + ".asset",
                    shock.Id,
                    shock.DisplayName,
                    "Paid " + shock.DisplayName + ".",
                    shock.Icon,
                    shock,
                    (i + 1) * 10,
                    false);
                items.Add(badge);
            }

            ArtSet art = AssetDatabase.LoadAssetAtPath<ArtSet>(DataRoot + "/ArtSet.asset");
            items.Add(Achievement(
                root + "/achievement_buffer_kid.asset",
                "buffer_kid",
                "Kept $2",
                "Still had $2 after the surprise bill.",
                art != null ? art.BadgeBufferKid : null,
                null,
                100,
                true));

            AchievementCatalog catalog = LoadOrCreate<AchievementCatalog>(DataRoot + "/AchievementCatalog.asset");
            catalog.Items = items.ToArray();
            EditorUtility.SetDirty(catalog);
        }

        static AchievementData Achievement(
            string path,
            string id,
            string displayName,
            string description,
            Sprite icon,
            ChoiceData shock,
            int sortOrder,
            bool bufferKid)
        {
            AchievementData data = LoadOrCreate<AchievementData>(path);
            if (string.IsNullOrEmpty(data.Id))
            {
                data.Id = id;
                data.DisplayName = displayName;
                data.Description = description;
                data.SortOrder = sortOrder;
                data.UnlockOnBufferKid = bufferKid;
                data.ChoiceId = shock != null ? shock.Id : "";
            }

            if (data.Icon == null && icon != null)
                data.Icon = icon;
            if (data.ShockChoice == null && shock != null)
                data.ShockChoice = shock;
            EditorUtility.SetDirty(data);
            return data;
        }

        static PoolSlotRule Slot(
            string label,
            bool useCost,
            float minCost,
            float maxCost,
            bool lateRisk,
            bool faceHit,
            bool useLeak,
            LeakTag leak,
            bool useOverspend,
            OverspendRule overspend)
        {
            return new PoolSlotRule
            {
                Label = label,
                UseCostRange = useCost,
                MinCost = minCost,
                MaxCost = maxCost,
                RequireLateRisk = lateRisk,
                RequireNegativeFace = faceHit,
                UseLeak = useLeak,
                Leak = leak,
                UseOverspend = useOverspend,
                Overspend = overspend
            };
        }

        static PromoCatalog EnsurePromos()
        {
            PromoData ride = Promo(
                "promo_ride",
                "20% off",
                "App pinged. Twenty percent off today.",
                "Car's cheaper if I tap the code.",
                "ride",
                0.30f,
                PromoCut.Percent,
                0.20f,
                4f);
            PromoData fastFood = Promo(
                "promo_fastfood",
                "$1.50 off",
                "App said $1.50 off today.",
                "There's a deal if we share.",
                "fastfood",
                0.26f,
                PromoCut.Amount,
                1.50f,
                1f);
            PromoData drinks = Promo(
                "promo_bubbletea",
                "$1 off",
                "Member price today.",
                "One dollar off if I show the app.",
                "bubbletea",
                0.28f,
                PromoCut.Amount,
                1f,
                1f);
            PromoData cafe = Promo(
                "promo_cafe",
                "15% off",
                "Breakfast set is on promo.",
                "Set is cheaper this morning.",
                "cafe",
                0.18f,
                PromoCut.Percent,
                0.15f,
                2f);
            PromoData fun = Promo(
                "promo_fun",
                "$2 off",
                "Student price today.",
                "They've got a student cut.",
                "fun",
                0.16f,
                PromoCut.Amount,
                2f,
                1f);

            PromoCatalog catalog = LoadOrCreate<PromoCatalog>(PromoCatalogPath);
            if (catalog.Items == null || catalog.Items.Length == 0)
                catalog.Items = new[] { ride, fastFood, drinks, cafe, fun };
            if (catalog.MaxActive <= 0)
                catalog.MaxActive = 2;
            if (catalog.BoostWeight <= 0)
                catalog.BoostWeight = 3;
            if (string.IsNullOrEmpty(catalog.Badge))
                catalog.Badge = "PROMO";
            if (string.IsNullOrEmpty(catalog.ExpenseFormat))
                catalog.ExpenseFormat = "{0} · {1}";
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static PromoData Promo(
            string id,
            string displayName,
            string banner,
            string prompt,
            string group,
            float chance,
            PromoCut cut,
            float cutValue,
            float minPaid)
        {
            PromoData promo = LoadOrCreate<PromoData>(PromoRoot + "/" + id + ".asset");
            if (string.IsNullOrEmpty(promo.Id))
            {
                promo.Id = id;
                promo.DisplayName = displayName;
                promo.BannerLine = banner;
                promo.PromptLine = prompt;
                promo.Group = group;
                promo.Chance = chance;
                promo.Cut = cut;
                promo.CutValue = cutValue;
                promo.MinPaid = minPaid;
            }

            if (string.IsNullOrEmpty(promo.Group) && !string.IsNullOrEmpty(group))
                promo.Group = group;
            EditorUtility.SetDirty(promo);
            return promo;
        }

        static ChoiceData Choice(
            string id,
            string displayName,
            string prompt,
            float cost,
            float fuel,
            float face,
            float lateRisk,
            LeakTag leak,
            OverspendRule overspend,
            string iconFile,
            string backdropFile = null,
            string promoGroup = null)
        {
            ChoiceData choice = LoadOrCreate<ChoiceData>(ChoiceRoot + "/" + id + ".asset");
            if (string.IsNullOrEmpty(choice.Id))
            {
                choice.Id = id;
                choice.DisplayName = displayName;
                choice.PromptLine = prompt;
                choice.Cost = cost;
                choice.LeakTag = leak;
                choice.Overspend = overspend;
                choice.PromoGroup = promoGroup;
                choice.Delta = new StatDelta
                {
                    PocketCost = cost,
                    FuelChange = fuel,
                    FaceChange = face,
                    BufferChange = 0f,
                    LateRisk = lateRisk,
                    MarksLeak = leak
                };
            }

            if (string.IsNullOrEmpty(choice.PromoGroup) && !string.IsNullOrEmpty(promoGroup))
                choice.PromoGroup = promoGroup;
            if (choice.Icon == null && !string.IsNullOrEmpty(iconFile))
                choice.Icon = SpriteAt(ArtIcons + "/" + iconFile);
            if (choice.Backdrop == null && !string.IsNullOrEmpty(backdropFile))
                choice.Backdrop = SpriteAt(ArtBackgrounds + "/" + backdropFile);
            EditorUtility.SetDirty(choice);
            return choice;
        }

        static BeatData Beat(
            string id,
            string title,
            string body,
            SpeakerKind speaker,
            BeatPhase phase,
            string backgroundFile,
            params ChoiceData[] choices)
        {
            BeatData beat = LoadOrCreate<BeatData>(BeatRoot + "/" + id + ".asset");
            if (string.IsNullOrEmpty(beat.Id))
            {
                beat.Id = id;
                beat.Title = title;
                beat.BodyLine = body;
                beat.Speaker = speaker;
                beat.Phase = phase;
                beat.Choices = choices;
            }

            if (beat.Background == null && !string.IsNullOrEmpty(backgroundFile))
                beat.Background = SpriteAt(ArtBackgrounds + "/" + backgroundFile);
            EditorUtility.SetDirty(beat);
            return beat;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            EnsureFolder(Path.GetDirectoryName(path));
            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static Sprite SpriteAt(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
