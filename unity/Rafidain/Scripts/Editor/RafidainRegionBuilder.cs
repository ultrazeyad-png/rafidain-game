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
                terrain.detailObjectDistance = 110f;
                terrain.detailObjectDensity = 1f;
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
                waterGo.isStatic = true;

                EditorUtility.DisplayProgressBar("Rafidain", "Building Zakho old town…", 0.6f);
                var world = new GameObject("World").transform;
                BuildTown(world, terrainData, mud1, mud2, limestone, roofMat, stone, wood, tile, glow, windowMat);
                BuildBridge(world, stone);
                BuildArena(world, stone, glow);

                EditorUtility.DisplayProgressBar("Rafidain", "Planting oak forest and rocks…", 0.75f);
                ScatterNature(world, terrainData, bark, leaves, rockMat);
                ScatterPalms(world, terrainData, palmBark, palmLeaf);

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
            td.detailPrototypes = new[] { proto };
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

        static Light SetupAtmosphere()
        {
            var sun = Object.FindAnyObjectByType<Light>();
            sun.name = "Sun";
            // low golden-hour sun: long shadows, dark-fantasy mood
            sun.color = new Color(1f, 0.64f, 0.40f);
            sun.intensity = 1.75f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            sun.transform.rotation = Quaternion.Euler(10f, -35f, 0f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 1.55f);
                sky.SetColor("_SkyTint", new Color(0.46f, 0.43f, 0.50f));
                sky.SetColor("_GroundColor", new Color(0.35f, 0.32f, 0.28f));
                sky.SetFloat("_Exposure", 1.0f);
                AssetDatabase.CreateAsset(sky, $"{Gen}/ZakhoSky.mat");
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.53f, 0.47f, 0.45f);
            RenderSettings.fogDensity = 0.0042f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.50f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.42f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.09f);
            return sun;
        }

        // =====================================================================
        // Old town, bridge, arena
        // =====================================================================

        static void BuildTown(Transform world, TerrainData td, Material mudA, Material mudB, Material lime, Material roof, Material stone,
            Material wood, Material tile, Material glow, Material windowMat)
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
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            RafidainSceneBuilder.MakeAsh(cam.transform);

            // Systems
            var sys = new GameObject("_Game");
            sys.AddComponent<PlayerInputReader>();
            var gm = sys.AddComponent<GameManager>();
            var hud = sys.AddComponent<HUD>();
            sys.AddComponent<MobileControls>();
            sys.AddComponent<IntroSequence>();
            gm.player = furat.GetComponent<PlayerStats>();
            hud.player = gm.player;

            // Flames
            var gate = RafidainSceneBuilder.MakeBonfire("zakho_gate", "زاخو - البوابة الجنوبية", OnRoad(td, 68f, 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire("zakho_oldtown", "زاخو - المدينة القديمة", Ground(td, PlazaCenter.x - 6f, PlazaCenter.y - 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire("zakho_bridge", "جسر الخابور", OnRoad(td, BridgeZ - 52f, 6f, 0f), ember, stone);
            RafidainSceneBuilder.MakeBonfire("zakho_ascent", "صعود الحارس", OnRoad(td, 835f, 6f, 0f), ember, stone);
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
            var brute = RafidainSceneBuilder.MakeEnemy(enemies, "Bridge Brute", OnRoad(td, BridgeZ + 50f, 0f, 1.4f), hollow, steel, 1.35f, 3);
            brute.maxHealth = 180f; brute.damage = 30f; brute.windup = 1.0f; brute.moveSpeed = 2.3f; brute.emberReward = 180;
            var brute2 = RafidainSceneBuilder.MakeEnemy(enemies, "Mountain Brute", OnRoad(td, 815f, -3f, 1.4f), hollow, steel, 1.35f, 3);
            brute2.maxHealth = 200f; brute2.damage = 32f; brute2.windup = 1.0f; brute2.moveSpeed = 2.3f; brute2.emberReward = 220;

            // Boss
            var boss = RafidainSceneBuilder.MakeEnemy(enemies, "حارس زاخو",
                new Vector3(ArenaCenter.x, ArenaHeight + 1.7f, ArenaCenter.y + 8f), guardian, steel, 1.6f, 2);
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
            boss.canLeap = true;
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
