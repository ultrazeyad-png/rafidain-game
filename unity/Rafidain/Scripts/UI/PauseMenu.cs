using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Pause menu (Esc / Start / the pause button on phones), all in Arabic:
    /// resume, graphics quality, camera sensitivity, delete progress, quit.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        GameObject panel;
        Text qualityLabel;
        Text sensLabel;
        Text volumeLabel;
        Text invertLabel;
        Text hapticsLabel;
        GameObject controlsPanel;
        GameObject statsPanel;
        RectTransform routeRoot;

        // rough longitude / latitude of each stop on the road, to draw the route over Iraq
        static readonly Vector2[] RouteGeo =
        {
            new Vector2(42.68f, 37.14f), new Vector2(42.99f, 36.87f), new Vector2(43.13f, 36.34f), new Vector2(44.01f, 36.19f),
            new Vector2(45.43f, 35.56f), new Vector2(44.39f, 35.47f), new Vector2(43.87f, 34.20f), new Vector2(43.30f, 33.42f),
            new Vector2(44.02f, 32.60f), new Vector2(44.33f, 32.00f), new Vector2(44.93f, 31.99f), new Vector2(45.28f, 31.31f),
            new Vector2(47.78f, 30.51f), new Vector2(47.15f, 31.84f), new Vector2(46.26f, 31.04f), new Vector2(45.82f, 32.51f),
            new Vector2(44.42f, 32.47f), new Vector2(44.65f, 33.75f), new Vector2(44.36f, 33.31f),
        };

        /// <summary>The road from Zakho to Baghdad drawn over Iraq: gold = guardian fallen, red = here.</summary>
        void DrawRoute()
        {
            for (int i = routeRoot.childCount - 1; i >= 0; i--) Destroy(routeRoot.GetChild(i).gameObject);
            var d = SaveSystem.Data;
            string here = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Vector2 size = routeRoot.sizeDelta;
            Vector2 P(int i) => new Vector2(
                Mathf.InverseLerp(42.3f, 48.2f, RouteGeo[i].x) * size.x - size.x * 0.5f,
                Mathf.InverseLerp(30.2f, 37.4f, RouteGeo[i].y) * size.y - size.y * 0.5f);

            int n = Mathf.Min(RouteGeo.Length, Regions.Count);
            for (int i = 0; i + 1 < n; i++)
            {
                Vector2 a = P(i), b = P(i + 1);
                var line = new GameObject("Road", typeof(RectTransform)).GetComponent<RectTransform>();
                line.SetParent(routeRoot, false);
                line.anchoredPosition = (a + b) * 0.5f;
                line.sizeDelta = new Vector2((b - a).magnitude, 3f);
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                var li = line.gameObject.AddComponent<Image>();
                li.color = new Color(0.8f, 0.65f, 0.4f, 0.35f);
                li.raycastTarget = false;
            }
            for (int i = 0; i < n; i++)
            {
                bool fallen = d.defeatedBosses.Contains("boss_" + (i == n - 1 ? "adapa" : Regions.Scenes[i].Replace("_Region", "").ToLowerInvariant()));
                bool isHere = Regions.Scenes[i] == here;
                bool visited = d.visitedRegions != null && d.visitedRegions.Contains(Regions.Scenes[i]);
                var dot = new GameObject("Stop", typeof(RectTransform)).GetComponent<RectTransform>();
                dot.SetParent(routeRoot, false);
                dot.anchoredPosition = P(i);
                dot.sizeDelta = Vector2.one * (isHere ? 20f : 13f);
                dot.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var di = dot.gameObject.AddComponent<Image>();
                di.color = isHere ? new Color(0.85f, 0.15f, 0.1f) : fallen ? new Color(1f, 0.78f, 0.35f) : visited ? new Color(0.8f, 0.75f, 0.65f) : new Color(0.35f, 0.32f, 0.3f);
                di.raycastTarget = false;
                var label = MakeText(routeRoot, ArabicText.Fix(Regions.Names[i]), P(i) + new Vector2(0f, 16f), 16, new Color(0.85f, 0.8f, 0.7f, visited || isHere ? 1f : 0.45f));
                label.rectTransform.sizeDelta = new Vector2(200f, 24f);
            }
        }
        Text statsBody;
        Font font;
        float sensitivity = 1.3f;
        bool invertY;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sensitivity = PlayerPrefs.GetFloat("rafidain_sens", 1.3f);
            invertY = PlayerPrefs.GetInt("rafidain_invert", 0) == 1;
            Build();
            SetPaused(false);
        }

        void Start() => ApplySensitivity();

        void Update()
        {
            var input = PlayerInputReader.Instance;
            if (input != null && input.PausePressed && !WorldMapUI.IsOpen && !LevelUpUI.IsOpen && !MerchantUI.IsOpen)
            {
                if (controlsPanel != null && controlsPanel.activeSelf) controlsPanel.SetActive(false);
                else if (statsPanel != null && statsPanel.activeSelf) statsPanel.SetActive(false);
                else SetPaused(!IsPaused);
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (panel != null) panel.SetActive(paused);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (statsPanel != null) statsPanel.SetActive(false);
            Time.timeScale = paused ? 0f : 1f;
            if (!Application.isMobilePlatform)
            {
                Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = paused;
            }
            RefreshLabels();
        }

        void CycleQuality()
        {
            int next = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            DeviceQuality.SetManual(next);
            RefreshLabels();
        }

        void ChangeSensitivity(float delta)
        {
            sensitivity = Mathf.Clamp(sensitivity + delta, 0.3f, 3f);
            PlayerPrefs.SetFloat("rafidain_sens", sensitivity);
            ApplySensitivity();
            RefreshLabels();
        }

        void ApplySensitivity()
        {
            if (ThirdPersonCamera.Instance != null)
            {
                ThirdPersonCamera.Instance.sensitivity = sensitivity;
                ThirdPersonCamera.Instance.invertY = invertY;
            }
        }

        void ToggleInvert()
        {
            invertY = !invertY;
            PlayerPrefs.SetInt("rafidain_invert", invertY ? 1 : 0);
            ApplySensitivity();
            RefreshLabels();
        }

        void ChangeVolume(float delta)
        {
            float v = Mathf.Clamp01(PlayerPrefs.GetFloat("rafidain_volume", 1f) + delta);
            PlayerPrefs.SetFloat("rafidain_volume", v);
            AudioListener.volume = v;
            if (AudioManager.Instance != null) AudioManager.Instance.masterVolume = v;
            RefreshLabels();
        }

        void DeleteProgress()
        {
            SaveSystem.DeleteSave();
            SetPaused(false);
            // a fresh journey starts from the title screen (or Zakho when there is none)
            SceneLoader.Load(Application.CanStreamedLevelBeLoaded("MainMenu") ? "MainMenu" : "Zakho_Region");
        }

        void ToTitle()
        {
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
            SetPaused(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneLoader.Load("MainMenu");
        }

        void Quit()
        {
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void RefreshLabels()
        {
            if (qualityLabel != null)
            {
                string q = QualitySettings.names[QualitySettings.GetQualityLevel()];
                qualityLabel.text = q + "  :" + ArabicText.Fix("الجودة");
            }
            if (volumeLabel != null)
                volumeLabel.text = Mathf.RoundToInt(PlayerPrefs.GetFloat("rafidain_volume", 1f) * 100f) + "%  :" + ArabicText.Fix("الصوت");
            if (hapticsLabel != null)
                hapticsLabel.text = ArabicText.Fix(Haptics.Enabled ? "الاهتزاز: نعم" : "الاهتزاز: لا");
            if (invertLabel != null)
                invertLabel.text = ArabicText.Fix(invertY ? "نعم" : "لا") + "  :" + ArabicText.Fix("عكس الكاميرا العمودي");
            if (sensLabel != null) sensLabel.text = sensitivity.ToString("0.0") + "  :" + ArabicText.Fix("حساسية الكاميرا");
        }

        // ------------------------------------------------------------

        void Build()
        {
            var canvasGo = new GameObject("Pause Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // small pause button for phones (top-right)
            if (Application.isMobilePlatform)
            {
                var pb = MakeButton(canvasGo.transform, "II", new Vector2(1, 1), new Vector2(-70, -70), new Vector2(90, 90), 36);
                pb.onClick.AddListener(() => SetPaused(!IsPaused));
            }

            panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.sizeDelta = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var title = MakeText(panel.transform, ArabicText.Fix("وادي الرافدين"), new Vector2(0, 330), 64, new Color(0.95f, 0.8f, 0.5f));
            title.fontStyle = FontStyle.Bold;

            MakeButton(panel.transform, ArabicText.Fix("استئناف"), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(520, 70), 34)
                .onClick.AddListener(() => SetPaused(false));

            var qb = MakeButton(panel.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, 145), new Vector2(520, 70), 28);
            qualityLabel = qb.GetComponentInChildren<Text>();
            qb.onClick.AddListener(CycleQuality);

            volumeLabel = MakeText(panel.transform, "", new Vector2(0, 65), 28, Color.white);
            MakeButton(panel.transform, "-", new Vector2(0.5f, 0.5f), new Vector2(-320, 65), new Vector2(80, 64), 40).onClick.AddListener(() => ChangeVolume(-0.1f));
            MakeButton(panel.transform, "+", new Vector2(0.5f, 0.5f), new Vector2(320, 65), new Vector2(80, 64), 40).onClick.AddListener(() => ChangeVolume(0.1f));

            sensLabel = MakeText(panel.transform, "", new Vector2(0, -15), 28, Color.white);
            MakeButton(panel.transform, "-", new Vector2(0.5f, 0.5f), new Vector2(-320, -15), new Vector2(80, 64), 40).onClick.AddListener(() => ChangeSensitivity(-0.1f));
            MakeButton(panel.transform, "+", new Vector2(0.5f, 0.5f), new Vector2(320, -15), new Vector2(80, 64), 40).onClick.AddListener(() => ChangeSensitivity(0.1f));

            var ib = MakeButton(panel.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(520, 64), 26);
            invertLabel = ib.GetComponentInChildren<Text>();
            ib.onClick.AddListener(ToggleInvert);
            if (Application.isMobilePlatform)
            {
                var hb = MakeButton(panel.transform, "", new Vector2(0.5f, 0.5f), new Vector2(420, -95), new Vector2(280, 64), 24);
                hapticsLabel = hb.GetComponentInChildren<Text>();
                hb.onClick.AddListener(() => { Haptics.Enabled = !Haptics.Enabled; RefreshLabels(); });
            }

            MakeButton(panel.transform, ArabicText.Fix("رحلتك"), new Vector2(0.5f, 0.5f), new Vector2(-135, -170), new Vector2(250, 64), 28)
                .onClick.AddListener(OpenStats);
            MakeButton(panel.transform, ArabicText.Fix("الأزرار"), new Vector2(0.5f, 0.5f), new Vector2(135, -170), new Vector2(250, 64), 28)
                .onClick.AddListener(() => controlsPanel.SetActive(true));

            MakeButton(panel.transform, ArabicText.Fix("مسح التقدم والبدء من جديد"), new Vector2(0.5f, 0.5f), new Vector2(0, -245), new Vector2(520, 64), 24)
                .onClick.AddListener(DeleteProgress);
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                MakeButton(panel.transform, ArabicText.Fix("القائمة الرئيسية"), new Vector2(0.5f, 0.5f), new Vector2(0, -320), new Vector2(520, 64), 28)
                    .onClick.AddListener(ToTitle);
            MakeButton(panel.transform, ArabicText.Fix("خروج"), new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(520, 70), 32)
                .onClick.AddListener(Quit);

            BuildControlsPage(canvasGo.transform);
            BuildStatsPage(canvasGo.transform);
        }

        void BuildControlsPage(Transform canvas)
        {
            controlsPanel = new GameObject("Controls", typeof(RectTransform));
            controlsPanel.transform.SetParent(canvas, false);
            var rt = controlsPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            controlsPanel.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.02f, 0.96f);

            MakeText(controlsPanel.transform, ArabicText.Fix("الأزرار والتحكم"), new Vector2(0, 400), 54, new Color(0.95f, 0.8f, 0.5f)).fontStyle = FontStyle.Bold;

            string[,] rows =
            {
                { "الحركة", "WASD", "العصا اليسرى" },
                { "الكاميرا", "الماوس", "العصا اليمنى" },
                { "ركض", "Shift", "B مطوّل" },
                { "تدحرج", "Space", "B" },
                { "ضربة خفيفة / إعدام", "زر الماوس الأيسر", "RB" },
                { "ضربة ثقيلة", "F", "RT" },
                { "صد بالدرع", "زر الماوس الأيمن", "LB" },
                { "صد متقن (بلحظة الضربة)", "زر الماوس الأيمن", "LB" },
                { "تثبيت على العدو", "Q / زر العجلة", "R3" },
                { "تبديل الهدف", "حرك الماوس يمين/يسار", "العصا اليمنى يمين/يسار" },
                { "تفاعل / كلام", "E", "Y" },
                { "شرب الدواء", "R", "X" },
                { "الخريطة", "M / Tab", "Select" },
                { "إيقاف", "Esc", "Start" },
            };

            float y = 335;
            MakeColumnRow(ArabicText.Fix("الفعل"), ArabicText.Fix("كيبورد"), ArabicText.Fix("يد تحكم"), y, new Color(0.95f, 0.8f, 0.5f));
            y -= 52;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                MakeColumnRow(ArabicText.Fix(rows[i, 0]), ArabicText.Fix(rows[i, 1]), ArabicText.Fix(rows[i, 2]), y, new Color(0.92f, 0.88f, 0.8f));
                y -= 42;
            }

            MakeText(controlsPanel.transform, ArabicText.Fix("بالموبايل: العصا يسار للحركة، اسحب يمين للكاميرا، والأزرار على اليمين"), new Vector2(0, y - 10), 24, new Color(0.8f, 0.75f, 0.65f));

            MakeButton(controlsPanel.transform, ArabicText.Fix("رجوع"), new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(360, 70), 32)
                .onClick.AddListener(() => controlsPanel.SetActive(false));
            controlsPanel.SetActive(false);
        }

        void BuildStatsPage(Transform canvas)
        {
            statsPanel = new GameObject("Stats", typeof(RectTransform));
            statsPanel.transform.SetParent(canvas, false);
            var rt = statsPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            statsPanel.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.02f, 0.96f);
            MakeText(statsPanel.transform, ArabicText.Fix("رحلة فرات"), new Vector2(0, 380), 56, new Color(0.95f, 0.8f, 0.5f)).fontStyle = FontStyle.Bold;
            statsBody = MakeText(statsPanel.transform, "", new Vector2(380, 0), 30, new Color(0.93f, 0.88f, 0.78f));
            statsBody.rectTransform.sizeDelta = new Vector2(820, 700);
            statsBody.alignment = TextAnchor.MiddleRight;
            routeRoot = new GameObject("Route Map", typeof(RectTransform)).GetComponent<RectTransform>();
            routeRoot.SetParent(statsPanel.transform, false);
            routeRoot.anchoredPosition = new Vector2(-430, 0);
            routeRoot.sizeDelta = new Vector2(620, 680);
            statsBody.lineSpacing = 1.25f;
            MakeButton(statsPanel.transform, ArabicText.Fix("رجوع"), new Vector2(0.5f, 0.5f), new Vector2(0, -420), new Vector2(360, 70), 32)
                .onClick.AddListener(() => statsPanel.SetActive(false));
            statsPanel.SetActive(false);
        }

        void OpenStats()
        {
            var d = SaveSystem.Data;
            int t = Mathf.FloorToInt(d.playTimeSeconds);
            string time = (t / 3600) + ":" + ((t / 60) % 60).ToString("00");
            string Row(string label, string value) => value + "   " + ArabicText.Fix(label);
            var lines = new[]
            {
                Row("المستوى", d.level.ToString()),
                Row("الرحلة", d.ngCycle == 0 ? ArabicText.Fix("الاولى") : "+" + d.ngCycle),
                Row("الحيوية / التحمل / القوة", d.vigor + " / " + d.endurance + " / " + d.strength),
                Row("السلاح", ArabicText.Fix(Weapons.Get(d.equippedWeapon).name)),
                Row("الحراس الذين سقطوا", d.defeatedBosses.Count + " / " + Regions.Count),
                Row("قطع المفتاح", Mathf.Min(d.keys.Count, Regions.KeysNeeded) + " / " + Regions.KeysNeeded),
                Row("ذكريات دجلة", Mathf.Min(d.keys.Count, Regions.Memories.Length) + " / " + Regions.Memories.Length),
                Row("قطع الخريطة", d.mapPieces.Count + " / " + Regions.Count * 2),
                Row("جرار الزيت المقدس", d.sacredOils.Count + " / " + Regions.Count),
                Row("الاسلحة", (d.weapons != null ? d.weapons.Count : 1) + " / 3"),
                Row("الشعلات المشتعلة", d.discoveredBonfires.Count.ToString()),
                Row("مرات الهلاك", d.deaths.ToString()),
                Row("وقت الرحلة", time),
            };
            statsBody.text = string.Join("\n", lines);
            DrawRoute();
            statsPanel.SetActive(true);
        }

        void MakeColumnRow(string action, string keyboard, string pad, float y, Color c)
        {
            // RTL: action on the right, keyboard in the middle, gamepad on the left
            MakeText(controlsPanel.transform, action, new Vector2(360, y), 28, c).alignment = TextAnchor.MiddleCenter;
            MakeText(controlsPanel.transform, keyboard, new Vector2(0, y), 28, c).alignment = TextAnchor.MiddleCenter;
            MakeText(controlsPanel.transform, pad, new Vector2(-360, y), 28, c).alignment = TextAnchor.MiddleCenter;
        }

        Text MakeText(Transform parent, string text, Vector2 pos, int size, Color color)
        {
            var rt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1000, 90);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }

        Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize)
        {
            var rt = new GameObject("Button", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.25f, 0.18f, 0.10f, 0.92f);
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.6f, 0.35f);
            b.colors = colors;

            var trt = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            var t = trt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = label;
            t.fontSize = fontSize;
            t.color = new Color(0.96f, 0.9f, 0.78f);
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return b;
        }
    }
}
