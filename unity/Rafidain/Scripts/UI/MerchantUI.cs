using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// The town merchant (عطار): spend embers on lasting upgrades — stronger flasks, a sharper blade, a steadier guard.
    /// Created on demand; Merchant.cs opens it.
    /// </summary>
    public class MerchantUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        static MerchantUI instance;

        const int MaxRank = 5;
        GameObject panel;
        Text embersText, greeting;
        readonly Text[] rows = new Text[3];
        readonly Text[] prices = new Text[3];
        Font font;
        string merchantName;

        public static void Open(string merchant, string line)
        {
            if (instance == null)
            {
                var go = new GameObject("_Merchant UI");
                instance = go.AddComponent<MerchantUI>();
            }
            instance.Show(merchant, line);
        }

        public static int Price(int rank) => Mathf.RoundToInt(450f * Mathf.Pow(rank + 1, 1.6f));

        void Awake()
        {
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

        void Show(string merchant, string line)
        {
            merchantName = merchant;
            greeting.text = ArabicText.Fix(merchant) + "\n" + ArabicText.Fix(line);
            IsOpen = true;
            panel.SetActive(true);
            Time.timeScale = 0f;
            if (!Application.isMobilePlatform) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            Refresh();
        }

        void Close()
        {
            IsOpen = false;
            panel.SetActive(false);
            Time.timeScale = 1f;
            if (!Application.isMobilePlatform) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
        }

        int Rank(int i)
        {
            var d = SaveSystem.Data;
            return i == 0 ? d.flaskPotency : i == 1 ? d.sharpness : d.guard;
        }

        void Buy(int i)
        {
            var player = GameManager.Instance != null ? GameManager.Instance.player : null;
            if (player == null) return;
            int rank = Rank(i);
            if (rank >= MaxRank) return;
            int cost = Price(rank);
            if (player.Embers < cost) return;
            player.Embers -= cost;
            var d = SaveSystem.Data;
            if (i == 0) d.flaskPotency++; else if (i == 1) d.sharpness++; else d.guard++;
            player.ApplyLevels();
            if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 0.8f);
            Refresh();
        }

        void Refresh()
        {
            var player = GameManager.Instance != null ? GameManager.Instance.player : null;
            int embers = player != null ? player.Embers : 0;
            embersText.text = embers + "  :" + ArabicText.Fix("جمرك");
            string[] names = { "زيت اقوى للقارورة", "شحذ السلاح", "تثبيت الدرع" };
            string[] what = { "كل قارورة تشفي اكثر", "ضرر اكثر بكل الاسلحة", "الصد ياكل طاقة اقل" };
            for (int i = 0; i < 3; i++)
            {
                int r = Rank(i);
                rows[i].text = ArabicText.Fix(names[i] + "  (" + what[i] + ")") + "   " + r + "/" + MaxRank;
                if (r >= MaxRank) { prices[i].text = ArabicText.Fix("اكتمل"); prices[i].color = new Color(0.6f, 0.9f, 0.5f); }
                else
                {
                    int c = Price(r);
                    prices[i].text = c.ToString();
                    prices[i].color = embers >= c ? new Color(1f, 0.85f, 0.55f) : new Color(0.8f, 0.3f, 0.25f);
                }
            }
        }

        void Build()
        {
            var canvasGo = new GameObject("Merchant Canvas");
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
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 700);
            panel.AddComponent<Image>().color = new Color(0.06f, 0.045f, 0.03f, 0.95f);

            greeting = MakeText("", new Vector2(0, 270), 30, new Color(1f, 0.8f, 0.5f));
            greeting.rectTransform.sizeDelta = new Vector2(1000, 110);
            embersText = MakeText("", new Vector2(0, 180), 28, new Color(1f, 0.85f, 0.6f));
            for (int i = 0; i < 3; i++)
            {
                float y = 80 - i * 110;
                rows[i] = MakeText("", new Vector2(120, y), 28, Color.white);
                rows[i].alignment = TextAnchor.MiddleRight;
                rows[i].rectTransform.sizeDelta = new Vector2(760, 70);
                int idx = i;
                var b = MakeButton("", new Vector2(-400, y), new Vector2(200, 70), 28);
                prices[i] = b.GetComponentInChildren<Text>();
                b.onClick.AddListener(() => Buy(idx));
            }
            MakeButton(ArabicText.Fix("مع السلامة"), new Vector2(0, -290), new Vector2(380, 70), 30).onClick.AddListener(Close);
        }

        Text MakeText(string text, Vector2 pos, int size, Color color)
        {
            var rt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(panel.transform, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1000, 60);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
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
