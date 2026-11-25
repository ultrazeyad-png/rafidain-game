using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>Title screen in Arabic: Continue / New Game / Settings / Quit, over a lone flame in the dark.</summary>
    public class TitleScreen : MonoBehaviour
    {
        public string firstScene = "Zakho_Region";

        Font font;
        GameObject mainPanel, settingsPanel;
        Text volumeLabel, sensLabel, qualityLabel, invertLabel;
        Transform cam;

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Camera.main != null) cam = Camera.main.transform;
            Build();
        }

        void Update()
        {
            if (cam != null) cam.rotation = Quaternion.Euler(8f + Mathf.Sin(Time.time * 0.2f) * 1.5f, Mathf.Sin(Time.time * 0.15f) * 4f, 0f);
        }

        bool HasSave => !string.IsNullOrEmpty(SaveSystem.Data.currentScene) || SaveSystem.Data.playTimeSeconds > 5f;

        void Continue()
        {
            string s = SaveSystem.Data.currentScene;
            Load(string.IsNullOrEmpty(s) ? firstScene : s);
        }

        void NewGame()
        {
            SaveSystem.DeleteSave();
            Load(firstScene);
        }

        void Load(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene)) scene = firstScene;
            SceneLoader.Load(scene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------- settings (shared PlayerPrefs with the pause menu)

        void ChangeVolume(float d)
        {
            float v = Mathf.Clamp01(PlayerPrefs.GetFloat("rafidain_volume", 1f) + d);
            PlayerPrefs.SetFloat("rafidain_volume", v);
            AudioListener.volume = v;
            Refresh();
        }

        void ChangeSens(float d)
        {
            PlayerPrefs.SetFloat("rafidain_sens", Mathf.Clamp(PlayerPrefs.GetFloat("rafidain_sens", 1.3f) + d, 0.3f, 3f));
            Refresh();
        }

        void CycleQuality()
        {
            DeviceQuality.SetManual((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
            Refresh();
        }

        void Refresh()
        {
            volumeLabel.text = Mathf.RoundToInt(PlayerPrefs.GetFloat("rafidain_volume", 1f) * 100f) + "%  :" + ArabicText.Fix("الصوت");
            sensLabel.text = PlayerPrefs.GetFloat("rafidain_sens", 1.3f).ToString("0.0") + "  :" + ArabicText.Fix("حساسية الكاميرا");
            invertLabel.text = ArabicText.Fix(PlayerPrefs.GetInt("rafidain_invert", 0) == 1 ? "نعم" : "لا") + "  :" + ArabicText.Fix("عكس الكاميرا العمودي");
            qualityLabel.text = QualitySettings.names[QualitySettings.GetQualityLevel()] + "  :" + ArabicText.Fix("الجودة");
        }

        // ---------------- UI

        void Build()
        {
            var canvasGo = new GameObject("Title Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var title = MakeText(canvasGo.transform, ArabicText.Fix("وادي الرافدين"), new Vector2(0, 330), 110, new Color(0.95f, 0.78f, 0.45f));
            title.fontStyle = FontStyle.Bold;
            MakeText(canvasGo.transform, ArabicText.Fix("من زاخو الى بغداد... ليعيد ما اخذه العالم السفلي"), new Vector2(0, 230), 30, new Color(0.85f, 0.8f, 0.72f));

            mainPanel = Panel(canvasGo.transform);
            float y = 60f;
            if (HasSave) { MakeButton(mainPanel.transform, ArabicText.Fix("متابعة"), new Vector2(0, y), Continue); y -= 100f; }
            MakeButton(mainPanel.transform, ArabicText.Fix("لعبة جديدة"), new Vector2(0, y), NewGame); y -= 100f;
            MakeButton(mainPanel.transform, ArabicText.Fix("الاعدادات"), new Vector2(0, y), () => { mainPanel.SetActive(false); settingsPanel.SetActive(true); Refresh(); }); y -= 100f;
            MakeButton(mainPanel.transform, ArabicText.Fix("خروج"), new Vector2(0, y), Quit);

            settingsPanel = Panel(canvasGo.transform);
            volumeLabel = MakeText(settingsPanel.transform, "", new Vector2(0, 80), 34, Color.white);
            MakeSmall(settingsPanel.transform, "-", new Vector2(-330, 80), () => ChangeVolume(-0.1f));
            MakeSmall(settingsPanel.transform, "+", new Vector2(330, 80), () => ChangeVolume(0.1f));
            sensLabel = MakeText(settingsPanel.transform, "", new Vector2(0, -10), 34, Color.white);
            MakeSmall(settingsPanel.transform, "-", new Vector2(-330, -10), () => ChangeSens(-0.1f));
            MakeSmall(settingsPanel.transform, "+", new Vector2(330, -10), () => ChangeSens(0.1f));
            qualityLabel = MakeText(settingsPanel.transform, "", new Vector2(0, -100), 34, Color.white);
            MakeSmall(settingsPanel.transform, ">", new Vector2(330, -100), CycleQuality);
            invertLabel = MakeText(settingsPanel.transform, "", new Vector2(0, -190), 34, Color.white);
            MakeSmall(settingsPanel.transform, ">", new Vector2(330, -190), () => { PlayerPrefs.SetInt("rafidain_invert", PlayerPrefs.GetInt("rafidain_invert", 0) == 1 ? 0 : 1); Refresh(); });
            MakeButton(settingsPanel.transform, ArabicText.Fix("رجوع"), new Vector2(0, -300), () => { settingsPanel.SetActive(false); mainPanel.SetActive(true); });
            settingsPanel.SetActive(false);

            MakeText(canvasGo.transform, "Next Level  ·  v0.9", new Vector2(0, -490), 20, new Color(0.6f, 0.55f, 0.5f));
        }

        GameObject Panel(Transform parent)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900, 600);
            rt.anchoredPosition = new Vector2(0, -90);
            return go;
        }

        Text MakeText(Transform parent, string text, Vector2 pos, int size, Color color)
        {
            var rt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1600, 140);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.8f);
            sh.effectDistance = new Vector2(3, -3);
            return t;
        }

        void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
            => Btn(parent, label, pos, new Vector2(460, 80), 36, action);

        void MakeSmall(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
            => Btn(parent, label, pos, new Vector2(80, 70), 40, action);

        void Btn(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize, UnityEngine.Events.UnityAction action)
        {
            var rt = new GameObject("Button", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.08f, 0.05f, 0.75f);
            var b = rt.gameObject.AddComponent<Button>();
            var c = b.colors; c.highlightedColor = new Color(1f, 0.8f, 0.5f); c.pressedColor = new Color(0.8f, 0.55f, 0.3f); b.colors = c;
            b.onClick.AddListener(action);
            var trt = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            var t = trt.gameObject.AddComponent<Text>();
            t.font = font; t.text = label; t.fontSize = fontSize;
            t.color = new Color(0.96f, 0.88f, 0.72f);
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
        }
    }
}
