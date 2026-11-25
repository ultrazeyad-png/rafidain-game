using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Resting at a flame opens this: spend embers to raise Vigor (health), Endurance (stamina) or Strength (damage).
    /// Each level costs more than the last, like souls games.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        public static LevelUpUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        GameObject panel;
        Text levelText, embersText, costText, vigorText, endText, strText;
        Font font;

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            panel.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            var input = PlayerInputReader.Instance;
            if (input != null && input.PausePressed) Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            panel.SetActive(true);
            Time.timeScale = 0f;
            if (!Application.isMobilePlatform) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            panel.SetActive(false);
            Time.timeScale = 1f;
            if (!Application.isMobilePlatform) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
        }

        void Buy(int stat)
        {
            var player = GameManager.Instance != null ? GameManager.Instance.player : null;
            if (player == null) return;
            var d = SaveSystem.Data;
            int cost = PlayerStats.LevelCost(d.level);
            if (player.Embers < cost) return;
            player.Embers -= cost;
            d.level++;
            if (stat == 0) d.vigor++; else if (stat == 1) d.endurance++; else d.strength++;
            player.ApplyLevels();
            player.RestoreAll();
            if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 0.8f);
            Refresh();
        }

        void Refresh()
        {
            var d = SaveSystem.Data;
            var player = GameManager.Instance != null ? GameManager.Instance.player : null;
            int embers = player != null ? player.Embers : 0;
            int cost = PlayerStats.LevelCost(d.level);
            levelText.text = d.level + "  :" + ArabicText.Fix("المستوى");
            embersText.text = embers + "  :" + ArabicText.Fix("جمرك");
            costText.text = cost + "  :" + ArabicText.Fix("كلفة المستوى التالي");
            costText.color = embers >= cost ? new Color(0.95f, 0.85f, 0.6f) : new Color(0.8f, 0.3f, 0.25f);
            vigorText.text = (100 + d.vigor * 8) + "  " + ArabicText.Fix("الحيوية - الصحة");
            endText.text = (100 + d.endurance * 5) + "  " + ArabicText.Fix("التحمل - الطاقة");
            strText.text = Mathf.RoundToInt((1f + d.strength * 0.045f) * 100f) + "%  " + ArabicText.Fix("القوة - الضرر");
        }

        // ------------------------------------------------------------

        void Build()
        {
            var canvasGo = new GameObject("LevelUp Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 38;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.sizeDelta = new Vector2(900, 720);
            panel.AddComponent<Image>().color = new Color(0.06f, 0.045f, 0.03f, 0.94f);

            MakeText(ArabicText.Fix("الشعلة تهمس... ماذا تريد ان تصبح"), new Vector2(0, 300), 34, new Color(1f, 0.78f, 0.45f));
            levelText = MakeText("", new Vector2(0, 230), 30, Color.white);
            embersText = MakeText("", new Vector2(0, 185), 28, new Color(1f, 0.85f, 0.6f));
            costText = MakeText("", new Vector2(0, 140), 26, Color.white);

            vigorText = StatRow(new Vector2(0, 50), 0);
            endText = StatRow(new Vector2(0, -50), 1);
            strText = StatRow(new Vector2(0, -150), 2);

            var leave = MakeButton(ArabicText.Fix("مغادرة الشعلة"), new Vector2(0, -280), new Vector2(420, 80), 32);
            leave.onClick.AddListener(Close);
        }

        Text StatRow(Vector2 pos, int stat)
        {
            var label = MakeText("", pos + new Vector2(60, 0), 30, Color.white);
            label.alignment = TextAnchor.MiddleRight;
            label.rectTransform.sizeDelta = new Vector2(520, 70);
            var plus = MakeButton("+", pos + new Vector2(-300, 0), new Vector2(90, 70), 42);
            plus.onClick.AddListener(() => Buy(stat));
            return label;
        }

        Text MakeText(string text, Vector2 pos, int size, Color color)
        {
            var rt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(panel.transform, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(860, 60);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Button MakeButton(string label, Vector2 pos, Vector2 size, int fontSize)
        {
            var rt = new GameObject("Button", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(panel.transform, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.1f, 0.95f);
            var b = rt.gameObject.AddComponent<Button>();
            var trt = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            var t = trt.gameObject.AddComponent<Text>();
            t.font = font; t.text = label; t.fontSize = fontSize;
            t.color = new Color(0.96f, 0.9f, 0.78f);
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return b;
        }
    }
}
