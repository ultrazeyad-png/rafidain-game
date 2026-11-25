using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// The governorate map (M / Tab / Select / MAP button).
    /// Hidden under old parchment until map pieces are found; each piece uncovers an irregular,
    /// ink-blot shaped area. Shows Furat and every kindled flame; click a flame to travel there.
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        const float MapPx = 860f;
        const int FogRes = 256;

        GameObject panel;
        RawImage mapImage, fogImage;
        RectTransform playerMarker;
        RectTransform markerLayer;
        Text title, info;
        Texture2D fogTex;
        Font font;
        bool open;
        public static bool IsOpen { get; private set; }
        readonly List<GameObject> flameButtons = new List<GameObject>();

        static readonly Color Parchment = new Color(0.36f, 0.30f, 0.22f, 1f);

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            panel.SetActive(false);
        }

        void Update()
        {
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Map)) Toggle();
            if (open)
            {
                UpdatePlayerMarker();
                if (input != null && input.PausePressed) Toggle();
            }
        }

        public void Toggle()
        {
            if ((PauseMenu.IsPaused || LevelUpUI.IsOpen) && !open) return;
            open = !open;
            IsOpen = open;
            panel.SetActive(open);
            Time.timeScale = open ? 0f : 1f;
            if (!Application.isMobilePlatform)
            {
                Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = open;
            }
            if (open) Refresh();
        }

        void Refresh()
        {
            var region = RegionInfo.Instance;
            mapImage.texture = region != null ? region.mapTexture : null;
            title.text = ArabicText.Fix("خريطة " + (region != null ? region.regionName : ""));

            int found = 0, total = region != null ? region.pieces.Length : 0;
            if (region != null) foreach (var p in region.pieces) if (SaveSystem.Data.mapPieces.Contains(p.id)) found++;
            info.text = SaveSystem.Data.keys.Count + " / " + Regions.KeysNeeded + "  " + ArabicText.Fix("قطع المفتاح") + "        " +
                        found + " / " + total + "  " + ArabicText.Fix("قطع الخريطة");

            BuildFog(region);
            BuildFlameButtons();
            UpdatePlayerMarker();
        }

        // Irregular reveal: distance to each found piece's centre, warped by noise, so edges look like torn ink blots.
        void BuildFog(RegionInfo region)
        {
            if (fogTex == null)
            {
                fogTex = new Texture2D(FogRes, FogRes, TextureFormat.RGBA32, false);
                fogTex.wrapMode = TextureWrapMode.Clamp;
                fogImage.texture = fogTex;
            }
            float size = region != null ? region.worldSize : 1024f;
            var px = new Color[FogRes * FogRes];
            for (int y = 0; y < FogRes; y++)
            {
                for (int x = 0; x < FogRes; x++)
                {
                    float wx = (x + 0.5f) / FogRes * size;
                    float wz = (y + 0.5f) / FogRes * size;
                    float reveal = 0f;
                    if (region != null)
                    {
                        foreach (var p in region.pieces)
                        {
                            if (!SaveSystem.Data.mapPieces.Contains(p.id)) continue;
                            float d = Vector2.Distance(new Vector2(wx, wz), p.center);
                            float warp = (Mathf.PerlinNoise(wx * 0.006f + p.seed, wz * 0.006f + p.seed * 0.37f) - 0.5f) * p.radius * 0.9f
                                       + (Mathf.PerlinNoise(wx * 0.02f + p.seed * 2f, wz * 0.02f) - 0.5f) * p.radius * 0.25f;
                            float edge = p.radius + warp - d;
                            reveal = Mathf.Max(reveal, Mathf.Clamp01(edge / 25f));
                        }
                    }
                    var c = Parchment;
                    float grain = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.08f;
                    c.r += grain; c.g += grain; c.b += grain * 0.6f;
                    c.a = 1f - reveal;
                    px[y * FogRes + x] = c;
                }
            }
            fogTex.SetPixels(px);
            fogTex.Apply();
        }

        void BuildFlameButtons()
        {
            foreach (var b in flameButtons) Destroy(b);
            flameButtons.Clear();
            var region = RegionInfo.Instance;
            float size = region != null ? region.worldSize : 1024f;

            foreach (var bf in FindObjectsByType<Bonfire>())
            {
                if (!bf.IsDiscovered) continue;
                var rt = new GameObject("Flame", typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(markerLayer, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(30f, 30f);
                rt.anchoredPosition = WorldToMap(bf.transform.position, size);
                rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 0.55f, 0.15f);
                var btn = rt.gameObject.AddComponent<Button>();
                var target = bf;
                btn.onClick.AddListener(() =>
                {
                    Toggle();
                    if (GameManager.Instance != null) GameManager.Instance.FastTravel(target);
                });

                var label = MakeText(markerLayer, ArabicText.Fix(bf.displayName), rt.anchoredPosition + new Vector2(0f, -30f), 20, new Color(1f, 0.85f, 0.6f));
                flameButtons.Add(rt.gameObject);
                flameButtons.Add(label.gameObject);
            }
        }

        void UpdatePlayerMarker()
        {
            var player = GameManager.Instance != null ? GameManager.Instance.player : null;
            if (player == null) return;
            var region = RegionInfo.Instance;
            float size = region != null ? region.worldSize : 1024f;
            playerMarker.anchoredPosition = WorldToMap(player.transform.position, size);
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -player.transform.eulerAngles.y);
        }

        static Vector2 WorldToMap(Vector3 world, float size)
            => new Vector2((world.x / size - 0.5f) * MapPx, (world.z / size - 0.5f) * MapPx);

        // ------------------------------------------------------------

        void Build()
        {
            var canvasGo = new GameObject("Map Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            panel = new GameObject("MapPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.sizeDelta = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.03f, 0.025f, 0.02f, 0.9f);

            var frame = new GameObject("Frame", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(panel.transform, false);
            frame.sizeDelta = new Vector2(MapPx + 24f, MapPx + 24f);
            frame.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.32f, 0.16f);

            mapImage = MakeRaw(panel.transform, "Map");
            fogImage = MakeRaw(panel.transform, "Fog");

            markerLayer = new GameObject("Markers", typeof(RectTransform)).GetComponent<RectTransform>();
            markerLayer.SetParent(panel.transform, false);
            markerLayer.sizeDelta = new Vector2(MapPx, MapPx);

            playerMarker = new GameObject("Furat", typeof(RectTransform)).GetComponent<RectTransform>();
            playerMarker.SetParent(markerLayer, false);
            playerMarker.sizeDelta = new Vector2(22f, 34f);
            var pImg = playerMarker.gameObject.AddComponent<Image>();
            pImg.color = new Color(0.95f, 0.92f, 0.8f);
            pImg.raycastTarget = false;

            title = MakeText(panel.transform, "", new Vector2(0f, MapPx * 0.5f + 50f), 44, new Color(0.95f, 0.8f, 0.5f));
            info = MakeText(panel.transform, "", new Vector2(0f, -MapPx * 0.5f - 45f), 26, new Color(0.9f, 0.85f, 0.75f));
        }

        RawImage MakeRaw(Transform parent, string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(MapPx, MapPx);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            return raw;
        }

        Text MakeText(Transform parent, string text, Vector2 pos, int size, Color color)
        {
            var rt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1000f, 60f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
