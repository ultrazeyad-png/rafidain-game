using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Menu: Rafidain > Build Zakho Test Arena
    /// Builds a complete playable test scene in one click: terrain blockout of Zakho's mountains,
    /// Furat, enemies, the Guardian of Zakho boss, two flames, realistic lighting and post-processing.
    /// </summary>
    public static class RafidainSceneBuilder
    {
        const string GenFolder = "Assets/Rafidain/Generated";
        const string SceneFolder = "Assets/Rafidain/Scenes";
        const string ScenePath = SceneFolder + "/Zakho_Test.unity";

        [MenuItem("Rafidain/Build Zakho Test Arena")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RafidainURPSetup.EnsureURP();
            EnsureFolder(GenFolder);
            EnsureFolder(SceneFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ---------- Materials ----------
            var groundMat = Mat("Ground", new Color(0.34f, 0.30f, 0.24f), 0.12f);
            var grassMat = Mat("DryGrass", new Color(0.36f, 0.38f, 0.22f), 0.08f);
            var rockMat = Mat("Rock", new Color(0.42f, 0.40f, 0.37f), 0.22f);
            var stoneMat = Mat("AncientStone", new Color(0.62f, 0.55f, 0.44f), 0.25f);
            var furatMat = Mat("Furat", new Color(0.22f, 0.20f, 0.19f), 0.35f);
            var hollowMat = Mat("Hollow", new Color(0.33f, 0.29f, 0.26f), 0.1f);
            var guardianMat = Mat("Guardian", new Color(0.32f, 0.24f, 0.14f), 0.55f, 0.6f);
            var steelMat = Mat("Steel", new Color(0.72f, 0.72f, 0.74f), 0.85f, 1f);
            var emberMat = Mat("Ember", new Color(1f, 0.45f, 0.1f), 0.5f);
            emberMat.EnableKeyword("_EMISSION");
            emberMat.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 5f);
            EditorUtility.SetDirty(emberMat);

            // ---------- Lighting & atmosphere ----------
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.name = "Sun";
                sun.color = new Color(1f, 0.84f, 0.66f);
                sun.intensity = 1.8f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(24f, -40f, 0f);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.66f, 0.62f, 0.56f);
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.40f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.15f, 0.12f);

            // ---------- World blockout (Zakho: valley between mountains) ----------
            var world = new GameObject("World").transform;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(world);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            ground.isStatic = true;

            Random.InitState(7);

            // Mountain ring
            for (int i = 0; i < 30; i++)
            {
                float a = i / 30f * Mathf.PI * 2f;
                float r = Random.Range(75f, 120f);
                var m = Prim(PrimitiveType.Sphere, "Mountain", world, new Vector3(Mathf.Cos(a) * r, -6f, Mathf.Sin(a) * r),
                    new Vector3(Random.Range(35f, 65f), Random.Range(25f, 55f), Random.Range(35f, 65f)), rockMat);
                m.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            // Scattered rocks and grass patches
            for (int i = 0; i < 70; i++)
            {
                Vector2 p = Random.insideUnitCircle * 65f;
                if (p.magnitude < 9f || Mathf.Abs(p.x) < 4f) continue; // keep the main path clear
                float s = Random.Range(0.8f, 4f);
                var rock = Prim(PrimitiveType.Cube, "Rock", world, new Vector3(p.x, s * 0.3f, p.y), new Vector3(s, s * Random.Range(0.5f, 1.2f), s * Random.Range(0.7f, 1.3f)), rockMat);
                rock.transform.rotation = Random.rotation;
            }
            for (int i = 0; i < 40; i++)
            {
                Vector2 p = Random.insideUnitCircle * 60f;
                Prim(PrimitiveType.Cylinder, "GrassPatch", world, new Vector3(p.x, 0.02f, p.y), new Vector3(Random.Range(3f, 8f), 0.02f, Random.Range(3f, 8f)), grassMat);
            }

            // Ruined path pillars leading north to the boss arena
            for (int i = 0; i < 6; i++)
            {
                float z = 4f + i * 7f;
                float h = Random.Range(2.5f, 5.5f);
                Prim(PrimitiveType.Cylinder, "PathPillar", world, new Vector3(-5f, h * 0.5f, z), new Vector3(0.9f, h * 0.5f, 0.9f), stoneMat);
                h = Random.Range(1.5f, 5.5f);
                Prim(PrimitiveType.Cylinder, "PathPillar", world, new Vector3(5f, h * 0.5f, z), new Vector3(0.9f, h * 0.5f, 0.9f), stoneMat);
            }

            // Boss arena: ring of broken Assyrian-style columns
            Vector3 arena = new Vector3(0f, 0f, 58f);
            var floor = Prim(PrimitiveType.Cylinder, "ArenaFloor", world, arena + Vector3.up * 0.05f, new Vector3(30f, 0.05f, 30f), stoneMat);
            floor.isStatic = true;
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                if (i == 9) continue; // entrance gap (south side)
                float h = (i % 3 == 0) ? Random.Range(2f, 4f) : 8f;
                Prim(PrimitiveType.Cylinder, "ArenaColumn", world, arena + new Vector3(Mathf.Cos(a) * 15f, h * 0.5f, Mathf.Sin(a) * 15f), new Vector3(1.4f, h * 0.5f, 1.4f), stoneMat);
            }

            // ---------- Player: Furat ----------
            var furat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            furat.name = "Furat";
            furat.tag = "Player";
            Object.DestroyImmediate(furat.GetComponent<CapsuleCollider>());
            furat.transform.position = new Vector3(0f, 1.05f, -8f);
            furat.GetComponent<Renderer>().sharedMaterial = furatMat;
            var cc = furat.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero;
            furat.AddComponent<PlayerStats>();
            furat.AddComponent<PlayerController>();
            var combat = furat.AddComponent<PlayerCombat>();
            combat.weaponPivot = MakeWeapon(furat.transform, steelMat, 1.1f);
            // Hood / face direction marker
            BuildHumanoid(furat, 0);

            // ---------- Camera ----------
            var cam = Camera.main;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            var rig = cam.gameObject.AddComponent<ThirdPersonCamera>();
            rig.target = furat.transform;
            cam.transform.position = furat.transform.position + new Vector3(0f, 2f, -4f);
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;

            // ---------- Post-processing (the realistic look) ----------
            CreatePostVolume();

            // ---------- Game systems ----------
            var sys = new GameObject("_Game");
            sys.AddComponent<PlayerInputReader>();
            var gm = sys.AddComponent<GameManager>();
            var hud = sys.AddComponent<HUD>();
            sys.AddComponent<MobileControls>();
            sys.AddComponent<IntroSequence>();
            gm.player = furat.GetComponent<PlayerStats>();
            hud.player = gm.player;

            // ---------- Flames (bonfires) ----------
            var gate = MakeBonfire("zakho_gate", "Zakho - Old Gate", new Vector3(2.5f, 0f, -10f), emberMat, stoneMat);
            MakeBonfire("zakho_ruins", "Zakho - Ruined Path", new Vector3(3f, 0f, 40f), emberMat, stoneMat);
            gm.defaultBonfire = gate;

            // ---------- Enemies ----------
            var enemies = new GameObject("Enemies").transform;
            MakeEnemy(enemies, "Hollow Soldier", new Vector3(3f, 1.05f, 6f), hollowMat, steelMat, 1f);
            MakeEnemy(enemies, "Hollow Soldier", new Vector3(-3f, 1.05f, 13f), hollowMat, steelMat, 1f);
            MakeEnemy(enemies, "Hollow Soldier", new Vector3(2f, 1.05f, 22f), hollowMat, steelMat, 1f);
            var brute = MakeEnemy(enemies, "Mountain Brute", new Vector3(-2f, 1.4f, 31f), hollowMat, steelMat, 1.3f, 3);
            brute.maxHealth = 160f; brute.damage = 30f; brute.windup = 1.0f; brute.moveSpeed = 2.2f; brute.emberReward = 150;

            // Boss: Guardian of Zakho (humanoid warrior lord, first of the five guardians)
            var boss = MakeEnemy(enemies, "حارس زاخو", arena + new Vector3(0f, 1.6f, 6f), guardianMat, steelMat, 1.6f, 2);
            boss.isBoss = true;
            boss.bossId = "boss_zakho";
            boss.maxHealth = 650f;
            boss.damage = 32f;
            boss.windup = 0.9f;
            boss.recovery = 1.1f;
            boss.attackRange = 2.4f;
            boss.attackReach = 2.4f;
            boss.moveSpeed = 3.2f;
            boss.detectRange = 13f;
            boss.leashRange = 28f;
            boss.emberReward = 1500;
            boss.canLeap = true;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // ---------- Save scene ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Rafidain",
                "Zakho test arena is ready.\n\nPress Play.\n\nPC: WASD move, Mouse look, Left click attack, Right click heavy, Space roll, Shift run, Q lock-on, R heal, E use, Esc cursor.",
                "OK");
        }

        [MenuItem("Rafidain/Delete Save File")]
        public static void DeleteSave()
        {
            SaveSystem.DeleteSave();
            Debug.Log("[Rafidain] Save deleted.");
        }

        // ---------- helpers ----------

        internal static Transform MakeWeapon(Transform owner, Material mat, float length)
        {
            var pivot = new GameObject("WeaponPivot").transform;
            pivot.SetParent(owner, false);
            pivot.localPosition = new Vector3(0.45f, 0.15f, 0.1f);
            Prim(PrimitiveType.Cube, "Blade", pivot, new Vector3(0f, 0f, length * 0.5f), new Vector3(0.07f, 0.12f, length), mat, keepCollider: false);
            return pivot;
        }

        internal static EnemyAI MakeEnemy(Transform parent, string name, Vector3 pos, Material body, Material weapon, float scale, int style = 1)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent);
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<Renderer>().sharedMaterial = body;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.45f; cc.center = Vector3.zero;
            var ai = go.AddComponent<EnemyAI>();
            ai.displayName = name;
            var pivot = MakeWeapon(go.transform, weapon, 1.2f);
            pivot.localRotation = Quaternion.Euler(30f, 0f, 0f);
            Prim(PrimitiveType.Cube, "Visor", go.transform, new Vector3(0f, 0.55f, 0.4f), new Vector3(0.35f, 0.1f, 0.1f), weapon, keepCollider: false);
            BuildHumanoid(go, style);
            return ai;
        }

        internal static Bonfire MakeBonfire(string id, string display, Vector3 pos, Material ember, Material stone)
        {
            var root = new GameObject("Flame - " + display);
            root.transform.position = pos;
            Prim(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.2f, 0.1f, 1.2f), stone);
            Prim(PrimitiveType.Sphere, "Coals", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.35f, 0.6f), ember, keepCollider: false);
            var sword = Prim(PrimitiveType.Cube, "PlantedSword", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.06f, 1.4f, 0.14f), stone, keepCollider: false);
            sword.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = 10f;
            light.intensity = 4f;
            light.shadows = LightShadows.Soft;

            var spawn = new GameObject("SpawnPoint").transform;
            spawn.SetParent(root.transform, false);
            spawn.localPosition = new Vector3(0f, 1.05f, -1.8f);

            MakeEmbers(root.transform, new Vector3(0f, 0.35f, 0f), 22f);

            var b = root.AddComponent<Bonfire>();
            b.bonfireId = id;
            b.displayName = display;
            b.fireLight = light;
            b.spawnPoint = spawn;
            return b;
        }

        internal static void CreatePostVolume()
        {
            string path = GenFolder + "/RafidainPost.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(1.0f);
            bloom.scatter.Override(0.65f);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(18f);
            color.saturation.Override(-18f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.45f);

            var white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(8f);

            // cool shadows, warm highlights: the classic dark-fantasy split tone
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.92f, 0.97f, 1.08f, -0.04f));
            smh.highlights.Override(new Vector4(1.06f, 1.0f, 0.92f, 0.02f));

            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.06f);

            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.12f);

            foreach (var c in profile.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volGo = new GameObject("Post Process Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        // ---------- procedural humanoid bodies ----------
        // style: 0 = Furat (hooded hunter, long coat, cape, mask)
        //        1 = hollow soldier (ragged, dented helmet)
        //        2 = guardian lord (bronze armour, pauldrons, horned crown, long cape)
        //        3 = brute (huge, bare-armed, crude helm)
        internal static HumanoidRig BuildHumanoid(GameObject owner, int style)
        {
            Material coat, cloth, skin, dark, metal;
            switch (style)
            {
                case 0:
                    coat = Mat("Hunter_Coat", new Color(0.11f, 0.10f, 0.10f), 0.25f);
                    cloth = Mat("Hunter_Cloth", new Color(0.20f, 0.17f, 0.14f), 0.15f);
                    skin = Mat("Skin", new Color(0.55f, 0.41f, 0.32f), 0.3f);
                    dark = Mat("Leather_Dark", new Color(0.06f, 0.05f, 0.045f), 0.35f);
                    metal = Mat("Steel", new Color(0.72f, 0.72f, 0.74f), 0.85f, 1f);
                    break;
                case 2:
                    coat = Mat("Guardian_Bronze", new Color(0.36f, 0.24f, 0.11f), 0.65f, 0.85f);
                    cloth = Mat("Guardian_Cape", new Color(0.28f, 0.05f, 0.04f), 0.2f);
                    skin = Mat("Guardian_Skin", new Color(0.30f, 0.24f, 0.20f), 0.3f);
                    dark = Mat("Leather_Dark", new Color(0.06f, 0.05f, 0.045f), 0.35f);
                    metal = Mat("Guardian_Bronze", new Color(0.36f, 0.24f, 0.11f), 0.65f, 0.85f);
                    break;
                case 3:
                    coat = Mat("Brute_Hide", new Color(0.24f, 0.19f, 0.15f), 0.15f);
                    cloth = Mat("Hollow_Rags", new Color(0.26f, 0.23f, 0.19f), 0.05f);
                    skin = Mat("Hollow_Skin", new Color(0.45f, 0.42f, 0.36f), 0.25f);
                    dark = Mat("Leather_Dark", new Color(0.06f, 0.05f, 0.045f), 0.35f);
                    metal = Mat("Rusted_Iron", new Color(0.30f, 0.22f, 0.17f), 0.4f, 0.7f);
                    break;
                default:
                    coat = Mat("Hollow_Rags", new Color(0.26f, 0.23f, 0.19f), 0.05f);
                    cloth = Mat("Hollow_Cloth", new Color(0.20f, 0.18f, 0.15f), 0.05f);
                    skin = Mat("Hollow_Skin", new Color(0.45f, 0.42f, 0.36f), 0.25f);
                    dark = Mat("Leather_Dark", new Color(0.06f, 0.05f, 0.045f), 0.35f);
                    metal = Mat("Rusted_Iron", new Color(0.30f, 0.22f, 0.17f), 0.4f, 0.7f);
                    break;
            }

            var root = owner.transform;
            var capsule = owner.GetComponent<MeshRenderer>();
            if (capsule != null) capsule.enabled = false;
            var visor = root.Find("Visor");
            if (visor != null) Object.DestroyImmediate(visor.gameObject);

            float bulk = style == 3 ? 1.3f : style == 2 ? 1.12f : 1f;
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);

            // legs
            Transform Leg(float x)
            {
                var pivot = new GameObject(x < 0 ? "LegL" : "LegR").transform;
                pivot.SetParent(body, false);
                pivot.localPosition = new Vector3(x * bulk, -0.08f, 0f);
                Prim(PrimitiveType.Capsule, "Leg", pivot, new Vector3(0f, -0.44f, 0f), new Vector3(0.19f * bulk, 0.44f, 0.19f * bulk), cloth, keepCollider: false);
                Prim(PrimitiveType.Cube, "Boot", pivot, new Vector3(0f, -0.86f, 0.04f), new Vector3(0.17f * bulk, 0.15f, 0.3f), dark, keepCollider: false);
                return pivot;
            }
            var legL = Leg(-0.12f);
            var legR = Leg(0.12f);

            // torso, belt, coat skirt
            Prim(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.33f, 0f), new Vector3(0.46f * bulk, 0.36f, 0.30f * bulk), coat, keepCollider: false);
            Prim(PrimitiveType.Cylinder, "Belt", body, new Vector3(0f, 0.02f, 0f), new Vector3(0.47f * bulk, 0.035f, 0.33f * bulk), dark, keepCollider: false);
            if (style == 0 || style == 2)
                Prim(PrimitiveType.Cylinder, "CoatSkirt", body, new Vector3(0f, -0.28f, 0f), new Vector3(0.5f * bulk, 0.3f, 0.38f * bulk), style == 0 ? coat : cloth, keepCollider: false);
            else
                Prim(PrimitiveType.Cylinder, "Rags", body, new Vector3(0f, -0.16f, 0f), new Vector3(0.48f * bulk, 0.16f, 0.36f * bulk), cloth, keepCollider: false);

            // arms
            Transform Arm(float x)
            {
                var pivot = new GameObject(x < 0 ? "ArmL" : "ArmR").transform;
                pivot.SetParent(body, false);
                pivot.localPosition = new Vector3(x * bulk, 0.6f, 0f);
                Prim(PrimitiveType.Capsule, "Arm", pivot, new Vector3(0f, -0.32f, 0f), new Vector3(0.13f * bulk, 0.33f, 0.13f * bulk), style == 3 ? skin : coat, keepCollider: false);
                Prim(PrimitiveType.Sphere, "Hand", pivot, new Vector3(0f, -0.66f, 0f), Vector3.one * 0.11f * bulk, style == 0 ? dark : skin, keepCollider: false);
                if (style == 2)
                    Prim(PrimitiveType.Sphere, "Pauldron", pivot, new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.24f, 0.34f), metal, keepCollider: false);
                return pivot;
            }
            var armL = Arm(-0.29f);
            var armR = Arm(0.29f);

            // head
            Prim(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 0.88f, 0.02f), Vector3.one * 0.23f, skin, keepCollider: false);
            switch (style)
            {
                case 0: // hood + cloth mask like a Yharnam hunter
                    Prim(PrimitiveType.Sphere, "Hood", body, new Vector3(0f, 0.91f, -0.03f), new Vector3(0.31f, 0.33f, 0.33f), coat, keepCollider: false);
                    Prim(PrimitiveType.Cube, "Mask", body, new Vector3(0f, 0.84f, 0.12f), new Vector3(0.2f, 0.1f, 0.06f), dark, keepCollider: false);
                    Prim(PrimitiveType.Cube, "Scarf", body, new Vector3(0f, 0.7f, 0.05f), new Vector3(0.36f, 0.1f, 0.3f), cloth, keepCollider: false);
                    break;
                case 2: // horned crown helm
                    Prim(PrimitiveType.Sphere, "Helm", body, new Vector3(0f, 0.92f, 0f), new Vector3(0.28f, 0.27f, 0.28f), metal, keepCollider: false);
                    Prim(PrimitiveType.Cylinder, "Crown", body, new Vector3(0f, 1.05f, 0f), new Vector3(0.26f, 0.05f, 0.26f), metal, keepCollider: false);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var horn = Prim(PrimitiveType.Capsule, "Horn", body, new Vector3(s * 0.17f, 1.08f, 0f), new Vector3(0.05f, 0.16f, 0.05f), dark, keepCollider: false);
                        horn.transform.localRotation = Quaternion.Euler(0f, 0f, -s * 35f);
                    }
                    break;
                default: // crude dented helmet
                    Prim(PrimitiveType.Sphere, "Helm", body, new Vector3(0f, 0.93f, 0f), new Vector3(0.26f, 0.19f, 0.26f), metal, keepCollider: false);
                    break;
            }

            // cape
            Transform capePivot = null;
            if (style != 3)
            {
                capePivot = new GameObject("CapePivot").transform;
                capePivot.SetParent(body, false);
                capePivot.localPosition = new Vector3(0f, 0.64f, -0.17f * bulk);
                float len = style == 1 ? 0.6f : 1.05f;
                Prim(PrimitiveType.Cube, "Cape", capePivot, new Vector3(0f, -len * 0.5f, 0f), new Vector3(0.5f * bulk, len, 0.03f), style == 0 ? coat : cloth, keepCollider: false);
            }

            // weapon into the right hand
            var weapon = root.Find("WeaponPivot");
            if (weapon != null)
            {
                weapon.SetParent(armR, false);
                weapon.localPosition = new Vector3(0f, -0.66f, 0.05f);
                weapon.localRotation = Quaternion.identity;
            }

            var rig = owner.GetComponent<HumanoidRig>();
            if (rig == null) rig = owner.AddComponent<HumanoidRig>();
            rig.body = body;
            rig.legL = legL; rig.legR = legR;
            rig.armL = armL; rig.armR = armR;
            rig.cape = capePivot;
            return rig;
        }

        // ---------- particles ----------

        internal static Material ParticleMat(string name, bool additive)
        {
            string path = $"{GenFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            var m = new Material(sh);
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
            }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Rising glowing embers (bonfires, braziers, lanterns).</summary>
        internal static ParticleSystem MakeEmbers(Transform parent, Vector3 localPos, float rate)
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.8f, 0.35f));
            main.gravityModifier = -0.12f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.35f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.6f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat("EmberParticle", true);
            return ps;
        }

        /// <summary>Ash and dust drifting in the air around the camera (Bloodborne mood).</summary>
        internal static ParticleSystem MakeAsh(Transform cam)
        {
            var go = new GameObject("Ash In The Air");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, 0f, 8f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.82f, 0.78f, 0.55f), new Color(0.6f, 0.55f, 0.5f, 0.35f));
            main.gravityModifier = 0.01f;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 60f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 14f, 36f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.2f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat("AshParticle", false);
            return ps;
        }

        internal static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
        {
            string path = $"{GenFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        internal static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
