using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Builds the whole HUD from code (no prefabs needed): health/stamina bars, flasks, embers,
    /// compass with lost-embers marker, boss bar, prompts, messages and the death screen.
    /// Text is English for now; Arabic needs an RTL text plugin (planned).
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public PlayerStats player;

        Font font;
        RectTransform hpFill, stFill, bossFill;
        Text embersText, flaskText, promptText, messageText, bossName, deathText, lostMarker;
        GameObject bossRoot;
        CanvasGroup deathGroup, messageGroup;
        RectTransform compass;
        readonly Text[] cardinals = new Text[8];
        static readonly string[] CardinalNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        EnemyAI boss;
        string pendingPrompt;
        Coroutine messageCo;
        Transform cam;

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerStats>();
            if (Camera.main != null) cam = Camera.main.transform;
        }

        // ---------- public API ----------

        public void SetPrompt(string text) => pendingPrompt = text;

        public void ShowMessage(string text, float seconds)
        {
            if (messageCo != null) StopCoroutine(messageCo);
            messageCo = StartCoroutine(MessageRoutine(text, seconds));
        }

        public void ShowBoss(EnemyAI e)
        {
            boss = e;
            bossName.text = e.displayName;
            bossRoot.SetActive(true);
        }

        public void HideBoss()
        {
            boss = null;
            if (bossRoot != null) bossRoot.SetActive(false);
        }

        Coroutine deathCo;

        public void ShowDeath(bool show)
        {
            if (deathCo != null) StopCoroutine(deathCo);
            deathCo = StartCoroutine(DeathFade(show));
        }

        // ---------- update ----------

        void Update()
        {
            if (player != null)
            {
                SetFill(hpFill, player.HealthRatio);
                SetFill(stFill, player.StaminaRatio);
                embersText.text = player.Embers.ToString("N0") + "  EMBERS";
                flaskText.text = "FLASK  " + player.Flasks + " / " + player.maxFlasks + "   [R]";
            }

            if (boss != null)
            {
                if (boss.IsDead || !boss.gameObject.activeInHierarchy) HideBoss();
                else SetFill(bossFill, boss.HealthRatio);
            }

            UpdateCompass();
        }

        void LateUpdate()
        {
            promptText.text = pendingPrompt ?? "";
            promptText.enabled = !string.IsNullOrEmpty(pendingPrompt);
            pendingPrompt = null;
        }

        void UpdateCompass()
        {
            if (cam == null) return;
            float yaw = cam.eulerAngles.y;
            const float halfWidth = 260f;

            for (int i = 0; i < 8; i++)
            {
                float delta = Mathf.DeltaAngle(yaw, i * 45f);
                bool visible = Mathf.Abs(delta) <= 90f;
                cardinals[i].enabled = visible;
                if (visible) cardinals[i].rectTransform.anchoredPosition = new Vector2(delta / 90f * halfWidth, 0f);
            }

            var gm = GameManager.Instance;
            var lost = gm != null ? gm.CurrentLostEmbers : null;
            if (lost != null && player != null)
            {
                Vector3 to = lost.transform.position - player.transform.position;
                float angle = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float delta = Mathf.Clamp(Mathf.DeltaAngle(yaw, angle), -90f, 90f);
                lostMarker.enabled = true;
                lostMarker.text = "EMBERS\n" + Mathf.RoundToInt(to.magnitude) + "m";
                lostMarker.rectTransform.anchoredPosition = new Vector2(delta / 90f * halfWidth, -34f);
            }
            else lostMarker.enabled = false;
        }

        IEnumerator MessageRoutine(string text, float seconds)
        {
            messageText.text = text;
            yield return Fade(messageGroup, 1f, 0.3f);
            yield return new WaitForSeconds(seconds);
            yield return Fade(messageGroup, 0f, 0.6f);
        }

        IEnumerator DeathFade(bool show)
        {
            yield return Fade(deathGroup, show ? 1f : 0f, show ? 1.2f : 0.5f);
        }

        static IEnumerator Fade(CanvasGroup g, float to, float time)
        {
            float from = g.alpha;
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / time);
                yield return null;
            }
            g.alpha = to;
        }

        static void SetFill(RectTransform fill, float ratio)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        // ---------- building ----------

        void Build()
        {
            var canvasGo = new GameObject("HUD Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            // Health + stamina (top-left)
            hpFill = Bar(root, "Health", new Vector2(60, -50), new Vector2(520, 16), new Color(0.62f, 0.08f, 0.06f));
            stFill = Bar(root, "Stamina", new Vector2(60, -76), new Vector2(400, 10), new Color(0.2f, 0.55f, 0.22f));
            flaskText = Label(root, "Flasks", TopLeft, new Vector2(60, -96), new Vector2(400, 30), 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.75f, 0.45f));

            // Embers (bottom-right)
            embersText = Label(root, "Embers", new Vector2(1, 0), new Vector2(-60, 60), new Vector2(500, 50), 30, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.6f));
            embersText.rectTransform.pivot = new Vector2(1, 0);

            // Compass (top-centre)
            var compassBg = Rect(root, "Compass", new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(560, 30));
            var bgImg = compassBg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.35f);
            bgImg.raycastTarget = false;
            compass = compassBg;
            for (int i = 0; i < 8; i++)
            {
                bool major = i % 2 == 0;
                cardinals[i] = Label(compass, CardinalNames[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 30), major ? 24 : 16, TextAnchor.MiddleCenter,
                    i == 0 ? new Color(1f, 0.6f, 0.4f) : new Color(0.9f, 0.88f, 0.82f));
                cardinals[i].text = CardinalNames[i];
            }
            lostMarker = Label(compass, "LostMarker", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 44), 16, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.15f));

            // Boss bar (bottom-centre)
            var bossRt = Rect(root, "Boss", new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(1000, 60));
            bossRoot = bossRt.gameObject;
            bossName = Label(bossRt, "BossName", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1000, 30), 26, TextAnchor.LowerLeft, new Color(0.95f, 0.92f, 0.85f));
            bossName.rectTransform.pivot = new Vector2(0, 1);
            bossFill = Bar(bossRt, "BossHealth", new Vector2(0, -34), new Vector2(1000, 14), new Color(0.55f, 0.05f, 0.04f));
            bossRoot.SetActive(false);

            // Prompt (lower-centre)
            promptText = Label(root, "Prompt", new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(800, 40), 28, TextAnchor.MiddleCenter, Color.white);
            promptText.enabled = false;

            // Message (centre)
            messageText = Label(root, "Message", new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1400, 140), 46, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.65f));
            messageGroup = messageText.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
            messageGroup.blocksRaycasts = false;

            // Death screen
            var deathRt = Rect(root, "Death", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            deathRt.anchorMin = Vector2.zero; deathRt.anchorMax = Vector2.one; deathRt.sizeDelta = Vector2.zero;
            var deathBg = deathRt.gameObject.AddComponent<Image>();
            deathBg.color = new Color(0, 0, 0, 0.55f);
            deathBg.raycastTarget = false;
            deathGroup = deathRt.gameObject.AddComponent<CanvasGroup>();
            deathGroup.alpha = 0f;
            deathGroup.blocksRaycasts = false;
            deathText = Label(deathRt, "DeathText", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 200), 96, TextAnchor.MiddleCenter, new Color(0.7f, 0.08f, 0.05f));
            deathText.text = "YOU PERISHED";
        }

        static readonly Vector2 TopLeft = new Vector2(0, 1);

        RectTransform Bar(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var bg = Rect(parent, name, TopLeft, pos, size);
            bg.pivot = new Vector2(0, 1);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            bgImg.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            fill.SetParent(bg, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(2, 2);
            fill.offsetMax = new Vector2(-2, -2);
            fill.pivot = new Vector2(0, 0.5f);
            var img = fill.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return fill;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Text Label(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            if (anchor == TopLeft) rt.pivot = new Vector2(0, 1);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);
            return t;
        }
    }
}
