using System.IO;
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
        const string ScenePath = "Assets/Rafidain/Scenes/Zakho_Region.unity";

        const float Size = 1024f;
        const float MaxH = 260f;
        const float WaterLevel = 14f;
        const int HeightRes = 513;
        const int AlphaRes = 512;

        const float TownRadius = 150f;
        const float BlockSize = 40f;
        const float StreetWidth = 9f;

        static readonly string[] LayerNames = { "Grass", "Dirt", "Rock", "Road", "Mud" };

        static float RoadX(float z) => 512f + 70f * Mathf.Sin(z * 0.006f) + 25f * Mathf.Sin(z * 0.017f);
        static float RoadTarget(float z) => 22f + 5f * Mathf.Sin(z * 0.004f);
        static float RiverZ(float x) => 620f + 35f * Mathf.Sin(x * 0.009f) + 12f * Mathf.Sin(x * 0.031f);

        static Vector2 TownCenter => new Vector2(RoadX(330f), 330f);
        static float TownHeight => RoadTarget(330f);
        static Vector2 ArenaCenter => new Vector2(RoadX(905f), 905f);
        static float ArenaHeight => RoadTarget(905f) + 4f;

        [MenuItem("Rafidain/Build Zakho Region (Big Map)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RafidainURPSetup.EnsureURP();
            RafidainURPSetup.TuneForOpenWorld();
            RafidainSceneBuilder.EnsureFolder(Gen);
            RafidainSceneBuilder.EnsureFolder("Assets/Rafidain/Scenes");
            foreach (var n in LayerNames) RafidainSceneBuilder.EnsureFolder($"{ImportRoot}/{n}");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Random.InitState(1985);

            try
            {
                EditorUtility.DisplayProgressBar("Rafidain", "Shaping terrain…", 0.1f);
                var terrainData = BuildTerrainData();
                var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
                terrainGo.name = "Zakho Terrain";
                var terrain = terrainGo.GetComponent<Terrain>();
                terrain.heightmapPixelError = 3f;
                terrain.basemapDistance = 500f;
                terrain.drawInstanced = true;
                var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (terrainShader != null)
                {
                    var tm = new Material(terrainShader);
                    AssetDatabase.CreateAsset(tm, $"{Gen}/TerrainMat.mat");
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
                var ember = RafidainSceneBuilder.Mat("Ember", new Color(1f, 0.45f, 0.1f), 0.5f);
                ember.EnableKeyword("_EMISSION");
                ember.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 5f);

                // ---------- Water ----------
                var waterGo = RafidainSceneBuilder.Prim(PrimitiveType.Plane, "Khabur River", null,
                    new Vector3(Size * 0.5f, WaterLevel, Size * 0.5f), new Vector3(Size / 10f, 1f, Size / 10f), water, keepCollider: false);
                waterGo.isStatic = true;

                EditorUtility.DisplayProgressBar("Rafidain", "Building Zakho old town…", 0.6f);
                var world = new GameObject("World").transform;
                BuildTown(world, terrainData, mud1, mud2, limestone, roofMat, stone);
                BuildBridge(world, stone);
                BuildArena(world, stone);

                EditorUtility.DisplayProgressBar("Rafidain", "Planting oak forest and rocks…", 0.75f);
                ScatterNature(world, terrainData, bark, leaves, rockMat);

                EditorUtility.DisplayProgressBar("Rafidain", "Placing Furat, enemies and flames…", 0.9f);
                PlaceGameplay(terrainData, sun, furatMat, hollowMat, guardianMat, steel, ember, stone);

                RafidainSceneBuilder.CreatePostVolume();

                EditorSceneManager.SaveScene(scene, ScenePath);
                RafidainSceneBuilder.AddSceneToBuild(ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("Rafidain",
                "Zakho region is ready (1 km x 1 km).\n\nStart at the southern gate, follow the road north through the old town, cross the Khabur bridge, and climb to the Guardian's arena.\n\nPress Play.",
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
            AssetDatabase.CreateAsset(td, $"{Gen}/ZakhoTerrain.asset");
            return td;
        }

        static float HeightAt(float x, float z)
        {
            float baseH = 20f + (Fbm(x, z, 4, 0.004f) - 0.5f) * 18f;

            float dEdge = Mathf.Min(Mathf.Min(x, Size - x), Mathf.Min(z, Size - z));
            float m = Smooth(230f, 10f, dEdge);
            float mountains = m * (90f + Ridged(x, z, 5, 0.006f) * 150f);

            float hills = Mathf.Max(0f, Fbm(x + 500f, z + 500f, 3, 0.008f) - 0.55f) * 120f;
            float h = baseH + mountains + hills;

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
            float[] tile = { 7f, 6f, 10f, 4f, 6f };

            for (int i = 0; i < layers.Length; i++)
            {
                FindImported(LayerNames[i], out var diffuse, out var normal);
                if (diffuse == null) diffuse = ProceduralTexture(LayerNames[i], palette[i, 0], palette[i, 1], i == 3 ? 0.08f : 0.03f, i == 2 ? 0.25f : 0.12f);

                var layer = new TerrainLayer
                {
                    diffuseTexture = diffuse,
                    normalMapTexture = normal,
                    tileSize = new Vector2(tile[i], tile[i]),
                    smoothness = i == 4 ? 0.35f : 0.05f
                };
                string path = $"{Gen}/Layer_{LayerNames[i]}.terrainlayer";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(layer, path);
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
            EditorUtility.SetDirty(td);
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

        static Texture2D ProceduralTexture(string name, Color a, Color b, float freq, float speckle)
        {
            string path = $"{Gen}/{name}_proc.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int S = 512;
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
            var rng = new System.Random(name.GetHashCode());
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    // tileable noise: blend four offset samples
                    float n = TexNoise(x, y, freq) * (1 - u) * (1 - v)
                            + TexNoise(x - S, y, freq) * u * (1 - v)
                            + TexNoise(x, y - S, freq) * (1 - u) * v
                            + TexNoise(x - S, y - S, freq) * u * v;
                    float s = 1f - speckle * (float)rng.NextDouble();
                    tex.SetPixel(x, y, Color.Lerp(a, b, n) * s);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float TexNoise(float x, float y, float f)
        {
            return Mathf.PerlinNoise(x * f + 1000f, y * f + 1000f) * 0.6f
                 + Mathf.PerlinNoise(x * f * 4f + 2000f, y * f * 4f + 2000f) * 0.4f;
        }

        // =====================================================================
        // Atmosphere
        // =====================================================================

        static Light SetupAtmosphere()
        {
            var sun = Object.FindAnyObjectByType<Light>();
            sun.name = "Sun";
            sun.color = new Color(1f, 0.82f, 0.62f);
            sun.intensity = 2.0f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(18f, -30f, 0f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 1.15f);
                sky.SetColor("_SkyTint", new Color(0.52f, 0.53f, 0.58f));
                sky.SetColor("_GroundColor", new Color(0.35f, 0.32f, 0.28f));
                sky.SetFloat("_Exposure", 1.15f);
                AssetDatabase.CreateAsset(sky, $"{Gen}/ZakhoSky.mat");
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.68f, 0.66f, 0.62f);
            RenderSettings.fogDensity = 0.0028f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.64f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.41f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.14f, 0.11f);
            return sun;
        }

        // =====================================================================
        // Old town, bridge, arena
        // =====================================================================

        static void BuildTown(Transform world, TerrainData td, Material mudA, Material mudB, Material lime, Material roof, Material stone)
        {
            var town = new GameObject("Zakho Old Town").transform;
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
            PlazaCenter = plaza;
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

            var bridge = new GameObject("Khabur Bridge").transform;
            bridge.SetParent(world);
            bridge.position = new Vector3(bx, 0f, bestZ);
            bridge.rotation = Quaternion.Euler(0f, yaw, 0f);

            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Deck", bridge, new Vector3(0f, deckY, 0f), new Vector3(8f, 1.2f, 84f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "WallL", bridge, new Vector3(-3.8f, deckY + 1.0f, 0f), new Vector3(0.5f, 1f, 84f), stone);
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "WallR", bridge, new Vector3(3.8f, deckY + 1.0f, 0f), new Vector3(0.5f, 1f, 84f), stone);
            var arch = RafidainSceneBuilder.Prim(PrimitiveType.Cylinder, "Arch", bridge, new Vector3(0f, deckY - 13.2f, 0f), new Vector3(26f, 3.6f, 26f), stone, keepCollider: false);
            arch.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        static void BuildArena(Transform world, Material stone)
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
                RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Canopy", tree, new Vector3(0f, 6f * s, 0f), new Vector3(6f * s, 4.5f * s, 6f * s), leaves, keepCollider: false);
                RafidainSceneBuilder.Prim(PrimitiveType.Sphere, "Canopy2", tree, new Vector3(1.2f * s, 7.2f * s, 0.8f * s), new Vector3(4f * s, 3.2f * s, 4f * s), leaves, keepCollider: false);
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
                float s = Random.Range(1.5f, h > 60f ? 9f : 4f);
                var r = RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Rock", rocks,
                    new Vector3(x, h + s * 0.15f, z), new Vector3(s, s * Random.Range(0.5f, 1.1f), s * Random.Range(0.7f, 1.3f)), rock);
                r.transform.rotation = Random.rotation;
                r.isStatic = true;
                placed++;
            }
        }

        static bool IsFreeGround(float x, float z, float margin)
        {
            if (z > 20f && z < 975f && Mathf.Abs(x - RoadX(z)) < 12f + margin) return false;
            if (Vector2.Distance(new Vector2(x, z), TownCenter) < TownRadius + margin) return false;
            if (Mathf.Abs(z - RiverZ(x)) < 26f + margin * 0.5f) return false;
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
            RafidainSceneBuilder.Prim(PrimitiveType.Cube, "Visor", furat.transform, new Vector3(0f, 0.55f, 0.38f), new Vector3(0.35f, 0.12f, 0.1f), steel, keepCollider: false);

            // Camera
            var cam = Camera.main;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1600f;
            var rig = cam.gameObject.AddComponent<ThirdPersonCamera>();
            rig.target = furat.transform;
            cam.transform.position = furat.transform.position + new Vector3(0f, 2f, -4f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            // Systems
            var sys = new GameObject("_Game");
            sys.AddComponent<PlayerInputReader>();
            var gm = sys.AddComponent<GameManager>();
            var hud = sys.AddComponent<HUD>();
            sys.AddComponent<MobileControls>();
            gm.player = furat.GetComponent<PlayerStats>();
            hud.player = gm.player;

            // Flames
            var gate = RafidainSceneBuilder.MakeBonfire("zakho_gate", "Zakho - Southern Gate", OnRoad(td, 68f, 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire("zakho_oldtown", "Zakho - Old Town", Ground(td, PlazaCenter.x - 6f, PlazaCenter.y - 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire("zakho_bridge", "Khabur Bridge", OnRoad(td, BridgeZ - 52f, 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire("zakho_ascent", "Guardian's Ascent", OnRoad(td, 835f, 6f, 0f), ember, stone);
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

            // Bridge guards
            var brute = RafidainSceneBuilder.MakeEnemy(enemies, "Bridge Brute", OnRoad(td, BridgeZ + 50f, 0f, 1.4f), hollow, steel, 1.35f);
            brute.maxHealth = 180f; brute.damage = 30f; brute.windup = 1.0f; brute.moveSpeed = 2.3f; brute.emberReward = 180;
            var brute2 = RafidainSceneBuilder.MakeEnemy(enemies, "Mountain Brute", OnRoad(td, 815f, -3f, 1.4f), hollow, steel, 1.35f);
            brute2.maxHealth = 200f; brute2.damage = 32f; brute2.windup = 1.0f; brute2.moveSpeed = 2.3f; brute2.emberReward = 220;

            // Boss
            var boss = RafidainSceneBuilder.MakeEnemy(enemies, "Guardian of Zakho",
                new Vector3(ArenaCenter.x, ArenaHeight + 1.7f, ArenaCenter.y + 8f), guardian, steel, 1.6f);
            boss.isBoss = true;
            boss.bossId = "boss_zakho";
            boss.maxHealth = 700f;
            boss.damage = 34f;
            boss.windup = 0.9f;
            boss.recovery = 1.1f;
            boss.attackRange = 2.4f;
            boss.attackReach = 2.4f;
            boss.moveSpeed = 3.3f;
            boss.detectRange = 15f;
            boss.leashRange = 32f;
            boss.emberReward = 2000;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
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
