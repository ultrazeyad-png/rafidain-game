using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Menu: Rafidain > Build Zakho Region (Big Map)
    /// Builds a 1 km x 1 km open region of Zakho:
    ///  - real terrain: mountain ring, hills, valley, the Khabur river
    ///  - a winding road from the southern gate to the guardian's arena in the north
    ///  - Zakho old town with a clear street grid, flat-roofed houses and a minaret plaza
    ///  - a stone arch bridge over the river (Delal bridge style)
    ///  - oak forest on the slopes, rocks, sky, fog, cinematic post-processing
    /// Textures: drop real textures (e.g. from polyhaven.com) into
    /// Assets/Rafidain/ImportedTextures/Grass | Dirt | Rock | Road | Mud and rebuild — they are used automatically.
    /// </summary>
    public static class RafidainRegionBuilder
    {
        const string Gen = "Assets/Rafidain/Generated/Zakho";
        const string ImportRoot = "Assets/Rafidain/ImportedTextures";
        // ---------- regions (governorates) ----------
        class RegionDef
        {
            public string id, name, sceneName, terrainName, townName;
            public int seed;
            public float noiseX, noiseZ, roadPhase, mountains = 1f;
            public bool river, lake;
            public Vector2 lakeCenter;
            public float lakeRadius;
            public string[] flameNames;     // gate, town, river/lake, ascent
            public string riverFlameId;
            public string bossName, bossId;
            public string nextScene, nextLabel, prevScene, prevLabel;
            public bool pieceAtLake;
            public string[] riddleTown, riddleWild;
            public string sageTown, sageWild;
            public int tier;                 // difficulty step along the road to Baghdad
            public string landmark = "";     // nineveh | citadel | fire | malwiya | underworld
            public float townRaise;          // Erbil: the town sits on its citadel mound
            public bool finalRegion;
            public Mood mood;
            public string ScenePath => $"Assets/Rafidain/Scenes/{sceneName}.unity";
        }

        // Light and air of each governorate
        class Mood
        {
            public float sunPitch = 10f, sunYaw = -35f, sunIntensity = 1.75f;
            public Color sun = new Color(1f, 0.64f, 0.40f);
            public Color skyTint = new Color(0.46f, 0.43f, 0.50f);
            public float atmosphere = 1.55f;
            public Color fog = new Color(0.53f, 0.47f, 0.45f);
            public float fogDensity = 0.0042f;
            public Color ambientSky = new Color(0.50f, 0.50f, 0.58f);
            public Color ambientEquator = new Color(0.52f, 0.42f, 0.35f);
            public Color filter = Color.white;
        }

        static readonly Mood MoodDuhok = new Mood { sunPitch = 16f, sun = new Color(1f, 0.85f, 0.72f), skyTint = new Color(0.42f, 0.5f, 0.62f), atmosphere = 1.1f,
            fog = new Color(0.55f, 0.6f, 0.66f), fogDensity = 0.0038f, ambientSky = new Color(0.5f, 0.56f, 0.66f), ambientEquator = new Color(0.45f, 0.46f, 0.48f), filter = new Color(0.93f, 0.97f, 1.05f) };
        static readonly Mood MoodMosul = new Mood { sunPitch = 34f, sunIntensity = 1.9f, sun = new Color(1f, 0.92f, 0.8f), skyTint = new Color(0.5f, 0.5f, 0.55f), atmosphere = 1.0f,
            fog = new Color(0.7f, 0.66f, 0.6f), fogDensity = 0.0032f, ambientSky = new Color(0.58f, 0.58f, 0.62f), ambientEquator = new Color(0.56f, 0.5f, 0.42f), filter = new Color(1.02f, 1f, 0.95f) };
        static readonly Mood MoodErbil = new Mood { sunPitch = 22f, sun = new Color(1f, 0.75f, 0.5f), skyTint = new Color(0.5f, 0.45f, 0.45f), atmosphere = 1.35f,
            fog = new Color(0.66f, 0.58f, 0.48f), fogDensity = 0.0036f, ambientEquator = new Color(0.58f, 0.46f, 0.34f), filter = new Color(1.05f, 0.99f, 0.9f) };
        static readonly Mood MoodKirkuk = new Mood { sunPitch = 12f, sun = new Color(1f, 0.5f, 0.25f), skyTint = new Color(0.55f, 0.38f, 0.3f), atmosphere = 2.2f,
            fog = new Color(0.55f, 0.38f, 0.25f), fogDensity = 0.006f, ambientSky = new Color(0.5f, 0.4f, 0.36f), ambientEquator = new Color(0.6f, 0.38f, 0.22f), filter = new Color(1.1f, 0.94f, 0.82f) };
        static readonly Mood MoodSamarra = new Mood { sunPitch = 7f, sun = new Color(1f, 0.6f, 0.35f), skyTint = new Color(0.52f, 0.44f, 0.4f), atmosphere = 1.9f,
            fog = new Color(0.62f, 0.52f, 0.4f), fogDensity = 0.005f, ambientEquator = new Color(0.58f, 0.45f, 0.32f), filter = new Color(1.06f, 0.98f, 0.86f) };
        static readonly Mood MoodBaghdad = new Mood { sunPitch = 4f, sunIntensity = 1.5f, sun = new Color(0.95f, 0.5f, 0.45f), skyTint = new Color(0.4f, 0.3f, 0.5f), atmosphere = 2.0f,
            fog = new Color(0.38f, 0.3f, 0.42f), fogDensity = 0.005f, ambientSky = new Color(0.42f, 0.36f, 0.55f), ambientEquator = new Color(0.48f, 0.34f, 0.4f), filter = new Color(0.98f, 0.9f, 1.06f) };

        static readonly RegionDef Zakho = new RegionDef
        {
            id = "zakho", name = "زاخو", sceneName = "Zakho_Region", terrainName = "Zakho Terrain", townName = "Zakho Old Town",
            seed = 1985, noiseX = 0f, noiseZ = 0f, roadPhase = 0f, mountains = 1f,
            river = true, lake = false,
            flameNames = new[] { "زاخو - البوابة الجنوبية", "زاخو - المدينة القديمة", "جسر الخابور", "صعود الحارس" },
            bossName = "حارس زاخو", bossId = "boss_zakho",
            nextScene = "Duhok_Region", nextLabel = "الطريق الى دهوك",
            sageTown = "شيخ عند البوابة",
            riddleTown = new[]
            {
                "المدينة القديمة لا تنام...\nلكن ازقتها البعيدة نسيت اسماءها",
                "ابتعد عن الطريق الكبير\nحيث يتقاطع زقاقان ولا يمر احد",
                "هناك ينتظر نصف الطريق\nلوح من طين ما زال دافئا",
            },
            sageWild = "امرأة عند المنارة",
            riddleWild = new[]
            {
                "الشرق يحرس سره بالصخور",
                "اصعد حيث يشرب البلوط من الحجر\nوتنام الريح فوق الوادي",
                "لا تتبع الطريق...\nفالطرق لا تصل الى ما يستحق",
            },
        };

        static readonly RegionDef Duhok = new RegionDef
        {
            mood = MoodDuhok,
            id = "duhok", name = "دهوك", sceneName = "Duhok_Region", terrainName = "Duhok Terrain", townName = "Duhok Town",
            seed = 2047, noiseX = 3100f, noiseZ = 1700f, roadPhase = 1.9f, mountains = 1.15f,
            river = false, lake = true, lakeCenter = new Vector2(250f, 620f), lakeRadius = 115f,
            flameNames = new[] { "دهوك - مدخل الوادي", "دهوك - السوق القديم", "ضفاف السد", "قلعة الجبل" },
            bossName = "حارس دهوك", bossId = "boss_duhok",
            prevScene = "Zakho_Region", prevLabel = "العودة الى زاخو",
            nextScene = "Mosul_Region", nextLabel = "الطريق الى الموصل",
            tier = 1,
            pieceAtLake = true,
            sageTown = "راع عند المدخل",
            riddleTown = new[]
            {
                "في السوق القديم يبيعون كل شيء\nالا الطريق",
                "اذهب الى حيث تنتهي الفوانيس\nعند زاوية لا يمر بها التجار",
                "هناك لوح من طين\nيعرف ما لا نعرفه",
            },
            sageWild = "صياد عند السوق",
            riddleWild = new[]
            {
                "الماء الذي حبسه الناس لا ينسى",
                "امش على ضفة البحيرة\nحيث تنظر الجبال الى وجهها",
                "بين الحجارة المبللة...\nينتظرك نصف الخريطة",
            },
        };

        static readonly RegionDef Mosul = new RegionDef
        {
            mood = MoodMosul,
            id = "mosul", name = "الموصل", sceneName = "Mosul_Region", terrainName = "Nineveh Terrain", townName = "Old Mosul",
            seed = 3121, noiseX = 5200f, noiseZ = 900f, roadPhase = 3.1f, mountains = 0.55f,
            river = true, tier = 2, landmark = "nineveh",
            flameNames = new[] { "الموصل - باب سنجار", "الموصل القديمة", "جسر دجلة", "اطلال نينوى" },
            bossName = "حارس نينوى", bossId = "boss_mosul",
            prevScene = "Duhok_Region", prevLabel = "العودة الى دهوك",
            nextScene = "Erbil_Region", nextLabel = "الطريق الى اربيل",
            sageTown = "حكواتي الموصل",
            riddleTown = new[] { "الازقة تتذكر من مر بها", "حيث يضيع الطريق الكبير\nويبقى زقاق لا اسم له", "هناك الطين الذي يعرف الطريق" },
            sageWild = "حارس الاطلال",
            riddleWild = new[] { "الثيران المجنحة ما زالت تحرس الشرق", "اترك النهر خلفك واصعد\nحيث الحجر اقدم من الملوك", "تحت السماء العالية... نصف الخريطة" },
        };

        static readonly RegionDef Erbil = new RegionDef
        {
            mood = MoodErbil,
            id = "erbil", name = "اربيل", sceneName = "Erbil_Region", terrainName = "Erbil Terrain", townName = "Erbil Citadel",
            seed = 4409, noiseX = 800f, noiseZ = 6100f, roadPhase = 4.4f, mountains = 0.7f,
            river = false, tier = 3, landmark = "citadel", townRaise = 16f,
            flameNames = new[] { "اربيل - السهل", "قلعة اربيل", "سوق القيصرية", "تل الحارس" },
            bossName = "حارس القلعة", bossId = "boss_erbil",
            prevScene = "Mosul_Region", prevLabel = "العودة الى الموصل",
            nextScene = "Kirkuk_Region", nextLabel = "الطريق الى كركوك",
            sageTown = "بائع في القيصرية",
            riddleTown = new[] { "القلعة لم تسقط منذ سبعة الاف عام", "لكن في ركنها البعيد\nحجر نسيه الحراس", "اصعد التل... وابحث حيث لا يصل الضوء" },
            sageWild = "راعي السهل",
            riddleWild = new[] { "السهل واسع ولا يخفي شيئا", "الا ما خبأته الريح شرقا\nبين صخور لا يسكنها احد", "هناك... حيث تنتهي الحقول" },
        };

        static readonly RegionDef Kirkuk = new RegionDef
        {
            mood = MoodKirkuk,
            id = "kirkuk", name = "كركوك", sceneName = "Kirkuk_Region", terrainName = "Kirkuk Terrain", townName = "Kirkuk Town",
            seed = 5077, noiseX = 7300f, noiseZ = 4400f, roadPhase = 5.2f, mountains = 0.6f,
            river = false, tier = 4, landmark = "fire",
            flameNames = new[] { "كركوك - الطريق", "كركوك القديمة", "بابا كركر", "سفح النار" },
            bossName = "حارس النار الازلية", bossId = "boss_kirkuk",
            prevScene = "Erbil_Region", prevLabel = "العودة الى اربيل",
            nextScene = "Samarra_Region", nextLabel = "الطريق الى سامراء",
            sageTown = "عجوز عند الموقد",
            riddleTown = new[] { "النار هنا لا تنطفئ منذ زمن الانبياء", "لكن البرد يسكن الازقة البعيدة", "ابحث هناك... بعيدا عن الدخان" },
            sageWild = "حداد كركوك",
            riddleWild = new[] { "الارض تتنفس نارا", "امش بين اللهب ولا تقف", "عند اطراف الحقل المحترق\nينتظر لوح من طين" },
        };

        static readonly RegionDef Samarra = new RegionDef
        {
            mood = MoodSamarra,
            id = "samarra", name = "سامراء", sceneName = "Samarra_Region", terrainName = "Samarra Terrain", townName = "Samarra",
            seed = 6151, noiseX = 2600f, noiseZ = 8800f, roadPhase = 0.7f, mountains = 0.45f,
            river = true, tier = 5, landmark = "malwiya",
            flameNames = new[] { "سامراء - البوابة", "سامراء القديمة", "ضفة دجلة", "ظل الملوية" },
            bossName = "حارس الملوية", bossId = "boss_samarra",
            prevScene = "Kirkuk_Region", prevLabel = "العودة الى كركوك",
            nextScene = "Baghdad_Region", nextLabel = "الطريق الى بغداد",
            sageTown = "حارس المئذنة",
            riddleTown = new[] { "الملوية تدور حول نفسها\nكما يدور الحائر حول سؤاله", "اصعدها... وانظر الى المدينة", "الزقاق الذي تراه وحيدا\nهو الذي تبحث عنه" },
            sageWild = "صياد على دجلة",
            riddleWild = new[] { "دجلة تعرف اسمك يا فرات", "اعبر الجسر ولا تلتفت", "في البرية بعد الماء\nنصف الطريق الى بغداد" },
        };

        static readonly RegionDef Baghdad = new RegionDef
        {
            mood = MoodBaghdad,
            id = "baghdad", name = "بغداد", sceneName = "Baghdad_Region", terrainName = "Baghdad Terrain", townName = "Old Baghdad",
            seed = 7777, noiseX = 9100f, noiseZ = 2300f, roadPhase = 2.4f, mountains = 0.4f,
            river = true, tier = 6, landmark = "underworld", finalRegion = true,
            flameNames = new[] { "بغداد - باب المعظم", "بغداد القديمة", "جسر الشهداء", "باب العالم السفلي" },
            bossName = "ادابا", bossId = "boss_adapa",
            prevScene = "Samarra_Region", prevLabel = "العودة الى سامراء",
            sageTown = "شاعر في شارع المتنبي",
            riddleTown = new[] { "بغداد تحفظ كل الحكايات", "الا حكاية دجلة... كتبتها انت", "ابحث في الزقاق الذي لا يقرأه احد" },
            sageWild = "ملاح على دجلة",
            riddleWild = new[] { "تحت المدينة باب\nوالباب يعرف خمسة مفاتيح", "اعبر النهر شرقا", "حيث ينتهي العشب\nيبدا الطريق الاخير" },
        };

        static readonly RegionDef[] AllRegions = { Zakho, Duhok, Mosul, Erbil, Kirkuk, Samarra, Baghdad };

        static RegionDef R = Zakho;
        static string ScenePath => R.ScenePath;

        const float Size = 1024f;
        const float MaxH = 260f;
        const float WaterLevel = 14f;
        const int HeightRes = 513;
        const int AlphaRes = 512;

        const float TownRadius = 150f;
        const float BlockSize = 40f;
        const float StreetWidth = 9f;

        static readonly string[] LayerNames = { "Grass", "Dirt", "Rock", "Road", "Mud" };

        static float RoadX(float z) => 512f + 70f * Mathf.Sin(z * 0.006f + R.roadPhase) + 25f * Mathf.Sin(z * 0.017f + R.roadPhase * 2f);
        static float RoadTarget(float z) => 22f + 5f * Mathf.Sin(z * 0.004f);
        static float RiverZ(float x) => R.river ? 620f + 35f * Mathf.Sin(x * 0.009f) + 12f * Mathf.Sin(x * 0.031f) : -5000f;

        static Vector2 TownCenter => new Vector2(RoadX(330f), 330f);
        static float TownHeight => RoadTarget(330f) + R.townRaise;
        static Vector2 ArenaCenter => new Vector2(RoadX(905f), 905f);
        static float ArenaHeight => RoadTarget(905f) + 4f;

        [MenuItem("Rafidain/Build Zakho Region (Big Map)")]
        public static void Build() => BuildRegion(Zakho, firstScene: true);

        [MenuItem("Rafidain/Build Duhok Region (Governorate 2)")]
        public static void BuildDuhok() => BuildRegion(Duhok, firstScene: false);

        [MenuItem("Rafidain/Governorates/3 Mosul (Nineveh)")]
        public static void BuildMosul() => BuildRegion(Mosul, false);
        [MenuItem("Rafidain/Governorates/4 Erbil (Citadel)")]
        public static void BuildErbil() => BuildRegion(Erbil, false);
        [MenuItem("Rafidain/Governorates/5 Kirkuk (Eternal Fire)")]
        public static void BuildKirkuk() => BuildRegion(Kirkuk, false);
        [MenuItem("Rafidain/Governorates/6 Samarra (Malwiya)")]
        public static void BuildSamarra() => BuildRegion(Samarra, false);
        [MenuItem("Rafidain/Governorates/7 Baghdad (Underworld Gate)")]
        public static void BuildBaghdad() => BuildRegion(Baghdad, false);

        [MenuItem("Rafidain/Build ALL Governorates (Zakho to Baghdad)")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            quiet = true;
            try
            {
                // Zakho last so it ends up open and first in the build list
                for (int i = AllRegions.Length - 1; i >= 0; i--) BuildRegion(AllRegions[i], AllRegions[i] == Zakho);
            }
            finally { quiet = false; }
            BuildTitleScene(openAfter: false);
            EditorSceneManager.OpenScene(Zakho.ScenePath);
            EditorUtility.DisplayDialog("Rafidain", "All 7 governorates and the title screen are built, from Zakho to Baghdad.\n\nZakho is open: press Play (or open MainMenu to start from the title screen).", "OK");
        }

        [MenuItem("Rafidain/Build Title Screen")]
        public static void BuildTitleMenu() => BuildTitleScene(openAfter: true);

        static void BuildTitleScene(bool openAfter)
        {
            if (openAfter && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RafidainSceneBuilder.EnsureFolder("Assets/Rafidain/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.015f, 0.012f);
            cam.transform.position = new Vector3(0f, 1.2f, -5f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null) sun.intensity = 0.05f;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.025f, 0.02f);

            // a lone flame with a planted sword
            var stone = RafidainSceneBuilder.Mat("AncientStone", new Color(0.62f, 0.55f, 0.44f), 0.25f);
            var ember = RafidainSceneBuilder.Mat("Ember", new Color(1f, 0.45f, 0.1f), 0.5f);
            RafidainSceneBuilder.MakeBonfire("title_flame", "", new Vector3(0f, 0f, 0f), ember, stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Plane, "Ground", null, Vector3.zero, Vector3.one * 3f, stone);
            RafidainSceneBuilder.CreatePostVolume();

            var go = new GameObject("_Title");
            go.AddComponent<TitleScreen>();
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var am = new GameObject("_Audio");
            RafidainSceneBuilder.AddAudioManager(am);

            string path = "Assets/Rafidain/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
            RafidainSceneBuilder.AddSceneToBuild(path, true);
            if (!openAfter) return;
            EditorUtility.DisplayDialog("Rafidain", "Title screen built and set as the first scene.\n\nPress Play.", "OK");
        }

        static bool quiet;

        static void BuildRegion(RegionDef def, bool firstScene)
        {
            R = def;
            if (!quiet && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RafidainURPSetup.EnsureURP();
            RafidainURPSetup.TuneForOpenWorld();
            RafidainSceneBuilder.EnsureFolder(Gen);
            RafidainSceneBuilder.EnsureFolder("Assets/Rafidain/Scenes");
            foreach (var n in LayerNames) RafidainSceneBuilder.EnsureFolder($"{ImportRoot}/{n}");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Random.InitState(R.seed);

            try
            {
                EditorUtility.DisplayProgressBar("Rafidain", "Shaping terrain…", 0.1f);
                var terrainData = BuildTerrainData();
                var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
                terrainGo.name = R.terrainName;
                var terrain = terrainGo.GetComponent<Terrain>();
                terrain.heightmapPixelError = 3f;
                terrain.basemapDistance = 500f;
                terrain.drawInstanced = true;
                terrain.detailObjectDistance = 110f;
                terrain.detailObjectDensity = 1f;
                var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (terrainShader != null)
                {
                    var tm = new Material(terrainShader);
                    AssetDatabase.CreateAsset(tm, $"{Gen}/TerrainMat_{R.id}.mat");
                    terrain.materialTemplate = tm;
                }

                EditorUtility.DisplayProgressBar("Rafidain", "Painting ground (grass, rock, roads)…", 0.35f);
                PaintTerrain(terrainData);

                EditorUtility.DisplayProgressBar("Rafidain", "Sky, sun and fog…", 0.5f);
                var sun = SetupAtmosphere();

                // ---------- Materials ----------
                var stone = RafidainSceneBuilder.Mat("AncientStone", new Color(0.62f, 0.55f, 0.44f), 0.25f);
                var rockMat = RafidainSceneBuilder.Mat("Rock", new Color(0.42f, 0.40f, 0.37f), 0.22f);
                var mud1 = RafidainSceneBuilder.Mat("MudBrick_A", new Color(0.66f, 0.55f, 0.42f), 0.1f);
                var mud2 = RafidainSceneBuilder.Mat("MudBrick_B", new Color(0.58f, 0.50f, 0.40f), 0.1f);
                var limestone = RafidainSceneBuilder.Mat("Limestone", new Color(0.74f, 0.70f, 0.62f), 0.15f);
                var roofMat = RafidainSceneBuilder.Mat("RoofParapet", new Color(0.45f, 0.38f, 0.30f), 0.1f);
                var bark = RafidainSceneBuilder.Mat("OakBark", new Color(0.28f, 0.21f, 0.15f), 0.05f);
                var leaves = RafidainSceneBuilder.Mat("OakLeaves", new Color(0.22f, 0.30f, 0.14f), 0.05f);
                var water = RafidainSceneBuilder.Mat("RiverWater", new Color(0.10f, 0.20f, 0.20f), 0.96f);
                var furatMat = RafidainSceneBuilder.Mat("Furat", new Color(0.22f, 0.20f, 0.19f), 0.35f);
                var hollowMat = RafidainSceneBuilder.Mat("Hollow", new Color(0.33f, 0.29f, 0.26f), 0.1f);
                var guardianMat = RafidainSceneBuilder.Mat("Guardian", new Color(0.32f, 0.24f, 0.14f), 0.55f, 0.6f);
                var steel = RafidainSceneBuilder.Mat("Steel", new Color(0.72f, 0.72f, 0.74f), 0.85f, 1f);
                var wood = RafidainSceneBuilder.Mat("DarkWood", new Color(0.22f, 0.14f, 0.08f), 0.2f);
                var windowMat = RafidainSceneBuilder.Mat("WindowDark", new Color(0.05f, 0.045f, 0.04f), 0.6f);
                var tile = RafidainSceneBuilder.Mat("TurquoiseTile", new Color(0.08f, 0.42f, 0.48f), 0.75f);
                var palmBark = RafidainSceneBuilder.Mat("PalmBark", new Color(0.36f, 0.27f, 0.18f), 0.05f);
                var palmLeaf = RafidainSceneBuilder.Mat("PalmLeaf", new Color(0.25f, 0.33f, 0.14f), 0.1f);
                var glow = RafidainSceneBuilder.Mat("LanternGlow", new Color(1f, 0.7f, 0.35f), 0.3f);
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.25f) * 4f);
                var ember = RafidainSceneBuilder.Mat("Ember", new Color(1f, 0.45f, 0.1f), 0.5f);
                ember.EnableKeyword("_EMISSION");
                ember.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 5f);

                // ---------- Water ----------
                var waterGo = RafidainSceneBuilder.Prim(PrimitiveType.Plane, "Khabur River", null,
                    new Vector3(Size * 0.5f, WaterLevel, Size * 0.5f), new Vector3(Size / 10f, 1f, Size / 10f), water, keepCollider: false);
                waterGo.isStatic = false;
                SetupWaterMaterial(water);
                waterGo.AddComponent<WaterFlow>();

                EditorUtility.DisplayProgressBar("Rafidain", "Building Zakho old town…", 0.6f);
                var world = new GameObject("World").transform;
                BuildTown(world, terrainData, mud1, mud2, limestone, roofMat, stone, wood, tile, glow, windowMat);
                if (R.river) BuildBridge(world, stone);
                BuildArena(world, stone, glow);
                BuildLandmark(world, terrainData, stone, mud1, limestone, glow, ember);

                EditorUtility.DisplayProgressBar("Rafidain", "Planting oak forest and rocks…", 0.75f);
                ScatterNature(world, terrainData, bark, leaves, rockMat);
                if (R.river) ScatterPalms(world, terrainData, palmBark, palmLeaf);
                ScatterBushes(world, terrainData, leaves);
                AddLife(world, terrainData);
                AddAtmosphereFX(world, terrainData);

                EditorUtility.DisplayProgressBar("Rafidain", "Placing Furat, enemies and flames…", 0.9f);
                PlaceGameplay(terrainData, sun, furatMat, hollowMat, guardianMat, steel, ember, stone);

                RafidainSceneBuilder.CreatePostVolume();
                CreateMoodVolume();
                PlaceGates(terrainData, stone);
                PlaceStory(terrainData, stone);

                EditorUtility.DisplayProgressBar("Rafidain", "Optimising (LODs, shadows, batching)…", 0.97f);
                Optimize(world.gameObject);
                var sysGo = GameObject.Find("_Game");
                if (sysGo != null && sysGo.GetComponent<PerformanceTuner>() == null) sysGo.AddComponent<PerformanceTuner>();

                EditorSceneManager.SaveScene(scene, ScenePath);
                RafidainSceneBuilder.AddSceneToBuild(ScenePath, firstScene);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (quiet) return;
            EditorUtility.DisplayDialog("Rafidain",
                R.sceneName + " is ready (1 km x 1 km).\n\nStart at the southern gate, follow the road north through the town and climb to the Guardian's arena. Beyond the arena, a fog gate leads to the next governorate.\n\nPress Play.",
                "OK");
        }

        // =====================================================================
        // Terrain
        // =====================================================================

        static TerrainData BuildTerrainData()
        {
            var td = new TerrainData { heightmapResolution = HeightRes };
            td.size = new Vector3(Size, MaxH, Size);
            td.alphamapResolution = AlphaRes;
            td.baseMapResolution = 1024;

            var h = new float[HeightRes, HeightRes];
            for (int y = 0; y < HeightRes; y++)
            {
                float z = y / (float)(HeightRes - 1) * Size;
                for (int x = 0; x < HeightRes; x++)
                {
                    float wx = x / (float)(HeightRes - 1) * Size;
                    h[y, x] = HeightAt(wx, z) / MaxH;
                }
            }
            td.SetHeights(0, 0, h);
            AssetDatabase.CreateAsset(td, $"{Gen}/{R.id}_Terrain.asset");
            return td;
        }

        static float HeightAt(float x, float z)
        {
            float nx = x + R.noiseX, nz = z + R.noiseZ;
            float baseH = 20f + (Fbm(nx, nz, 4, 0.004f) - 0.5f) * 18f;

            float dEdge = Mathf.Min(Mathf.Min(x, Size - x), Mathf.Min(z, Size - z));
            float m = Smooth(230f, 10f, dEdge);
            float mountains = m * (90f + Ridged(nx, nz, 5, 0.006f) * 150f) * R.mountains;

            float hills = Mathf.Max(0f, Fbm(nx + 500f, nz + 500f, 3, 0.008f) - 0.55f) * 120f;
            float h = baseH + mountains + hills;

            // dam lake (Duhok)
            if (R.lake)
            {
                float dLake = Vector2.Distance(new Vector2(x, z), R.lakeCenter);
                h = Mathf.Lerp(h, WaterLevel - 6f, Smooth(R.lakeRadius + 35f, R.lakeRadius, dLake));
            }

            // Khabur river channel
            float dRiver = Mathf.Abs(z - RiverZ(x));
            h = Mathf.Lerp(h, WaterLevel - 4f, Smooth(34f, 10f, dRiver));

            // Road (not across the river: the bridge does that)
            float dRoad = Mathf.Abs(x - RoadX(z));
            if (z > 25f && z < 970f)
            {
                float t = Smooth(18f, 7f, dRoad) * (1f - Smooth(42f, 22f, dRiver));
                h = Mathf.Lerp(h, RoadTarget(z), t);
            }

            // Old town: flat ground
            float dTown = Vector2.Distance(new Vector2(x, z), TownCenter);
            h = Mathf.Lerp(h, TownHeight, Smooth(TownRadius + 45f, TownRadius, dTown));

            // Guardian's arena plateau
            float dArena = Vector2.Distance(new Vector2(x, z), ArenaCenter);
            h = Mathf.Lerp(h, ArenaHeight, Smooth(72f, 48f, dArena));

            return Mathf.Clamp(h, 0f, MaxH - 1f);
        }

        static void PaintTerrain(TerrainData td)
        {
            var layers = new TerrainLayer[LayerNames.Length];
            Color[,] palette =
            {
                { new Color(0.30f, 0.34f, 0.17f), new Color(0.42f, 0.42f, 0.22f) }, // grass
                { new Color(0.40f, 0.33f, 0.24f), new Color(0.52f, 0.44f, 0.33f) }, // dirt
                { new Color(0.38f, 0.36f, 0.33f), new Color(0.55f, 0.52f, 0.48f) }, // rock
                { new Color(0.47f, 0.43f, 0.37f), new Color(0.60f, 0.56f, 0.49f) }, // road / cobbles
                { new Color(0.24f, 0.21f, 0.17f), new Color(0.33f, 0.29f, 0.23f) }, // mud
            };
            float[] tile = { 6f, 5f, 7f, 2.6f, 5f };

            for (int i = 0; i < layers.Length; i++)
            {
                FindImported(LayerNames[i], out var diffuse, out var normal);
                if (diffuse == null)
                {
                    int kind = i == 2 ? 1 : i == 3 ? 2 : 0;
                    diffuse = ProceduralTexture(LayerNames[i], palette[i, 0], palette[i, 1], i == 3 ? 0.08f : 0.03f, i == 2 ? 0.18f : 0.1f, kind, out var procNormal);
                    if (normal == null) normal = procNormal;
                }

                // update the shared layer in place so terrains built earlier keep their link
                string path = $"{Gen}/Layer_{LayerNames[i]}.terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                bool isNew = layer == null;
                if (isNew) layer = new TerrainLayer();
                layer.diffuseTexture = diffuse;
                layer.normalMapTexture = normal;
                layer.tileSize = new Vector2(tile[i], tile[i]);
                layer.smoothness = i == 4 ? 0.35f : 0.05f;
                if (isNew) AssetDatabase.CreateAsset(layer, path);
                else EditorUtility.SetDirty(layer);
                layers[i] = layer;
            }
            td.terrainLayers = layers;

            var maps = new float[AlphaRes, AlphaRes, layers.Length];
            Vector2 townOrigin = TownCenter - new Vector2(TownRadius, TownRadius);

            for (int ay = 0; ay < AlphaRes; ay++)
            {
                float v = (ay + 0.5f) / AlphaRes;
                float z = v * Size;
                for (int ax = 0; ax < AlphaRes; ax++)
                {
                    float u = (ax + 0.5f) / AlphaRes;
                    float x = u * Size;
                    float height = td.GetInterpolatedHeight(u, v);
                    float steep = td.GetSteepness(u, v);

                    float dRiver = Mathf.Abs(z - RiverZ(x));
                    float road = (z > 25f && z < 970f) ? Smooth(10f, 5f, Mathf.Abs(x - RoadX(z))) * (1f - Smooth(40f, 24f, dRiver)) : 0f;

                    float dTown = Vector2.Distance(new Vector2(x, z), TownCenter);
                    if (dTown < TownRadius)
                    {
                        float lx = Mathf.Repeat(x - townOrigin.x, BlockSize);
                        float lz = Mathf.Repeat(z - townOrigin.y, BlockSize);
                        if (lx < StreetWidth || lz < StreetWidth) road = Mathf.Max(road, 1f);
                    }
                    if (Vector2.Distance(new Vector2(x, z), ArenaCenter) < 30f) road = Mathf.Max(road, 0.8f);

                    float mud = Smooth(30f, 14f, dRiver);
                    float rock = Mathf.Clamp01(Smooth(22f, 38f, steep) + Smooth(70f, 115f, height) * 0.8f);
                    float dirt = Mathf.Clamp01((Fbm(x, z, 3, 0.02f) - 0.45f) * 2.5f);

                    float rest = 1f - road;
                    float wRock = rock * rest;
                    float wMud = mud * rest * (1f - rock);
                    float remain = Mathf.Max(0f, rest - wRock - wMud);
                    float wDirt = remain * dirt;
                    float wGrass = remain - wDirt;

                    maps[ay, ax, 0] = wGrass;
                    maps[ay, ax, 1] = wDirt;
                    maps[ay, ax, 2] = wRock;
                    maps[ay, ax, 3] = road;
                    maps[ay, ax, 4] = wMud;
                }
            }
            td.SetAlphamaps(0, 0, maps);
            PaintGrass(td, maps);
            EditorUtility.SetDirty(td);
        }

        // Wild grass tufts on grassy ground (terrain details, wave in the wind)
        static void PaintGrass(TerrainData td, float[,,] maps)
        {
            var grassTex = GrassBladeTexture();
            if (grassTex == null) return;

            var proto = new DetailPrototype
            {
                prototypeTexture = grassTex,
                renderMode = DetailRenderMode.GrassBillboard,
                usePrototypeMesh = false,
                minWidth = 0.6f, maxWidth = 1.3f,
                minHeight = 0.35f, maxHeight = 0.9f,
                healthyColor = new Color(0.55f, 0.62f, 0.32f),
                dryColor = new Color(0.72f, 0.66f, 0.40f),
                noiseSpread = 0.3f
            };
            var poppy = new DetailPrototype
            {
                prototypeTexture = FlowerTexture("Poppies", new Color(0.78f, 0.12f, 0.08f), new Color(0.15f, 0.05f, 0.03f)),
                renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.4f, maxWidth = 0.7f, minHeight = 0.35f, maxHeight = 0.6f,
                healthyColor = Color.white, dryColor = new Color(0.9f, 0.8f, 0.7f), noiseSpread = 0.5f
            };
            var chamomile = new DetailPrototype
            {
                prototypeTexture = FlowerTexture("Chamomile", new Color(0.95f, 0.93f, 0.85f), new Color(0.95f, 0.75f, 0.2f)),
                renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.35f, maxWidth = 0.6f, minHeight = 0.25f, maxHeight = 0.45f,
                healthyColor = Color.white, dryColor = new Color(0.9f, 0.9f, 0.8f), noiseSpread = 0.5f
            };
            td.detailPrototypes = new[] { proto, poppy, chamomile };
            const int DetailRes = 1024;
            td.SetDetailResolution(DetailRes, 16);
            td.wavingGrassStrength = 0.35f;
            td.wavingGrassSpeed = 0.4f;
            td.wavingGrassAmount = 0.35f;
            td.wavingGrassTint = new Color(0.75f, 0.75f, 0.6f);

            var layer = new int[DetailRes, DetailRes];
            var rng = new System.Random(7);
            for (int y = 0; y < DetailRes; y++)
            {
                int ay = Mathf.Min(AlphaRes - 1, y * AlphaRes / DetailRes);
                for (int x = 0; x < DetailRes; x++)
                {
                    int ax = Mathf.Min(AlphaRes - 1, x * AlphaRes / DetailRes);
                    float grass = maps[ay, ax, 0] + maps[ay, ax, 1] * 0.3f;
                    if (grass < 0.35f) continue;
                    double r = rng.NextDouble();
                    layer[y, x] = r < grass * 0.85f ? 1 + (int)(grass * 3f) : 0;
                }
            }
            td.SetDetailLayer(0, 0, 0, layer);

            // wild flowers in patches on the grass (poppies and chamomile, like a Kurdish spring)
            var poppies = new int[DetailRes, DetailRes];
            var daisies = new int[DetailRes, DetailRes];
            for (int y = 0; y < DetailRes; y++)
            {
                int ay = Mathf.Min(AlphaRes - 1, y * AlphaRes / DetailRes);
                for (int x = 0; x < DetailRes; x++)
                {
                    int ax = Mathf.Min(AlphaRes - 1, x * AlphaRes / DetailRes);
                    if (maps[ay, ax, 0] < 0.6f) continue;
                    float patchA = Mathf.PerlinNoise(x * 0.02f + 31f, y * 0.02f + 7f);
                    float patchB = Mathf.PerlinNoise(x * 0.025f + 91f, y * 0.025f + 53f);
                    double r = rng.NextDouble();
                    if (patchA > 0.66f && r < 0.5) poppies[y, x] = 1 + (int)((patchA - 0.66f) * 10f);
                    else if (patchB > 0.68f && r < 0.4) daisies[y, x] = 1 + (int)((patchB - 0.68f) * 8f);
                }
            }
            td.SetDetailLayer(0, 0, 1, poppies);
            td.SetDetailLayer(0, 0, 2, daisies);
        }

        static Texture2D GrassBladeTexture()
        {
            string path = $"{Gen}/GrassBlades.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int W = 128, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var clear = new Color(0.4f, 0.45f, 0.25f, 0f);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) tex.SetPixel(x, y, clear);

            var rng = new System.Random(11);
            for (int b = 0; b < 16; b++)
            {
                float baseX = 10 + (float)rng.NextDouble() * (W - 20);
                float height = H * (0.55f + (float)rng.NextDouble() * 0.45f);
                float lean = ((float)rng.NextDouble() - 0.5f) * 30f;
                float width = 2.5f + (float)rng.NextDouble() * 2.5f;
                var col = Color.Lerp(new Color(0.32f, 0.42f, 0.16f), new Color(0.58f, 0.60f, 0.30f), (float)rng.NextDouble());
                for (int y = 0; y < (int)height; y++)
                {
                    float t = y / height;
                    float cx = baseX + lean * t * t;
                    float w = width * (1f - t);
                    for (int x = Mathf.FloorToInt(cx - w); x <= Mathf.CeilToInt(cx + w); x++)
                    {
                        if (x < 0 || x >= W) continue;
                        var c = col * (0.7f + 0.3f * t);
                        c.a = 1f;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void FindImported(string layer, out Texture2D diffuse, out Texture2D normal)
        {
            diffuse = null;
            normal = null;
            string folder = $"{ImportRoot}/{layer}";
            if (!AssetDatabase.IsValidFolder(folder)) return;

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (n.Contains("nor") || n.Contains("normal"))
                {
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp != null && imp.textureType != TextureImporterType.NormalMap)
                    {
                        imp.textureType = TextureImporterType.NormalMap;
                        imp.SaveAndReimport();
                    }
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                else if (n.Contains("diff") || n.Contains("albedo") || n.Contains("color") || n.Contains("col") || n.Contains("basecolor"))
                {
                    diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
        }

        // Procedural ground material: albedo + matching normal map from one tileable height field.
        // kind: 0 soft ground, 1 cracked rock, 2 cobblestones
        static Texture2D ProceduralTexture(string name, Color a, Color b, float freq, float speckle, int kind, out Texture2D normal)
        {
            string path = $"{Gen}/{name}_albedo.png";
            string npath = $"{Gen}/{name}_normal.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            normal = AssetDatabase.LoadAssetAtPath<Texture2D>(npath);
            if (existing != null && normal != null) return existing;

            const int S = 512;
            var h = HeightField(name, S, freq, kind);
            var rng = new System.Random(name.GetHashCode());

            var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
            var ntex = new Texture2D(S, S, TextureFormat.RGB24, false);
            float strength = kind == 0 ? 2.5f : 5f;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float hv = h[x, y];
                    float sp = 1f - speckle * (float)rng.NextDouble();
                    var c = Color.Lerp(a, b, hv) * sp;
                    if (kind == 2 && hv < 0.25f) c *= 0.55f;        // dark grout between stones
                    if (kind == 1 && hv < 0.18f) c *= 0.7f;         // deep cracks
                    tex.SetPixel(x, y, c);

                    float dx = (h[(x + 1) % S, y] - h[(x - 1 + S) % S, y]) * strength;
                    float dy = (h[x, (y + 1) % S] - h[x, (y - 1 + S) % S]) * strength;
                    Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                    ntex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f));
                }
            }
            tex.Apply();
            ntex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            File.WriteAllBytes(npath, ntex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(ntex);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.ImportAsset(npath);
            var imp = AssetImporter.GetAtPath(npath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.SaveAndReimport();
            }
            normal = AssetDatabase.LoadAssetAtPath<Texture2D>(npath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float[,] HeightField(string name, int S, float freq, int kind)
        {
            var h = new float[S, S];
            // Worley points for cobblestones (tileable grid)
            const int Cells = 8;
            var pts = new Vector2[Cells, Cells];
            var rng = new System.Random(name.GetHashCode() ^ 0x5bd1);
            for (int i = 0; i < Cells; i++)
                for (int j = 0; j < Cells; j++)
                    pts[i, j] = new Vector2((i + 0.15f + 0.7f * (float)rng.NextDouble()) * S / Cells, (j + 0.15f + 0.7f * (float)rng.NextDouble()) * S / Cells);

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float n = TexNoise(x, y, freq) * (1 - u) * (1 - v)
                            + TexNoise(x - S, y, freq) * u * (1 - v)
                            + TexNoise(x, y - S, freq) * (1 - u) * v
                            + TexNoise(x - S, y - S, freq) * u * v;

                    if (kind == 1)
                    {
                        float r = TexNoise(x * 1.7f, y * 1.7f, freq * 2.5f) * (1 - u) * (1 - v)
                                + TexNoise((x - S) * 1.7f, y * 1.7f, freq * 2.5f) * u * (1 - v)
                                + TexNoise(x * 1.7f, (y - S) * 1.7f, freq * 2.5f) * (1 - u) * v
                                + TexNoise((x - S) * 1.7f, (y - S) * 1.7f, freq * 2.5f) * u * v;
                        float crack = Mathf.Abs(r - 0.5f) * 2f; // 0 on the crack line
                        n = Mathf.Lerp(n, n * 0.3f, 1f - Mathf.Clamp01(crack * 6f));
                    }
                    else if (kind == 2)
                    {
                        int ci = x * Cells / S, cj = y * Cells / S;
                        float f1 = float.MaxValue, f2 = float.MaxValue;
                        for (int di = -1; di <= 1; di++)
                        {
                            for (int dj = -1; dj <= 1; dj++)
                            {
                                int ii = (ci + di + Cells) % Cells, jj = (cj + dj + Cells) % Cells;
                                // unwrap the neighbour point position across the tile edge
                                Vector2 p;
                                p.x = pts[ii, jj].x + (ci + di < 0 ? -S : ci + di >= Cells ? S : 0);
                                p.y = pts[ii, jj].y + (cj + dj < 0 ? -S : cj + dj >= Cells ? S : 0);
                                float d = Vector2.Distance(new Vector2(x, y), p);
                                if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                            }
                        }
                        float edge = Mathf.Clamp01((f2 - f1) / 10f);         // 0 at the gap between stones
                        float dome = 1f - Mathf.Clamp01(f1 / (S / Cells * 0.75f)); // rounded stone tops
                        n = Mathf.Clamp01(edge * 0.6f + dome * 0.3f + n * 0.25f);
                    }
                    h[x, y] = n;
                }
            }
            return h;
        }

        static float TexNoise(float x, float y, float f)
        {
            return Mathf.PerlinNoise(x * f + 1000f, y * f + 1000f) * 0.6f
                 + Mathf.PerlinNoise(x * f * 4f + 2000f, y * f * 4f + 2000f) * 0.4f;
        }

        // =====================================================================
        // Atmosphere
        // =====================================================================

        // A small per-governorate grade layered over the shared one (priority 1)
        static void CreateMoodVolume()
        {
            var m = R.mood ?? new Mood();
            string path = $"{Gen}/{R.id}_Mood.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            else
            {
                foreach (var old in profile.components.ToArray()) if (old != null) Object.DestroyImmediate(old, true);
                profile.components.Clear();
            }
            var ca = profile.Add<ColorAdjustments>(true);
            ca.colorFilter.Override(m.filter);
            foreach (var c in profile.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            EditorUtility.SetDirty(profile);
            var go = new GameObject("Mood Volume - " + R.name);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = profile;
        }

        static Light SetupAtmosphere()
        {
            var m = R.mood ?? new Mood();
            var sun = Object.FindAnyObjectByType<Light>();
            sun.name = "Sun";
            sun.color = m.sun;
            sun.intensity = m.sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            sun.transform.rotation = Quaternion.Euler(m.sunPitch, m.sunYaw, 0f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", m.atmosphere);
                sky.SetColor("_SkyTint", m.skyTint);
                sky.SetColor("_GroundColor", new Color(0.35f, 0.32f, 0.28f));
                sky.SetFloat("_Exposure", 1.0f);
                AssetDatabase.CreateAsset(sky, $"{Gen}/{R.id}_Sky.mat");
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = m.fog;
            RenderSettings.fogDensity = m.fogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = m.ambientSky;
            RenderSettings.ambientEquatorColor = m.ambientEquator;
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.09f);
            return sun;
        }

        // =====================================================================
        // Old town, bridge, arena
        // =====================================================================

        static void BuildTown(Transform world, TerrainData td, Material mudA, Material mudB, Material lime, Material roof, Material stone,
            Material wood, Material tile, Material glow, Material windowMat)
        {
            var town = new GameObject(R.townName).transform;
            town.SetParent(world);
            Vector2 origin = TownCenter - new Vector2(TownRadius, TownRadius);
            int cells = Mathf.CeilToInt(TownRadius * 2f / BlockSize);
            float inner = BlockSize - StreetWidth;
            Material[] walls = { mudA, mudB, lime };

            Vector2 plaza = Vector2.zero;
            float bestPlaza = float.MaxValue;

            for (int i = 0; i < cells; i++)
            {
                for (int j = 0; j < cells; j++)
                {
                    Vector2 blockMin = origin + new Vector2(i * BlockSize + StreetWidth, j * BlockSize + StreetWidth);
                    Vector2 c = blockMin + new Vector2(inner, inner) * 0.5f;
                    if (Vector2.Distance(c, TownCenter) > TownRadius - 18f) continue;
                    if (Mathf.Abs(c.x - RoadX(c.y)) < 24f) continue; // main road runs through here

                    float dCenter = Vector2.Distance(c, TownCenter);
                    if (dCenter < bestPlaza) { bestPlaza = dCenter; plaza = c; }
                }
            }

            for (int i = 0; i < cells; i++)
            {
                for (int j = 0; j < cells; j++)
                {
                    Vector2 blockMin = origin + new Vector2(i * BlockSize + StreetWidth, j * BlockSize + StreetWidth);
                    Vector2 c = blockMin + new Vector2(inner, inner) * 0.5f;
                    if (Vector2.Distance(c, TownCenter) > TownRadius - 18f) continue;
                    if (Mathf.Abs(c.x - RoadX(c.y)) < 24f) continue;
                    if (c == plaza) continue;

                    float lot = inner / 2f;
                    for (int a = 0; a < 2; a++)
                    {
                        for (int b = 0; b < 2; b++)
                        {
                            if (Random.value < 0.15f) continue; // courtyard
                            Vector2 lc = blockMin + new Vector2((a + 0.5f) * lot, (b + 0.5f) * lot);
                            float w = Random.Range(9f, lot - 1f);
                            float d = Random.Range(9f, lot - 1f);
                            int floors = Random.Range(1, 4);
                            float hgt = floors * 3.4f + Random.Range(0f, 0.8f);
                            var mat = walls[Random.Range(0, walls.Length)];

                            var house = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "House", town,
                                new Vector3(lc.x, TownHeight + hgt * 0.5f - 0.3f, lc.y), new Vector3(w, hgt, d), mat);
                            house.isStatic = true;
                            // flat roof with parapet (traditional Iraqi/Kurdish houses)
                            var parapet = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Parapet", town,
                                new Vector3(lc.x, TownHeight + hgt + 0.3f, lc.y), new Vector3(w + 0.3f, 0.6f, d + 0.3f), roof, keepCollider: false);
                            parapet.isStatic = true;
                            // a dark doorway facing the nearest street
                            var door = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Door", town,
                                new Vector3(lc.x, TownHeight + 1.1f, lc.y - d * 0.5f - 0.02f), new Vector3(1.3f, 2.3f, 0.1f), roof, keepCollider: false);
                            door.isStatic = true;

                            // arched top over the door
                            var arch = RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "DoorArch", town,
                                new Vector3(lc.x, TownHeight + 2.25f, lc.y - d * 0.5f - 0.03f), new Vector3(1.3f, 0.05f, 1.3f), roof, keepCollider: false);
                            arch.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                            arch.isStatic = true;

                            // stone plinth band at the base
                            var plinth = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Plinth", town,
                                new Vector3(lc.x, TownHeight + 0.15f, lc.y), new Vector3(w + 0.25f, 0.9f, d + 0.25f), stone, keepCollider: false);
                            plinth.isStatic = true;

                            // windows on all four sides; some glow warm from inside (people live here)
                            for (int f = 0; f < floors; f++)
                            {
                                float wy = TownHeight + 1.9f + f * 3.4f;
                                for (int side = 0; side < 4; side++)
                                {
                                    bool alongX = side < 2;
                                    float face = (side % 2 == 0 ? -1f : 1f);
                                    float span = alongX ? w : d;
                                    int count = span > 11f ? 3 : 2;
                                    for (int k = 0; k < count; k++)
                                    {
                                        float off = (k - (count - 1) * 0.5f) * (span / (count + 0.5f));
                                        if (f == 0 && side == 0 && Mathf.Abs(off) < 1.6f) continue; // the door
                                        if (Random.value < 0.2f) continue;
                                        var wm = Random.value < 0.14f ? glow : windowMat;
                                        Vector3 wp = alongX
                                            ? new Vector3(lc.x + off, wy, lc.y + face * (d * 0.5f + 0.03f))
                                            : new Vector3(lc.x + face * (w * 0.5f + 0.03f), wy, lc.y + off);
                                        Vector3 ws = alongX ? new Vector3(0.85f, 1.25f, 0.06f) : new Vector3(0.06f, 1.25f, 0.85f);
                                        var win = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Window", town, wp, ws, wm, keepCollider: false);
                                        win.isStatic = true;
                                    }
                                }
                            }

                            // shanasheel: wooden bay window on the upper floor (Baghdadi / Mosuli houses)
                            if (floors >= 2 && Random.value < 0.6f)
                            {
                                var sh = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Shanasheel", town,
                                    new Vector3(lc.x + Random.Range(-w * 0.2f, w * 0.2f), TownHeight + 4.6f, lc.y - d * 0.5f - 0.55f),
                                    new Vector3(w * 0.45f, 2.3f, 1.1f), wood, keepCollider: false);
                                sh.isStatic = true;
                                var shRoof = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "ShanasheelCap", town,
                                    sh.transform.position + Vector3.up * 1.25f, new Vector3(w * 0.5f, 0.2f, 1.4f), roof, keepCollider: false);
                                shRoof.isStatic = true;
                            }
                        }
                    }
                }
            }

            // Plaza with minaret
            var minaret = RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Minaret", town,
                new Vector3(plaza.x + 8f, TownHeight + 14f, plaza.y + 8f), new Vector3(3.2f, 14f, 3.2f), lime);
            minaret.isStatic = true;
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "MinaretBalcony", town,
                new Vector3(plaza.x + 8f, TownHeight + 24f, plaza.y + 8f), new Vector3(4.6f, 0.4f, 4.6f), stone, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "MinaretTop", town,
                new Vector3(plaza.x + 8f, TownHeight + 29f, plaza.y + 8f), new Vector3(2.6f, 3.4f, 2.6f), lime, keepCollider: false);
            // mosque with turquoise dome beside the minaret
            var mosque = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Mosque", town,
                new Vector3(plaza.x - 5f, TownHeight + 3.5f, plaza.y + 7f), new Vector3(14f, 7f, 14f), lime);
            mosque.isStatic = true;
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "DomeDrum", town,
                new Vector3(plaza.x - 5f, TownHeight + 7.5f, plaza.y + 7f), new Vector3(10f, 0.6f, 10f), lime, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Dome", town,
                new Vector3(plaza.x - 5f, TownHeight + 8f, plaza.y + 7f), new Vector3(10f, 9f, 10f), tile, keepCollider: false);

            // street lanterns at crossings
            for (int i = 0; i <= cells; i++)
            {
                for (int j = 0; j <= cells; j++)
                {
                    if ((i + j) % 2 != 0) continue;
                    var p = origin + new Vector2(i * BlockSize + StreetWidth * 0.5f, j * BlockSize + StreetWidth * 0.5f);
                    if (Vector2.Distance(p, TownCenter) > TownRadius - 10f) continue;
                    MakeLantern(town, new Vector3(p.x + 3.5f, TownHeight, p.y + 3.5f), wood, glow);
                }
            }

            // town gate on the southern road
            float gz = TownCenter.y - TownRadius - 6f;
            float gx = RoadX(gz);
            float gy = RoadTarget(gz);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "GatePillarL", town, new Vector3(gx - 7f, gy + 4.5f, gz), new Vector3(2.6f, 9f, 2.6f), lime);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "GatePillarR", town, new Vector3(gx + 7f, gy + 4.5f, gz), new Vector3(2.6f, 9f, 2.6f), lime);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "GateLintel", town, new Vector3(gx, gy + 9.8f, gz), new Vector3(17f, 2.2f, 2.8f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "GateCrown", town, new Vector3(gx, gy + 11.4f, gz), new Vector3(6f, 1.2f, 2.2f), tile, keepCollider: false);
            MakeLantern(town, new Vector3(gx - 5f, gy, gz - 2.5f), wood, glow);
            MakeLantern(town, new Vector3(gx + 5f, gy, gz - 2.5f), wood, glow);

            PlazaCenter = plaza;
        }

        static void MakeLantern(Transform parent, Vector3 basePos, Material wood, Material glow)
        {
            var root = new GameObject("Lantern").transform;
            root.SetParent(parent);
            root.position = basePos;
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Pole", root, new Vector3(0f, 1.6f, 0f), new Vector3(0.14f, 1.6f, 0.14f), wood);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Lamp", root, new Vector3(0f, 3.4f, 0f), new Vector3(0.42f, 0.6f, 0.42f), glow, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "LampCap", root, new Vector3(0f, 3.78f, 0f), new Vector3(0.6f, 0.12f, 0.6f), wood, keepCollider: false);
            var lightGo = new GameObject("LanternLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.3f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.62f, 0.32f);
            l.range = 10f;
            l.intensity = 2.6f;
            l.shadows = LightShadows.None;
        }

        static Vector2 PlazaCenter;
        static float BridgeZ;

        static void BuildBridge(Transform world, Material stone)
        {
            // find where the road meets the river
            float bestZ = 620f, best = float.MaxValue;
            for (float z = 540f; z < 720f; z += 0.5f)
            {
                float d = Mathf.Abs(z - RiverZ(RoadX(z)));
                if (d < best) { best = d; bestZ = z; }
            }
            BridgeZ = bestZ;
            float bx = RoadX(bestZ);
            float slope = (RoadX(bestZ + 1f) - RoadX(bestZ - 1f)) * 0.5f;
            float yaw = Mathf.Atan(slope) * Mathf.Rad2Deg;
            float deckY = RoadTarget(bestZ) + 0.6f;

            var bridge = new GameObject("جسر الخابور").transform;
            bridge.SetParent(world);
            bridge.position = new Vector3(bx, 0f, bestZ);
            bridge.rotation = Quaternion.Euler(0f, yaw, 0f);

            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Deck", bridge, new Vector3(0f, deckY, 0f), new Vector3(8f, 1.2f, 84f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "WallL", bridge, new Vector3(-3.8f, deckY + 1.0f, 0f), new Vector3(0.5f, 1f, 84f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "WallR", bridge, new Vector3(3.8f, deckY + 1.0f, 0f), new Vector3(0.5f, 1f, 84f), stone);
            var arch = RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Arch", bridge, new Vector3(0f, deckY - 13.2f, 0f), new Vector3(26f, 3.6f, 26f), stone, keepCollider: false);
            arch.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        static void BuildArena(Transform world, Material stone, Material glow)
        {
            var arena = new GameObject("Guardian Arena").transform;
            arena.SetParent(world);
            Vector3 c = new Vector3(ArenaCenter.x, ArenaHeight, ArenaCenter.y);
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "ArenaFloor", arena, c + Vector3.up * 0.05f, new Vector3(34f, 0.05f, 34f), stone);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                if (i == 10 || i == 11) continue; // south entrance
                float h = (i % 3 == 0) ? Random.Range(2f, 5f) : 10f;
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Column", arena,
                    c + new Vector3(Mathf.Cos(a) * 17f, h * 0.5f, Mathf.Sin(a) * 17f), new Vector3(1.6f, h * 0.5f, 1.6f), stone);
            }

            // braziers lighting the duel
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) / 4f * Mathf.PI * 2f;
                var bp = c + new Vector3(Mathf.Cos(a) * 12f, 0f, Mathf.Sin(a) * 12f);
                var br = new GameObject("Brazier").transform;
                br.SetParent(arena);
                br.position = bp;
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Stand", br, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.6f, 0.5f), stone);
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Bowl", br, new Vector3(0f, 1.3f, 0f), new Vector3(1.4f, 0.15f, 1.4f), stone, keepCollider: false);
                RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Fire", br, new Vector3(0f, 1.45f, 0f), new Vector3(0.9f, 0.4f, 0.9f), glow, keepCollider: false);
                var fl = new GameObject("FireLight");
                fl.transform.SetParent(br, false);
                fl.transform.localPosition = new Vector3(0f, 2f, 0f);
                var l = fl.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.5f, 0.2f);
                l.range = 14f;
                l.intensity = 3.5f;
                l.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
                RafidainSceneBuilder.MakeEmbers(br, new Vector3(0f, 1.5f, 0f), 18f);
                RafidainSceneBuilder.AddLoop(br.gameObject, "fire", 0.45f, 14f);
            }

            // two Assyrian guardian statues (winged-bull style) at the entrance
            for (int s = -1; s <= 1; s += 2)
            {
                var st = new GameObject("Guardian Statue").transform;
                st.SetParent(arena);
                st.position = c + new Vector3(s * 6f, 0f, -21f);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Plinth", st, new Vector3(0f, 0.75f, 0f), new Vector3(3.4f, 1.5f, 6.4f), stone);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Body", st, new Vector3(0f, 3.4f, 0f), new Vector3(2.6f, 3.8f, 5.6f), stone);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Wing", st, new Vector3(s * 1.4f, 4.4f, 0.4f), new Vector3(0.4f, 3.4f, 4.6f), stone, keepCollider: false);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Head", st, new Vector3(0f, 6.1f, -2.3f), new Vector3(1.6f, 1.9f, 1.6f), stone, keepCollider: false);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Beard", st, new Vector3(0f, 5.0f, -2.85f), new Vector3(1.2f, 1.3f, 0.5f), stone, keepCollider: false);
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Crown", st, new Vector3(0f, 7.4f, -2.3f), new Vector3(1.5f, 0.6f, 1.5f), stone, keepCollider: false);
            }
        }

        // =====================================================================
        // Nature
        // =====================================================================

        static void ScatterNature(Transform world, TerrainData td, Material bark, Material leaves, Material rock)
        {
            var forest = new GameObject("Oak Forest").transform;
            forest.SetParent(world);
            int placed = 0, tries = 0;
            while (placed < 320 && tries < 6000)
            {
                tries++;
                float x = Random.Range(20f, Size - 20f), z = Random.Range(20f, Size - 20f);
                float u = x / Size, v = z / Size;
                float h = td.GetInterpolatedHeight(u, v);
                if (h < 24f || h > 120f || td.GetSteepness(u, v) > 28f) continue;
                if (!IsFreeGround(x, z, 14f)) continue;

                float s = Random.Range(0.8f, 1.4f);
                var tree = new GameObject("Oak").transform;
                tree.SetParent(forest);
                tree.position = new Vector3(x, h - 0.3f, z);
                tree.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 2.5f * s, 0f), new Vector3(0.5f * s, 2.5f * s, 0.5f * s), bark);
                var limb = RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Branch", tree, new Vector3(0.6f * s, 4.6f * s, 0f), new Vector3(0.25f * s, 1.3f * s, 0.25f * s), bark, keepCollider: false);
                limb.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
                int blobs = Random.Range(3, 6);
                for (int bI = 0; bI < blobs; bI++)
                {
                    var off = new Vector3(Random.Range(-1.8f, 1.8f), Random.Range(5.2f, 7.6f), Random.Range(-1.8f, 1.8f)) * s;
                    float cs = Random.Range(3.2f, 5.6f) * s;
                    MeshObj("Canopy", tree, ProceduralMeshes.Canopy(Random.Range(0, 6)), leaves, off,
                        new Vector3(cs, cs * Random.Range(0.7f, 0.9f), cs), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), false);
                }
                placed++;
            }

            var rocks = new GameObject("Rocks").transform;
            rocks.SetParent(world);
            placed = 0; tries = 0;
            while (placed < 220 && tries < 6000)
            {
                tries++;
                float x = Random.Range(10f, Size - 10f), z = Random.Range(10f, Size - 10f);
                if (!IsFreeGround(x, z, 10f)) continue;
                float h = td.GetInterpolatedHeight(x / Size, z / Size);
                float s = Random.Range(1.5f, h > 60f ? 10f : 4.5f);
                var r = MeshObj("Rock", rocks, ProceduralMeshes.Rock(Random.Range(0, 10)), rock,
                    new Vector3(x, h + s * 0.05f, z), new Vector3(s, s * Random.Range(0.7f, 1.2f), s),
                    Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)), true);
                r.isStatic = true;
                // small stones around big ones
                if (s > 3f)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Vector2 d2 = Random.insideUnitCircle * s * 0.9f;
                        float px = x + d2.x, pz = z + d2.y;
                        float ph = td.GetInterpolatedHeight(px / Size, pz / Size);
                        float ss = Random.Range(0.4f, 1.2f);
                        var st = MeshObj("Stone", rocks, ProceduralMeshes.Rock(Random.Range(0, 10)), rock,
                            new Vector3(px, ph, pz), Vector3.one * ss, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), false);
                        st.isStatic = true;
                    }
                }
                placed++;
            }
        }

        static void ScatterPalms(Transform world, TerrainData td, Material bark, Material leaf)
        {
            var grove = new GameObject("Palm Groves (Khabur banks)").transform;
            grove.SetParent(world);
            int placed = 0, tries = 0;
            while (placed < 80 && tries < 8000)
            {
                tries++;
                float x = Random.Range(30f, Size - 30f);
                float dRiver = Random.Range(30f, 75f) * (Random.value < 0.5f ? -1f : 1f);
                float z = RiverZ(x) + dRiver;
                float h = td.GetInterpolatedHeight(x / Size, z / Size);
                if (h < WaterLevel + 1f || h > 50f) continue;
                if (Mathf.Abs(x - RoadX(z)) < 16f) continue;

                var palm = new GameObject("Palm").transform;
                palm.SetParent(grove);
                palm.position = new Vector3(x, h - 0.2f, z);
                palm.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                int segs = Random.Range(6, 9);
                float lean = Random.Range(0.05f, 0.25f);
                Vector3 top = Vector3.zero;
                for (int i = 0; i < segs; i++)
                {
                    var seg = RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Trunk", palm,
                        new Vector3(lean * i * i * 0.12f, 0.75f + i * 1.45f, 0f), new Vector3(0.42f - i * 0.015f, 0.75f, 0.42f - i * 0.015f), bark, keepCollider: i == 0);
                    top = seg.transform.localPosition + Vector3.up * 0.8f;
                }
                for (int f = 0; f < 10; f++)
                {
                    var pivot = new GameObject("Frond").transform;
                    pivot.SetParent(palm, false);
                    pivot.localPosition = top;
                    pivot.localRotation = Quaternion.Euler(Random.Range(18f, 40f), f * 36f + Random.Range(-8f, 8f), 0f);
                    RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Leaf", pivot, new Vector3(0f, 0f, 2.2f), new Vector3(0.7f, 0.06f, 4.4f), leaf, keepCollider: false);
                }
                placed++;
            }
        }

        static Texture2D FlowerTexture(string name, Color petal, Color centre)
        {
            string path = $"{Gen}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var clear = new Color(0.3f, 0.4f, 0.2f, 0f);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) tex.SetPixel(x, y, clear);
            var stem = new Color(0.25f, 0.38f, 0.14f, 1f);
            var rng = new System.Random(name.GetHashCode());
            for (int f = 0; f < 5; f++)
            {
                int bx = 14 + rng.Next(S - 28);
                int top = 50 + rng.Next(60);
                for (int y = 0; y < top; y++) for (int x = bx - 1; x <= bx + 1; x++) tex.SetPixel(x, y, stem);
                int pr = 6 + rng.Next(5);
                for (int y = -pr; y <= pr; y++)
                    for (int x = -pr; x <= pr; x++)
                    {
                        float d = Mathf.Sqrt(x * x + y * y);
                        if (d > pr) continue;
                        int px = bx + x, py = top + y;
                        if (px < 0 || px >= S || py < 0 || py >= S) continue;
                        var c = d < pr * 0.35f ? centre : petal * (0.85f + 0.15f * (1f - d / pr));
                        c.a = 1f;
                        tex.SetPixel(px, py, c);
                    }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Birds over the town and the arena, and hollow townsfolk sitting against the walls
        static void AddLife(Transform world, TerrainData td)
        {
            var life = new GameObject("Life").transform;
            life.SetParent(world);

            void Flock(string name, Vector2 at, float r, float h, int n, float size, Color c)
            {
                var go = new GameObject(name);
                go.transform.SetParent(life);
                go.transform.position = Ground(td, at.x, at.y, 0f);
                var f = go.AddComponent<BirdFlock>();
                f.radius = r; f.height = h; f.count = n; f.birdSize = size; f.color = c;
            }
            Flock("Crows over the town", TownCenter, 45f, 28f, 11, 0.55f, new Color(0.04f, 0.04f, 0.045f));
            Flock("Vultures over the arena", ArenaCenter, 26f, 40f, 6, 1.1f, new Color(0.12f, 0.09f, 0.07f));
            Flock("Eagles over the mountains", new Vector2(850f, 500f), 70f, 80f, 3, 1.4f, new Color(0.25f, 0.18f, 0.12f));
            Flock("Birds over the road", new Vector2(RoadX(150f), 150f), 35f, 30f, 7, 0.45f, new Color(0.08f, 0.07f, 0.06f));

            // hollow townsfolk: sitting, heads bowed, harmless
            Vector2 origin = TownCenter - new Vector2(TownRadius, TownRadius);
            int placed = 0;
            for (int k = 0; k < 60 && placed < 9; k++)
            {
                int i = Random.Range(1, 8), j = Random.Range(1, 8);
                Vector2 p = origin + new Vector2(i * BlockSize + StreetWidth - 0.8f, j * BlockSize + Random.Range(StreetWidth + 2f, BlockSize - 2f));
                if (Vector2.Distance(p, TownCenter) > TownRadius - 15f) continue;
                if (Mathf.Abs(p.x - RoadX(p.y)) < 20f) continue;
                var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npc.name = "Hollow Townsfolk";
                Object.DestroyImmediate(npc.GetComponent<CapsuleCollider>());
                npc.transform.SetParent(life);
                npc.transform.position = Ground(td, p.x, p.y, 1f);
                npc.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                RafidainSceneBuilder.BuildHumanoid(npc, 1);
                var rig = npc.GetComponent<HumanoidRig>();
                if (rig != null)
                {
                    // sitting pose: body down, legs forward, head bowed
                    rig.body.localPosition = new Vector3(0f, -0.48f, 0f);
                    rig.body.localRotation = Quaternion.Euler(14f, 0f, 0f);
                    rig.legL.localRotation = Quaternion.Euler(-85f, 0f, 0f);
                    rig.legR.localRotation = Quaternion.Euler(-80f, 0f, 0f);
                    rig.armL.localRotation = Quaternion.Euler(-30f, 0f, 0f);
                    rig.armR.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                    Object.DestroyImmediate(rig);
                }
                var weapon = npc.GetComponentsInChildren<Transform>(true);
                foreach (var t in weapon) if (t.name == "WeaponPivot") { Object.DestroyImmediate(t.gameObject); break; }
                placed++;
            }
        }

        // Ripple normal maps for the river/lake: tileable sine-wave sums, scrolled at runtime by WaterFlow
        static void SetupWaterMaterial(Material water)
        {
            var nrm = WaterNormal("WaterRipples", 7, 0.9f);
            var nrm2 = WaterNormal("WaterRipplesFine", 23, 0.5f);
            water.SetColor("_BaseColor", new Color(0.07f, 0.15f, 0.16f));
            water.SetFloat("_Smoothness", 0.96f);
            water.SetFloat("_Metallic", 0.05f);
            if (nrm != null)
            {
                water.SetTexture("_BumpMap", nrm);
                water.SetFloat("_BumpScale", 0.6f);
                water.EnableKeyword("_NORMALMAP");
                water.mainTextureScale = new Vector2(60f, 60f);
            }
            if (nrm2 != null && water.HasProperty("_DetailNormalMap"))
            {
                water.SetTexture("_DetailNormalMap", nrm2);
                water.SetFloat("_DetailNormalMapScale", 0.5f);
                water.SetVector("_DetailAlbedoMap_ST", new Vector4(140f, 140f, 0f, 0f));
                water.EnableKeyword("_DETAIL_MULX2");
            }
            EditorUtility.SetDirty(water);
        }

        static Texture2D WaterNormal(string name, int baseFreq, float strength)
        {
            string path = $"{Gen}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int S = 256;
            var rng = new System.Random(baseFreq * 31);
            int waves = 6;
            var kx = new int[waves]; var ky = new int[waves]; var ph = new float[waves]; var amp = new float[waves];
            for (int w = 0; w < waves; w++)
            {
                kx[w] = rng.Next(-baseFreq, baseFreq + 1);
                ky[w] = rng.Next(1, baseFreq + 1);
                ph[w] = (float)rng.NextDouble() * Mathf.PI * 2f;
                amp[w] = 1f / (1f + w * 0.6f);
            }
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = 0f, dy = 0f;
                    for (int w = 0; w < waves; w++)
                    {
                        float a = 2f * Mathf.PI * (kx[w] * x + ky[w] * y) / S + ph[w];
                        float c = Mathf.Cos(a) * amp[w];
                        dx += c * kx[w];
                        dy += c * ky[w];
                    }
                    var n = new Vector3(-dx * strength * 0.08f, -dy * strength * 0.08f, 1f).normalized;
                    tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Small props stop casting shadows and fade out at distance; static props get batched.
        static void Optimize(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = r.gameObject;
                if (go.GetComponentInParent<BirdFlock>() != null || go.GetComponentInParent<WaterFlow>() != null) continue;
                float size = r.bounds.size.magnitude;

                if (size < 1.6f) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                if (go.GetComponent<LODGroup>() == null && size < 14f)
                {
                    var lod = go.AddComponent<LODGroup>();
                    float cull = size < 1.6f ? 0.025f : size < 5f ? 0.012f : 0.006f;
                    lod.SetLODs(new[] { new LOD(cull, new Renderer[] { r }) });
                    lod.RecalculateBounds();
                }

                bool movable = go.GetComponentInParent<EnemyAI>() != null || go.GetComponentInParent<MapPiece>() != null
                            || go.GetComponentInParent<SacredOil>() != null || go.GetComponentInParent<UnderworldDoor>() != null;
                if (!movable)
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        static void ScatterBushes(Transform world, TerrainData td, Material leaves)
        {
            var bushes = new GameObject("Bushes").transform;
            bushes.SetParent(world);
            var dark = RafidainSceneBuilder.Mat("BushLeaves", new Color(0.18f, 0.24f, 0.11f), 0.08f);
            int placed = 0, tries = 0;
            while (placed < 380 && tries < 8000)
            {
                tries++;
                float x = Random.Range(20f, Size - 20f), z = Random.Range(20f, Size - 20f);
                float u = x / Size, v = z / Size;
                float h = td.GetInterpolatedHeight(u, v);
                if (h < WaterLevel + 1f || h > 110f || td.GetSteepness(u, v) > 32f) continue;
                if (!IsFreeGround(x, z, 3f)) continue;
                float sz = Random.Range(1.2f, 2.8f);
                var b = MeshObj("Bush", bushes, ProceduralMeshes.Canopy(Random.Range(0, 6)), Random.value < 0.5f ? leaves : dark,
                    new Vector3(x, h + sz * 0.25f, z), new Vector3(sz, sz * 0.7f, sz), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), false);
                b.isStatic = true;
                placed++;
            }
        }

        // Ground mist creeping through the valley and along the water, fireflies by the river at dusk
        static void AddAtmosphereFX(Transform world, TerrainData td)
        {
            var fx = new GameObject("Atmosphere FX").transform;
            fx.SetParent(world);
            var mistMat = RafidainSceneBuilder.ParticleMat("MistParticle", false);
            var flyMat = RafidainSceneBuilder.ParticleMat("EmberParticle", true);

            var spots = new System.Collections.Generic.List<Vector3>();
            for (float z = 90f; z < 900f; z += 110f) spots.Add(OnRoad(td, z, Random.Range(-30f, 30f), 0.5f));
            if (R.river)
                for (float x = 80f; x < 1000f; x += 140f)
                {
                    var rp = new Vector3(x, WaterLevel + 0.6f, RiverZ(x));
                    spots.Add(rp);
                    var water = new GameObject("River Sound");
                    water.transform.SetParent(fx);
                    water.transform.position = rp;
                    RafidainSceneBuilder.AddLoop(water, "river", 0.6f, 45f);
                }
            if (R.lake) for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2f;
                spots.Add(new Vector3(R.lakeCenter.x + Mathf.Cos(a) * R.lakeRadius * 0.7f, WaterLevel + 0.6f, R.lakeCenter.y + Mathf.Sin(a) * R.lakeRadius * 0.7f));
            }

            foreach (var p in spots)
            {
                var go = new GameObject("Ground Mist");
                go.transform.SetParent(fx);
                go.transform.position = p;
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 18f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
                main.startSize = new ParticleSystem.MinMaxCurve(9f, 18f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.78f, 0.74f, 0.70f, 0.07f), new Color(0.70f, 0.66f, 0.62f, 0.04f));
                main.maxParticles = 50;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.prewarm = true;
                var em = ps.emission; em.rateOverTime = 3f;
                var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(70f, 1.5f, 70f);
                var col = ps.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = mistMat;
                r.sortingFudge = 10f;
            }

            // fireflies near the water / palms
            if (R.river || R.lake)
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector3 p = R.river
                        ? new Vector3(90f + k * 115f, 0f, 0f)
                        : new Vector3(R.lakeCenter.x + Mathf.Cos(k) * (R.lakeRadius + 30f), 0f, R.lakeCenter.y + Mathf.Sin(k) * (R.lakeRadius + 30f));
                    if (R.river) p.z = RiverZ(p.x) + (k % 2 == 0 ? 40f : -40f);
                    p.y = td.GetInterpolatedHeight(p.x / Size, p.z / Size) + 1.5f;

                    var go = new GameObject("Fireflies");
                    go.transform.SetParent(fx);
                    go.transform.position = p;
                    var ps = go.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
                    main.startSpeed = 0.1f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 1f, 0.45f), new Color(1f, 0.9f, 0.4f));
                    main.maxParticles = 80;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.prewarm = true;
                    var em = ps.emission; em.rateOverTime = 12f;
                    var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(30f, 3f, 30f);
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.4f;
                    var col = ps.colorOverLifetime; col.enabled = true;
                    var g = new Gradient();
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.2f, 0.5f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
                    col.color = g;
                    go.GetComponent<ParticleSystemRenderer>().sharedMaterial = flyMat;
                }
            }
        }

        static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 scale, Quaternion rot, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        static bool IsFreeGround(float x, float z, float margin)
        {
            if (z > 20f && z < 975f && Mathf.Abs(x - RoadX(z)) < 12f + margin) return false;
            if (Vector2.Distance(new Vector2(x, z), TownCenter) < TownRadius + margin) return false;
            if (Mathf.Abs(z - RiverZ(x)) < 26f + margin * 0.5f) return false;
            if (R.lake && Vector2.Distance(new Vector2(x, z), R.lakeCenter) < R.lakeRadius + 10f + margin * 0.5f) return false;
            if (Vector2.Distance(new Vector2(x, z), ArenaCenter) < 60f) return false;
            return true;
        }

        // =====================================================================
        // Gameplay
        // =====================================================================

        static Vector3 Ground(TerrainData td, float x, float z, float up)
            => new Vector3(x, td.GetInterpolatedHeight(x / Size, z / Size) + up, z);

        static Vector3 OnRoad(TerrainData td, float z, float side, float up) => Ground(td, RoadX(z) + side, z, up);

        static void PlaceGameplay(TerrainData td, Light sun, Material furatMat, Material hollow, Material guardian, Material steel, Material ember, Material stone)
        {
            // Furat
            var furat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            furat.name = "Furat";
            furat.tag = "Player";
            Object.DestroyImmediate(furat.GetComponent<CapsuleCollider>());
            furat.transform.position = OnRoad(td, 55f, 0f, 1.1f);
            furat.GetComponent<Renderer>().sharedMaterial = furatMat;
            var cc = furat.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero;
            furat.AddComponent<PlayerStats>();
            furat.AddComponent<PlayerController>();
            var combat = furat.AddComponent<PlayerCombat>();
            combat.weaponPivot = RafidainSceneBuilder.MakeWeapon(furat.transform, steel, 1.1f);
            RafidainSceneBuilder.BuildHumanoid(furat, 0);

            // Camera
            var cam = Camera.main;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1600f;
            var rig = cam.gameObject.AddComponent<ThirdPersonCamera>();
            rig.target = furat.transform;
            cam.transform.position = furat.transform.position + new Vector3(0f, 2f, -4f);
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            RafidainSceneBuilder.MakeAsh(cam.transform);
            MakeWeather(cam.transform);

            // Systems
            var sys = new GameObject("_Game");
            sys.AddComponent<PlayerInputReader>();
            var gm = sys.AddComponent<GameManager>();
            var hud = sys.AddComponent<HUD>();
            sys.AddComponent<MobileControls>();
            sys.AddComponent<IntroSequence>();
            sys.AddComponent<PauseMenu>();
            sys.AddComponent<LevelUpUI>();
            RafidainSceneBuilder.AddAudioManager(sys);
            gm.player = furat.GetComponent<PlayerStats>();
            hud.player = gm.player;

            // Flames
            if (!R.river) BridgeZ = 600f;
            var gate = RafidainSceneBuilder.MakeBonfire(R.id + "_gate", R.flameNames[0], OnRoad(td, 68f, 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire(R.id + "_oldtown", R.flameNames[1], Ground(td, PlazaCenter.x - 6f, PlazaCenter.y - 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire(R.id + "_water", R.flameNames[2], OnRoad(td, BridgeZ - 52f, 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire(R.id + "_ascent", R.flameNames[3], OnRoad(td, 835f, 6f, 0f), ember, stone);
            gm.defaultBonfire = gate;

            // Enemies
            var enemies = new GameObject("Enemies").transform;
            float[] roadZ = { 140f, 190f, 235f, 470f, 520f, 700f, 745f, 790f };
            foreach (var z in roadZ)
                RafidainSceneBuilder.MakeEnemy(enemies, "Hollow Soldier", OnRoad(td, z, Random.Range(-4f, 4f), 1.05f), hollow, steel, 1f);

            // Town patrols in the streets
            Vector2 origin = TownCenter - new Vector2(TownRadius, TownRadius);
            int townCount = 0;
            for (int i = 1; i < 7 && townCount < 9; i++)
            {
                float sx = origin.x + i * BlockSize + StreetWidth * 0.5f;
                float sz = TownCenter.y + Random.Range(-90f, 90f);
                if (Vector2.Distance(new Vector2(sx, sz), TownCenter) > TownRadius - 15f) continue;
                RafidainSceneBuilder.MakeEnemy(enemies, "Hollow Soldier", Ground(td, sx, sz, 1.05f), hollow, steel, 1f);
                townCount++;
            }

            // Archers on the slopes above the road
            float[] archerZ = { 260f, 560f, 760f };
            for (int k = 0; k < archerZ.Length; k++)
            {
                float side = (k % 2 == 0 ? 1f : -1f) * Random.Range(14f, 22f);
                var archer = RafidainSceneBuilder.MakeEnemy(enemies, "Hollow Archer", OnRoad(td, archerZ[k], side, 1.05f), hollow, steel, 1f);
                archer.ranged = true;
                archer.maxHealth = 55f;
                archer.damage = 15f;
                archer.detectRange = 22f;
                archer.leashRange = 36f;
                archer.emberReward = 70;
            }

            // Spearmen: long, narrow thrusts with a fast lunge — step to the side, don't back away
            float[] spearZ = { 300f, 640f, 860f };
            foreach (var sz in spearZ)
            {
                var spear = RafidainSceneBuilder.MakeEnemy(enemies, "Hollow Spearman", OnRoad(td, sz, Random.Range(-3f, 3f), 1.05f), hollow, steel, 1.05f);
                spear.maxHealth = 95f;
                spear.damage = 24f;
                spear.attackRange = 3.6f;
                spear.attackReach = 4.3f;
                spear.attackAngle = 22f;
                spear.windup = 0.85f;
                spear.lungeSpeed = 11f;
                spear.recovery = 1.1f;
                spear.emberReward = 90;
                foreach (var t in spear.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "Blade") continue;
                    t.localScale = new Vector3(0.045f, 0.045f, 2.6f);
                    t.localPosition = new Vector3(0f, 0f, 1.1f);
                    RafidainSceneBuilder.Prim(PrimitiveType.Cube, "SpearHead", t.parent, new Vector3(0f, 0f, 2.45f), new Vector3(0.12f, 0.03f, 0.35f), steel, keepCollider: false)
                        .transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                    break;
                }
            }

            // Feral dogs: packs of 2-3 on the outskirts — fast, fragile, they circle in and bite
            var dogFur = RafidainSceneBuilder.Mat("DogFur", new Color(0.36f, 0.29f, 0.22f), 0.05f);
            var dogDark = RafidainSceneBuilder.Mat("DogFurDark", new Color(0.14f, 0.11f, 0.09f), 0.05f);
            Vector3[] packs = { OnRoad(td, 190f, 13f, 0f), OnRoad(td, 500f, -14f, 0f), OnRoad(td, 730f, 12f, 0f) };
            for (int p = 0; p < packs.Length; p++)
            {
                int count = Random.Range(2, 4);
                for (int k = 0; k < count; k++)
                {
                    Vector2 off = Random.insideUnitCircle * 3f;
                    MakeDog(enemies, Ground(td, packs[p].x + off.x, packs[p].z + off.y, 0.55f), dogFur, dogDark);
                }
            }

            // Bridge guards
            var brute = RafidainSceneBuilder.MakeEnemy(enemies, "Bridge Brute", OnRoad(td, BridgeZ + 50f, 0f, 1.4f), hollow, steel, 1.35f, 3);
            brute.maxHealth = 180f; brute.damage = 30f; brute.windup = 1.0f; brute.moveSpeed = 2.3f; brute.emberReward = 180;
            var brute2 = RafidainSceneBuilder.MakeEnemy(enemies, "Mountain Brute", OnRoad(td, 815f, -3f, 1.4f), hollow, steel, 1.35f, 3);
            brute2.maxHealth = 200f; brute2.damage = 32f; brute2.windup = 1.0f; brute2.moveSpeed = 2.3f; brute2.emberReward = 220;

            // Boss
            var boss = RafidainSceneBuilder.MakeEnemy(enemies, R.bossName,
                new Vector3(ArenaCenter.x, ArenaHeight + 1.7f, ArenaCenter.y + 8f), guardian, steel, 1.6f, 2);
            boss.isBoss = true;
            boss.bossId = R.bossId;
            boss.maxHealth = 700f + 260f * R.tier;
            boss.damage = 34f;
            boss.windup = 0.9f;
            boss.recovery = 1.1f;
            boss.attackRange = 2.4f;
            boss.attackReach = 2.4f;
            boss.moveSpeed = 3.3f;
            boss.detectRange = 15f;
            boss.leashRange = 32f;
            boss.emberReward = 2000;
            boss.canLeap = true;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            switch (R.id)
            {
                case "duhok": boss.special = EnemyAI.BossMove.Wave; break;
                case "mosul": boss.special = EnemyAI.BossMove.WingGust; break;
                case "erbil": boss.special = EnemyAI.BossMove.ShieldCharge; break;
                case "kirkuk": boss.special = EnemyAI.BossMove.FireTrail; break;
                case "samarra": boss.special = EnemyAI.BossMove.Spin; break;
                case "baghdad": boss.special = EnemyAI.BossMove.ShadowStep; boss.specialCooldown = 6f; break;
            }
            if (R.finalRegion)
            {
                boss.maxHealth = 3200f;
                boss.damage = 46f;
                boss.moveSpeed = 3.8f;
                boss.leapCooldown = 5f;
                boss.transform.localScale = Vector3.one * 1.9f;
            }

            // mist seals the arena once the guardian wakes (the final arena has its own door)
            if (!R.finalRegion)
            {
                var arenaSeal = new GameObject("Arena Mist").AddComponent<BossArena>();
                arenaSeal.transform.position = new Vector3(ArenaCenter.x, ArenaHeight, ArenaCenter.y);
                arenaSeal.boss = boss;
            }

            // each governorate on the road to Baghdad is harder
            float hp = 1f + 0.25f * R.tier, dmg = 1f + 0.15f * R.tier;
            foreach (var e in enemies.GetComponentsInChildren<EnemyAI>(true))
            {
                if (e.isBoss) { e.damage *= dmg; continue; }
                e.maxHealth *= hp;
                e.damage *= dmg;
                e.emberReward = Mathf.RoundToInt(e.emberReward * (1f + 0.5f * R.tier));
            }
        }

        // =====================================================================
        // Landmarks of each governorate
        // =====================================================================

        static void BuildLandmark(Transform world, TerrainData td, Material stone, Material mud, Material lime, Material glow, Material ember)
        {
            var root = new GameObject("Landmark - " + R.landmark).transform;
            root.SetParent(world);
            switch (R.landmark)
            {
                case "nineveh": BuildNineveh(root, td, stone, mud); break;
                case "citadel": BuildCitadelWalls(root, td, mud, lime); break;
                case "fire": BuildEternalFire(root, td, ember); break;
                case "malwiya": BuildMalwiya(root, td, mud); break;
                case "underworld": BuildZiggurat(root, mud, stone); break;
            }
        }

        static void MakeLamassu(Transform parent, Vector3 pos, float side, Material stone)
        {
            var st = new GameObject("Lamassu").transform;
            st.SetParent(parent);
            st.position = pos;
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Plinth", st, new Vector3(0f, 0.75f, 0f), new Vector3(3.4f, 1.5f, 6.4f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Body", st, new Vector3(0f, 3.4f, 0f), new Vector3(2.6f, 3.8f, 5.6f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Wing", st, new Vector3(side * 1.4f, 4.4f, 0.4f), new Vector3(0.4f, 3.4f, 4.6f), stone, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Head", st, new Vector3(0f, 6.1f, -2.3f), new Vector3(1.6f, 1.9f, 1.6f), stone, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Beard", st, new Vector3(0f, 5.0f, -2.85f), new Vector3(1.2f, 1.3f, 0.5f), stone, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Crown", st, new Vector3(0f, 7.4f, -2.3f), new Vector3(1.5f, 0.6f, 1.5f), stone, keepCollider: false);
        }

        // Mosul: the Nergal-style gate of Nineveh with winged bulls and broken mud-brick walls
        static void BuildNineveh(Transform root, TerrainData td, Material stone, Material mud)
        {
            float gz = 540f;
            Vector3 c = OnRoad(td, gz, 0f, 0f);
            MakeLamassu(root, c + new Vector3(-6.5f, 0f, 0f), -1f, stone);
            MakeLamassu(root, c + new Vector3(6.5f, 0f, 0f), 1f, stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "GateTowerL", root, c + new Vector3(-11f, 6f, 1f), new Vector3(5f, 12f, 6f), mud);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "GateTowerR", root, c + new Vector3(11f, 6f, 1f), new Vector3(5f, 12f, 6f), mud);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (Random.value < 0.25f) continue; // breaches in the wall
                    float x = c.x + side * (16f + k * 9f);
                    float h = Random.Range(3f, 8f);
                    var g = Ground(td, x, gz, 0f);
                    RafidainSceneBuilder.Prim(PrimitiveType.Cube, "NinevehWall", root, new Vector3(x, g.y + h * 0.5f - 0.5f, gz + 1f), new Vector3(9f, h, 3.5f), mud).isStatic = true;
                }
            }
            // scattered ruins on the eastern hill
            for (int k = 0; k < 14; k++)
            {
                float x = Random.Range(640f, 820f), z = Random.Range(420f, 560f);
                if (!IsFreeGround(x, z, 2f)) continue;
                var g = Ground(td, x, z, 0f);
                float h = Random.Range(1.5f, 5f);
                var w = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Ruin", root, g + Vector3.up * (h * 0.5f - 0.4f), new Vector3(Random.Range(3f, 8f), h, 2f), mud);
                w.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
                w.isStatic = true;
            }
        }

        // Erbil: the walls of the citadel ringing the mound
        static void BuildCitadelWalls(Transform root, TerrainData td, Material mud, Material lime)
        {
            const int Segments = 52;
            float r = TownRadius + 6f;
            for (int i = 0; i < Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                Vector2 p = TownCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Mathf.Abs(p.x - RoadX(p.y)) < 13f) continue; // the gate where the road climbs in
                float len = 2f * Mathf.PI * r / Segments + 0.6f;
                var seg = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "CitadelWall", root,
                    new Vector3(p.x, TownHeight + 4f, p.y), new Vector3(len, 11f, 3f), mud);
                seg.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                seg.isStatic = true;
                if (i % 6 == 0)
                    RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "CitadelTower", root,
                        new Vector3(p.x, TownHeight + 5.5f, p.y), new Vector3(6f, 7.5f, 6f), lime).isStatic = true;
            }
        }

        // Kirkuk: Baba Gurgur, ground that has burned since before the prophets
        static void BuildEternalFire(Transform root, TerrainData td, Material ember)
        {
            int placed = 0, tries = 0;
            while (placed < 34 && tries < 3000)
            {
                tries++;
                float z = Random.Range(430f, 780f);
                float x = RoadX(z) + Random.Range(18f, 150f) * (Random.value < 0.5f ? -1f : 1f);
                if (!IsFreeGround(x, z, 1f)) continue;
                var g = Ground(td, x, z, 0f);
                if (g.y > 60f) continue;
                var vent = new GameObject("Eternal Fire").transform;
                vent.SetParent(root);
                vent.position = g;
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Burning Ground", vent, new Vector3(0f, 0.03f, 0f), new Vector3(3.4f, 0.03f, 3.4f), ember, keepCollider: false);
                if (placed % 3 == 0)
                {
                    var l = new GameObject("Firelight").AddComponent<Light>();
                    l.transform.SetParent(vent, false);
                    l.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.45f, 0.12f);
                    l.range = 11f;
                    l.intensity = 3.5f;
                }
                var em = RafidainSceneBuilder.MakeEmbers(vent, new Vector3(0f, 0.1f, 0f), 28f);
                var shape = em.shape; shape.radius = 1.3f;
                vent.gameObject.AddComponent<FireHazard>().radius = 1.6f;
                if (placed % 2 == 0) RafidainSceneBuilder.AddLoop(vent.gameObject, "fire", 0.5f, 12f);
                placed++;
            }
        }

        // Samarra: the spiral minaret, climbable by its outer ramp
        static void BuildMalwiya(Transform root, TerrainData td, Material mud)
        {
            Vector2 c2 = TownCenter + new Vector2(TownRadius + 48f, 10f);
            float g = td.GetInterpolatedHeight(c2.x / Size, c2.y / Size);
            Vector3 c = new Vector3(c2.x, g, c2.y);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Base", root, c + new Vector3(0f, -2f, 0f), new Vector3(38f, 10f, 38f), mud).isStatic = true;
            float baseY = g + 3f;
            const int Tiers = 5;
            const float TierH = 5.5f;
            for (int i = 0; i < Tiers; i++)
            {
                float r = 15f - i * 2.2f;
                RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Tier", root,
                    new Vector3(c.x, baseY + i * TierH + TierH * 0.5f, c.z), new Vector3(r * 2f, TierH * 0.5f, r * 2f), mud).isStatic = true;
            }
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Pavilion", root,
                new Vector3(c.x, baseY + Tiers * TierH + 1.5f, c.z), new Vector3(5f, 1.5f, 5f), mud).isStatic = true;

            const int Steps = 180;
            float total = Tiers * TierH;
            for (int k = 0; k < Steps; k++)
            {
                float t = k / (float)Steps;
                float y = baseY + t * total;
                int tier = Mathf.Min(Tiers - 1, Mathf.FloorToInt(t * Tiers));
                float rr = 15f - tier * 2.2f + 1.3f;
                float a = t * Mathf.PI * 2f * Tiers;
                Vector3 p = new Vector3(c.x + Mathf.Cos(a) * rr, y, c.z + Mathf.Sin(a) * rr);
                Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                var step = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Ramp", root, p, new Vector3(2.6f, 0.4f, 2.4f), mud);
                step.transform.rotation = Quaternion.LookRotation(tangent);
                step.isStatic = true;
            }
        }

        // Baghdad: a ziggurat rising behind the underworld gate
        static void BuildZiggurat(Transform root, Material mud, Material stone)
        {
            Vector3 c = new Vector3(ArenaCenter.x, ArenaHeight, ArenaCenter.y + 40f);
            float[] w = { 34f, 24f, 14f };
            float y = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "ZigguratTier", root, c + new Vector3(0f, y + 3.5f, 0f), new Vector3(w[i], 7f, w[i]), i == 2 ? stone : mud).isStatic = true;
                y += 7f;
            }
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Stair", root, c + new Vector3(0f, 6f, -w[0] * 0.5f - 4f), new Vector3(6f, 1f, 14f), stone)
                .transform.rotation = Quaternion.Euler(-35f, 0f, 0f);
        }

        static void MakeUnderworldDoor(Material stone, Material glow)
        {
            EnemyAI boss = null;
            foreach (var e in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include)) if (e.isBoss) boss = e;

            var root = new GameObject("Underworld Gate").transform;
            root.position = new Vector3(ArenaCenter.x, ArenaHeight, ArenaCenter.y - 10f);
            var door = new GameObject("Door").transform;
            door.SetParent(root, false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Slab", door, new Vector3(0f, 6f, 0f), new Vector3(9f, 12f, 1.6f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Seal", door, new Vector3(0f, 6.5f, -0.85f), new Vector3(3.2f, 0.08f, 3.2f), glow, keepCollider: false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "KeyHole", door, new Vector3(Mathf.Cos(a) * 2.5f, 6.5f + Mathf.Sin(a) * 2.5f, -0.85f), Vector3.one * 0.45f, glow, keepCollider: false);
            }
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "FrameL", root, new Vector3(-5.5f, 7f, 0f), new Vector3(2f, 14f, 2.4f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "FrameR", root, new Vector3(5.5f, 7f, 0f), new Vector3(2f, 14f, 2.4f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "FrameTop", root, new Vector3(0f, 14.5f, 0f), new Vector3(13f, 2f, 2.6f), stone);

            var ud = root.gameObject.AddComponent<UnderworldDoor>();
            ud.door = door;
            ud.boss = boss != null ? boss.gameObject : null;

            var sys = GameObject.Find("_Game");
            if (sys != null && sys.GetComponent<EndingSequence>() == null) sys.AddComponent<EndingSequence>();
        }

        static void PlaceGates(TerrainData td, Material stone)
        {
            if (!string.IsNullOrEmpty(R.nextScene))
            {
                Vector3 p = new Vector3(ArenaCenter.x, ArenaHeight, ArenaCenter.y + 26f);
                MakeFogGate("Fog Gate - next", p, stone, R.nextScene, R.nextLabel, R.bossId);
            }
            if (!string.IsNullOrEmpty(R.prevScene))
            {
                Vector3 p = OnRoad(td, 32f, 0f, 0f);
                MakeFogGate("Fog Gate - back", p, stone, R.prevScene, R.prevLabel, "");
            }
            if (R.finalRegion)
                MakeUnderworldDoor(stone, RafidainSceneBuilder.Mat("LanternGlow", new Color(1f, 0.7f, 0.35f), 0.3f));
        }

        static void MakeFogGate(string name, Vector3 pos, Material stone, string scene, string label, string requiredBoss)
        {
            var root = new GameObject(name).transform;
            root.position = pos;
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "PillarL", root, new Vector3(-4.5f, 4f, 0f), new Vector3(1.8f, 8f, 1.8f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "PillarR", root, new Vector3(4.5f, 4f, 0f), new Vector3(1.8f, 8f, 1.8f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Lintel", root, new Vector3(0f, 8.6f, 0f), new Vector3(11f, 1.4f, 2f), stone);

            var fogMat = AssetDatabase.LoadAssetAtPath<Material>($"{Gen}/FogWall.mat");
            if (fogMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                fogMat = new Material(sh);
                fogMat.SetFloat("_Surface", 1f);
                fogMat.SetFloat("_Blend", 2f);
                fogMat.SetOverrideTag("RenderType", "Transparent");
                fogMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                fogMat.SetFloat("_DstBlend", (float)BlendMode.One);
                fogMat.SetFloat("_ZWrite", 0f);
                fogMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                fogMat.renderQueue = 3000;
                var c = new Color(0.95f, 0.85f, 0.65f, 0.28f);
                if (fogMat.HasProperty("_BaseColor")) fogMat.SetColor("_BaseColor", c);
                fogMat.color = c;
                AssetDatabase.CreateAsset(fogMat, $"{Gen}/FogWall.mat");
            }
            var fog = RafidainSceneBuilder.Prim(PrimitiveType.Quad, "FogWall", root, new Vector3(0f, 3.9f, 0f), new Vector3(7.2f, 7.8f, 1f), fogMat, keepCollider: false);
            var fogBack = RafidainSceneBuilder.Prim(PrimitiveType.Quad, "FogWallBack", root, new Vector3(0f, 3.9f, 0f), new Vector3(7.2f, 7.8f, 1f), fogMat, keepCollider: false);
            fogBack.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            RafidainSceneBuilder.MakeEmbers(root, new Vector3(0f, 0.2f, 0f), 30f);

            var gate = root.gameObject.AddComponent<RegionGate>();
            gate.targetScene = scene;
            gate.label = label;
            gate.requiredBossId = requiredBoss;
        }

        // =====================================================================
        // Story: map pieces, riddle givers, painted map, region info
        // =====================================================================

        static void PlaceStory(TerrainData td, Material stone)
        {
            var glow = RafidainSceneBuilder.Mat("LanternGlow", new Color(1f, 0.7f, 0.35f), 0.3f);
            var clay = RafidainSceneBuilder.Mat("ClayTablet", new Color(0.62f, 0.45f, 0.28f), 0.2f);
            clay.EnableKeyword("_EMISSION");
            clay.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.25f) * 1.6f);
            var wood = RafidainSceneBuilder.Mat("DarkWood", new Color(0.22f, 0.14f, 0.08f), 0.2f);

            // --- piece 1: a quiet crossing in the old town, far from the main road
            Vector2 origin = TownCenter - new Vector2(TownRadius, TownRadius);
            int cells = Mathf.CeilToInt(TownRadius * 2f / BlockSize);
            Vector2 townSpot = TownCenter;
            float best = -1f;
            for (int i = 0; i <= cells; i++)
            {
                for (int j = 0; j <= cells; j++)
                {
                    var p = origin + new Vector2(i * BlockSize + StreetWidth * 0.5f, j * BlockSize + StreetWidth * 0.5f);
                    if (Vector2.Distance(p, TownCenter) > TownRadius - 15f) continue;
                    float away = Mathf.Abs(p.x - RoadX(p.y));
                    if (away > best) { best = away; townSpot = p; }
                }
            }

            // --- piece 2: high on the eastern slopes, or on the dam lake shore
            Vector2 wildSpot = new Vector2(780f, 520f);
            if (R.pieceAtLake && R.lake)
            {
                for (int k = 0; k < 64; k++)
                {
                    float a = k / 64f * Mathf.PI * 2f;
                    var p = R.lakeCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (R.lakeRadius + 22f);
                    float h = td.GetInterpolatedHeight(p.x / Size, p.y / Size);
                    if (h > WaterLevel + 1.5f && h < WaterLevel + 12f) { wildSpot = p; break; }
                }
            }
            else
            {
                float bestH = 0f;
                for (int k = 0; k < 3000; k++)
                {
                    float x = Random.Range(640f, 900f), z = Random.Range(380f, 820f);
                    float h = td.GetInterpolatedHeight(x / Size, z / Size);
                    float st = td.GetSteepness(x / Size, z / Size);
                    if (st > 24f || h > 110f || h < 45f) continue;
                    if (!IsFreeGround(x, z, 4f)) continue;
                    if (h > bestH) { bestH = h; wildSpot = new Vector2(x, z); }
                }
            }

            var pieces = new[]
            {
                new MapPieceInfo { id = R.id + "_map_1", center = new Vector2(512f, 300f), radius = 380f, seed = R.seed % 97 },
                new MapPieceInfo { id = R.id + "_map_2", center = new Vector2(512f, 760f), radius = 380f, seed = R.seed % 89 + 40 },
            };
            MakePiece(td, pieces[0].id, townSpot, clay);
            MakePiece(td, pieces[1].id, wildSpot, clay);

            // --- sacred oil: one jar per governorate, tucked away on the western slopes
            Vector2 oilSpot = new Vector2(240f, 420f);
            for (int k = 0; k < 2000; k++)
            {
                float x = Random.Range(120f, 380f), z = Random.Range(260f, 760f);
                float h = td.GetInterpolatedHeight(x / Size, z / Size);
                if (h < WaterLevel + 3f || h > 85f || td.GetSteepness(x / Size, z / Size) > 22f) continue;
                if (!IsFreeGround(x, z, 6f)) continue;
                oilSpot = new Vector2(x, z);
                break;
            }
            var oil = new GameObject("Sacred Oil");
            oil.transform.position = Ground(td, oilSpot.x, oilSpot.y, 0.6f);
            var jarMat = RafidainSceneBuilder.Mat("OilJar", new Color(0.55f, 0.38f, 0.2f), 0.5f);
            jarMat.EnableKeyword("_EMISSION");
            jarMat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.3f) * 1.2f);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Jar", oil.transform, Vector3.zero, new Vector3(0.45f, 0.55f, 0.45f), jarMat, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Neck", oil.transform, new Vector3(0f, 0.32f, 0f), new Vector3(0.18f, 0.1f, 0.18f), jarMat, keepCollider: false);
            var ol = new GameObject("Glow").AddComponent<Light>();
            ol.transform.SetParent(oil.transform, false);
            ol.type = LightType.Point; ol.color = new Color(1f, 0.75f, 0.35f); ol.range = 5f; ol.intensity = 2f;
            oil.AddComponent<SacredOil>().oilId = R.id + "_oil";

            // --- a new weapon waits in Mosul (khanjar) and Kirkuk (mace), on the eastern slopes
            string weaponId = R.id == "mosul" ? "khanjar" : R.id == "kirkuk" ? "mace" : null;
            if (weaponId != null)
            {
                Vector2 wSpot = new Vector2(760f, 520f);
                for (int k = 0; k < 2000; k++)
                {
                    float x = Random.Range(640f, 900f), z = Random.Range(300f, 760f);
                    float h = td.GetInterpolatedHeight(x / Size, z / Size);
                    if (h < WaterLevel + 3f || h > 85f || td.GetSteepness(x / Size, z / Size) > 22f) continue;
                    if (!IsFreeGround(x, z, 6f)) continue;
                    wSpot = new Vector2(x, z);
                    break;
                }
                var wp = new GameObject("Weapon - " + weaponId);
                wp.transform.position = Ground(td, wSpot.x, wSpot.y, 0f);
                var metal = RafidainSceneBuilder.Mat(weaponId == "mace" ? "MaceStone" : "KhanjarSteel",
                    weaponId == "mace" ? new Color(0.32f, 0.26f, 0.22f) : new Color(0.78f, 0.74f, 0.66f), 0.7f, 0.8f);
                var vis = new GameObject("Visual").transform;
                vis.SetParent(wp.transform, false);
                vis.localPosition = new Vector3(0f, 0.35f, 0f);
                vis.localRotation = Quaternion.Euler(-70f, 0f, 0f);    // planted in the ground
                float len = weaponId == "mace" ? 1.0f : 0.55f;
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Blade", vis, new Vector3(0f, 0f, len * 0.3f), new Vector3(0.07f, 0.1f, len), metal, keepCollider: false);
                if (weaponId == "mace")
                    RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Head", vis, new Vector3(0f, 0f, len * 0.8f), Vector3.one * 0.3f, metal, keepCollider: false);
                var wl = new GameObject("Glow").AddComponent<Light>();
                wl.transform.SetParent(wp.transform, false);
                wl.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                wl.type = LightType.Point; wl.color = new Color(0.9f, 0.85f, 1f); wl.range = 5f; wl.intensity = 2f;
                wl.shadows = LightShadows.None;
                RafidainSceneBuilder.MakeEmbers(wp.transform, new Vector3(0f, 0.5f, 0f), 6f);
                wp.AddComponent<WeaponPickup>().weaponId = weaponId;
            }

            // --- riddle givers
            // stand on the road edge (the gate is in a narrow canyon, the slopes are too steep)
            Vector3 sageSpot = OnRoad(td, 84f, -4.5f, 0f);
            MakeSage(td, R.sageTown, R.riddleTown, new Vector2(sageSpot.x, sageSpot.z), wood, glow);
            MakeSage(td, R.sageWild, R.riddleWild, PlazaCenter + new Vector2(6f, -9f), wood, glow);

            // --- tutorial tablets on the first stretch of road (Zakho only)
            if (R.id == "zakho") PlaceTutorialTablets(td);

            // --- shafts of light and floating dust in the old town and the guardian's arena
            var shafts = new GameObject("Light Shafts").transform;
            Vector2[] shaftSpots =
            {
                PlazaCenter, PlazaCenter + new Vector2(14f, 9f), PlazaCenter + new Vector2(-12f, 16f),
                TownCenter + new Vector2(-40f, -30f), TownCenter + new Vector2(35f, 45f), ArenaCenter,
            };
            foreach (var sp in shaftSpots) MakeLightShafts(shafts, Ground(td, sp.x, sp.y, 0f), R.mood != null ? R.mood.sun : new Color(1f, 0.64f, 0.4f));

            // --- painted map + region info
            var info = new GameObject("_Region").AddComponent<RegionInfo>();
            info.regionId = R.id;
            info.regionName = R.name;
            info.worldSize = Size;
            info.pieces = pieces;
            info.mapTexture = PaintMap(td);

            var sys = GameObject.Find("_Game");
            if (sys != null && sys.GetComponent<WorldMapUI>() == null) sys.AddComponent<WorldMapUI>();
        }

        static void MakePiece(TerrainData td, string id, Vector2 at, Material clay)
        {
            var root = new GameObject("Map Piece " + id);
            root.transform.position = Ground(td, at.x, at.y, 0f);
            var vis = new GameObject("Tablet").transform;
            vis.SetParent(root.transform, false);
            vis.localPosition = new Vector3(0f, 1.1f, 0f);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Clay", vis, Vector3.zero, new Vector3(0.45f, 0.6f, 0.07f), clay, keepCollider: false);
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(root.transform, false);
            l.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            l.type = LightType.Point;
            l.color = new Color(1f, 0.6f, 0.3f);
            l.range = 6f;
            l.intensity = 2.5f;
            RafidainSceneBuilder.MakeEmbers(root.transform, new Vector3(0f, 0.6f, 0f), 10f);
            root.AddComponent<MapPiece>().pieceId = id;
        }

        /// <summary>Weather that travels with the camera: Duhok drizzle, Kirkuk falling embers, Samarra blowing sand, Baghdad violet ash.</summary>
        static void MakeWeather(Transform cam)
        {
            string kind = R.id;
            if (kind != "duhok" && kind != "kirkuk" && kind != "samarra" && kind != "baghdad") return;

            var go = new GameObject("Weather - " + kind);
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, 6f, 6f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            var vel = ps.velocityOverLifetime;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            switch (kind)
            {
                case "duhok":   // light drizzle off the dam lake
                    main.startLifetime = 1.1f;
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.02f);
                    main.startColor = new Color(0.75f, 0.8f, 0.88f, 0.35f);
                    main.maxParticles = 1500;
                    em.rateOverTime = 900f;
                    shape.scale = new Vector3(30f, 1f, 30f);
                    vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(-1f, -0.6f);
                    vel.y = new ParticleSystem.MinMaxCurve(-14f, -11f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = 0.06f;
                    r.lengthScale = 1f;
                    r.sharedMaterial = RafidainSceneBuilder.ParticleMat("RainStreak", false);
                    break;
                case "kirkuk":  // embers drifting down from the eternal fire
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f, 0.9f), new Color(1f, 0.75f, 0.3f, 0.8f));
                    main.maxParticles = 500;
                    em.rateOverTime = 70f;
                    shape.scale = new Vector3(34f, 2f, 34f);
                    vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
                    vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.6f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    var n = ps.noise; n.enabled = true; n.strength = 0.5f; n.frequency = 0.4f;
                    r.sharedMaterial = RafidainSceneBuilder.ParticleMat("EmberFall", true);
                    break;
                case "samarra": // sand carried on the wind across the plain
                    main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
                    main.startColor = new Color(0.85f, 0.72f, 0.52f, 0.55f);
                    main.maxParticles = 1200;
                    em.rateOverTime = 420f;
                    go.transform.localPosition = new Vector3(-12f, -4.5f, 6f);
                    shape.scale = new Vector3(4f, 4f, 30f);
                    vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(8f, 13f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.4f, 0.6f);
                    vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = 0.04f;
                    r.lengthScale = 1f;
                    r.sharedMaterial = RafidainSceneBuilder.ParticleMat("SandStreak", false);
                    break;
                default:        // Baghdad: violet ash from the gate below
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.55f, 0.9f, 0.6f), new Color(0.45f, 0.35f, 0.6f, 0.5f));
                    main.maxParticles = 700;
                    em.rateOverTime = 80f;
                    shape.scale = new Vector3(36f, 2f, 36f);
                    vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.7f, -0.3f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                    var bn = ps.noise; bn.enabled = true; bn.strength = 0.3f; bn.frequency = 0.25f;
                    r.sharedMaterial = RafidainSceneBuilder.ParticleMat("VioletAsh", true);
                    break;
            }
        }

        static void MakeLightShafts(Transform parent, Vector3 groundPos, Color sun)
        {
            var go = new GameObject("Shafts");
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.up * 7f;

            // soft vertical beams that slowly fade in and out
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(12f, 18f);
            main.startSizeZ = 1f;
            Color beam = Color.Lerp(sun, Color.white, 0.35f); beam.a = 0.07f;
            main.startColor = beam;
            main.maxParticles = 8;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            main.loop = true;
            var em = ps.emission; em.rateOverTime = 0.5f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(8f, 0.5f, 8f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            r.sharedMaterial = RafidainSceneBuilder.ParticleMat("LightShaft", true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            // dust motes drifting inside the light
            var dgo = new GameObject("Dust");
            dgo.transform.SetParent(go.transform, false);
            dgo.transform.localPosition = new Vector3(0f, -4f, 0f);
            var dps = dgo.AddComponent<ParticleSystem>();
            var dm = dps.main;
            dm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            dm.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            dm.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            Color mote = Color.Lerp(sun, Color.white, 0.5f); mote.a = 0.7f;
            dm.startColor = mote;
            dm.gravityModifier = -0.004f;
            dm.maxParticles = 120;
            dm.simulationSpace = ParticleSystemSimulationSpace.World;
            dm.prewarm = true;
            var dem = dps.emission; dem.rateOverTime = 14f;
            var dsh = dps.shape; dsh.shapeType = ParticleSystemShapeType.Box; dsh.scale = new Vector3(7f, 6f, 7f);
            var dn = dps.noise; dn.enabled = true; dn.strength = 0.15f; dn.frequency = 0.25f;
            var dr = dgo.GetComponent<ParticleSystemRenderer>();
            dr.sharedMaterial = RafidainSceneBuilder.ParticleMat("DustMote", true);
            dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static EnemyAI MakeDog(Transform parent, Vector3 pos, Material fur, Material dark)
        {
            var go = new GameObject("Feral Dog");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.0f; cc.radius = 0.35f; cc.center = Vector3.zero;

            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            var torso = RafidainSceneBuilder.Prim(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.42f, 0.36f), fur, keepCollider: false);
            torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var head = new GameObject("Head").transform;
            head.SetParent(body, false);
            head.localPosition = new Vector3(0f, 0.22f, 0.5f);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(0.26f, 0.24f, 0.3f), fur, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Snout", head, new Vector3(0f, -0.04f, 0.2f), new Vector3(0.12f, 0.1f, 0.2f), dark, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "EarL", head, new Vector3(-0.08f, 0.14f, -0.02f), new Vector3(0.05f, 0.12f, 0.04f), dark, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "EarR", head, new Vector3(0.08f, 0.14f, -0.02f), new Vector3(0.05f, 0.12f, 0.04f), dark, keepCollider: false);
            var eyes = RafidainSceneBuilder.Mat("DogEyes", new Color(1f, 0.4f, 0.1f), 0.6f);
            eyes.EnableKeyword("_EMISSION");
            eyes.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.05f) * 2f);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.06f, 0.04f, 0.13f), Vector3.one * 0.04f, eyes, keepCollider: false);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.06f, 0.04f, 0.13f), Vector3.one * 0.04f, eyes, keepCollider: false);

            Transform Leg(string n, float x, float z)
            {
                var hip = new GameObject(n).transform;
                hip.SetParent(body, false);
                hip.localPosition = new Vector3(x, -0.05f, z);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Shin", hip, new Vector3(0f, -0.2f, 0f), new Vector3(0.08f, 0.4f, 0.08f), dark, keepCollider: false);
                return hip;
            }
            var rig = go.AddComponent<DogRig>();
            rig.body = body;
            rig.head = head;
            rig.legFL = Leg("LegFL", -0.12f, 0.3f);
            rig.legFR = Leg("LegFR", 0.12f, 0.3f);
            rig.legBL = Leg("LegBL", -0.12f, -0.3f);
            rig.legBR = Leg("LegBR", 0.12f, -0.3f);
            var tail = new GameObject("Tail").transform;
            tail.SetParent(body, false);
            tail.localPosition = new Vector3(0f, 0.12f, -0.45f);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "TailMesh", tail, new Vector3(0f, 0f, -0.15f), new Vector3(0.05f, 0.05f, 0.32f), fur, keepCollider: false);
            rig.tail = tail;

            var ai = go.AddComponent<EnemyAI>();
            ai.displayName = "كلب سائب";
            ai.maxHealth = 38f;
            ai.moveSpeed = 6.2f;
            ai.turnSpeed = 10f;
            ai.detectRange = 16f;
            ai.leashRange = 34f;
            ai.attackRange = 1.9f;
            ai.attackReach = 2.1f;
            ai.attackAngle = 55f;
            ai.damage = 11f;
            ai.windup = 0.38f;
            ai.activeTime = 0.14f;
            ai.recovery = 0.75f;
            ai.lungeSpeed = 9f;
            ai.poise = 18f;
            ai.staggerTime = 1.1f;
            ai.emberReward = 35;
            return ai;
        }

        static void PlaceTutorialTablets(TerrainData td)
        {
            var clay = RafidainSceneBuilder.Mat("MessageClay", new Color(0.62f, 0.48f, 0.32f), 0.15f);
            clay.EnableKeyword("_EMISSION");
            clay.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.25f) * 0.6f);
            var root = new GameObject("Tutorial Tablets").transform;
            (float z, float side, string body)[] msgs =
            {
                (60f, -4f, "امش بهدوء يا فرات... اركض اذا ضغطت Shift\nلكن الركض يأكل من صبرك (الطاقة)"),
                (74f, -4f, "الشعلة بيت المسافر\nاسترح عندها: ترجع صحتك وقواريرك... ويرجع اعداؤك ايضا"),
                (104f, 4f, "اذا رأيت الضربة قادمة فتدحرج (Space)\nفي لحظة التدحرج لا يمسك شيء"),
                (128f, -4f, "ضربة خفيفة (كلك يسار) سريعة\nوالثقيلة (F) تكسر توازن العدو... لكنها بطيئة"),
                (156f, 4f, "ارفع درعك (كلك يمين) لتصد\nوان رفعته في اخر لحظة قبل الضربة... ترنح عدوك فاعدمه"),
                (186f, -4f, "ثبت عينك على عدوك (Q)\nوحرك الماوس يمينا او يسارا لتنتقل الى غيره"),
                (214f, 4f, "القارورة (R) تشفي جراحك\nلكنها لا تكفي الا قليلا... فاشرب حين تبتعد"),
                (250f, -4f, "اذا هلكت سقط جمرك مكانك\nارجع اليه قبل ان تهلك ثانية... والا ضاع الى الابد"),
                (300f, 4f, "قطع الخريطة مخبأة بعيدا عن الطريق\nاضغط M لترى ما كشفت من الارض"),
            };
            foreach (var m in msgs)
            {
                var go = new GameObject("Tablet");
                go.transform.SetParent(root, false);
                go.transform.position = OnRoad(td, m.z, m.side, 0.05f);
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Clay", go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.55f, 0.07f, 0.4f), clay, keepCollider: false);
                var l = new GameObject("Glow").AddComponent<Light>();
                l.transform.SetParent(go.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                l.type = LightType.Point;
                l.color = new Color(1f, 0.6f, 0.3f);
                l.range = 3.5f;
                l.intensity = 1.6f;
                l.shadows = LightShadows.None;
                var gm = go.AddComponent<GroundMessage>();
                gm.body = m.body;
                gm.glow = l;
            }
        }

        static void MakeSage(TerrainData td, string speaker, string[] lines, Vector2 at, Material wood, Material glow)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "Sage - " + speaker;
            npc.transform.position = Ground(td, at.x, at.y, 1.0f);
            RafidainSceneBuilder.BuildHumanoid(npc, 1);
            var helm = npc.transform.Find("Body/Helm");
            if (helm != null) Object.DestroyImmediate(helm.gameObject);
            var robe = RafidainSceneBuilder.Mat("Sage_Robe", new Color(0.30f, 0.26f, 0.20f), 0.05f);
            RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Keffiyeh", npc.transform.Find("Body"), new Vector3(0f, 0.92f, -0.02f), new Vector3(0.3f, 0.3f, 0.32f), robe, keepCollider: false);
            var sage = npc.AddComponent<RiddleNPC>();
            sage.speaker = speaker;
            sage.lines = lines;
            MakeLantern(npc.transform.parent, Ground(td, at.x + 1.6f, at.y, 0f), wood, glow);
        }

        static Texture2D PaintMap(TerrainData td)
        {
            const int S = 512;
            string path = $"{Gen}/{R.id}_map.png";
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
            Vector3 light = new Vector3(-0.5f, 0.8f, 0.4f).normalized;
            Color sand = new Color(0.70f, 0.60f, 0.44f), green = new Color(0.47f, 0.48f, 0.30f);
            Color rock = new Color(0.55f, 0.50f, 0.44f), snow = new Color(0.82f, 0.79f, 0.72f);
            Color water = new Color(0.22f, 0.32f, 0.38f), road = new Color(0.33f, 0.25f, 0.17f), town = new Color(0.78f, 0.70f, 0.55f);
            Color ink = new Color(0.62f, 0.12f, 0.08f);
            Vector2 townOrigin = TownCenter - new Vector2(TownRadius, TownRadius);

            for (int y = 0; y < S; y++)
            {
                float v = (y + 0.5f) / S, z = v * Size;
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, wx = u * Size;
                    float h = td.GetInterpolatedHeight(u, v);
                    Vector3 n = td.GetInterpolatedNormal(u, v);
                    float shade = Mathf.Clamp01(Vector3.Dot(n, light)) * 0.7f + 0.45f;

                    Color c = h < 40f ? Color.Lerp(green, sand, Mathf.InverseLerp(14f, 40f, h))
                            : h < 110f ? Color.Lerp(sand, rock, Mathf.InverseLerp(40f, 110f, h))
                            : Color.Lerp(rock, snow, Mathf.InverseLerp(110f, 200f, h));
                    c *= shade;

                    if (h < WaterLevel + 0.4f) c = water * (0.85f + 0.15f * shade);
                    if (z > 25f && z < 970f && Mathf.Abs(wx - RoadX(z)) < 4.5f && Mathf.Abs(z - RiverZ(wx)) > 30f) c = road;

                    float dTown = Vector2.Distance(new Vector2(wx, z), TownCenter);
                    if (dTown < TownRadius)
                    {
                        float lx = Mathf.Repeat(wx - townOrigin.x, BlockSize), lz = Mathf.Repeat(z - townOrigin.y, BlockSize);
                        c = (lx < StreetWidth || lz < StreetWidth) ? road * 1.2f : town * shade;
                    }
                    if (Mathf.Abs(Vector2.Distance(new Vector2(wx, z), ArenaCenter) - 17f) < 2.5f) c = ink;

                    // old paper tint
                    c = Color.Lerp(c, new Color(0.74f, 0.64f, 0.47f), 0.22f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }


        // =====================================================================
        // Noise helpers
        // =====================================================================

        static float Smooth(float from, float to, float v)
        {
            float t = Mathf.InverseLerp(from, to, v);
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float x, float z, int octaves, float freq)
        {
            float amp = 1f, sum = 0f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq + 137f, z * freq + 911f);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / norm;
        }

        static float Ridged(float x, float z, int octaves, float freq)
        {
            float amp = 1f, sum = 0f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Mathf.Abs(Mathf.PerlinNoise(x * freq + 53f, z * freq + 371f) * 2f - 1f);
                sum += amp * n * n;
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / norm;
        }
    }
}
