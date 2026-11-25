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
        Font font;
        float sensitivity = 1.3f;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sensitivity = PlayerPrefs.GetFloat("rafidain_sens", 1.3f);
            Build();
            SetPaused(false);
        }

        void Start() => ApplySensitivity();

        void Update()
        {
            var input = PlayerInputReader.Instance;
            if (input != null && input.PausePressed && !WorldMapUI.IsOpen && !LevelUpUI.IsOpen) SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (panel != null) panel.SetActive(paused);
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
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.sensitivity = sensitivity;
        }

        void DeleteProgress()
        {
            SaveSystem.DeleteSave();
            SetPaused(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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

            MakeButton(panel.transform, ArabicText.Fix("استئناف"), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(520, 80), 36)
                .onClick.AddListener(() => SetPaused(false));

            var qb = MakeButton(panel.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(520, 80), 30);
            qualityLabel = qb.GetComponentInChildren<Text>();
            qb.onClick.AddListener(CycleQuality);

            sensLabel = MakeText(panel.transform, "", new Vector2(0, -30), 30, Color.white);
            MakeButton(panel.transform, "-", new Vector2(0.5f, 0.5f), new Vector2(-320, -30), new Vector2(80, 70), 40).onClick.AddListener(() => ChangeSensitivity(-0.1f));
            MakeButton(panel.transform, "+", new Vector2(0.5f, 0.5f), new Vector2(320, -30), new Vector2(80, 70), 40).onClick.AddListener(() => ChangeSensitivity(0.1f));

            MakeButton(panel.transform, ArabicText.Fix("مسح التقدم والبدء من جديد"), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(520, 70), 26)
                .onClick.AddListener(DeleteProgress);
            MakeButton(panel.transform, ArabicText.Fix("خروج"), new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(520, 80), 34)
                .onClick.AddListener(Quit);
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
