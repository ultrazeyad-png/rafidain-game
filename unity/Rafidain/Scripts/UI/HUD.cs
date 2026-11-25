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
        CanvasGroup deathGroup, messageGroup, blackGroup, victoryGroup;
        Text victoryText, victorySub, bossTitle, bossEpithet;
        CanvasGroup bossTitleGroup;
        RectTransform bossTitleRt;
        RectTransform deathTextRt, victoryTextRt;
        RectTransform compass;
        readonly Text[] cardinals = new Text[8];
        static readonly string[] CardinalNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        static readonly string EmbersWord = ArabicText.Fix("جمر");
        static readonly string FlaskWord = ArabicText.Fix("قارورة");
        static readonly string LostWord = ArabicText.Fix("جمرك");

        EnemyAI boss;
        RectTransform lockMarker;
        ThirdPersonCamera camRig;
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
            StartCoroutine(Arrival());
            if (player == null) player = FindAnyObjectByType<PlayerStats>();
            if (Camera.main != null)
            {
                cam = Camera.main.transform;
                camRig = Camera.main.GetComponent<ThirdPersonCamera>();
            }
        }

        // ---------- public API ----------

        public void SetPrompt(string text) => pendingPrompt = text;

        public void Shake(float amount)
        {
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(amount);
        }

        public void ShowMessage(string text, float seconds)
        {
            if (messageCo != null) StopCoroutine(messageCo);
            messageCo = StartCoroutine(MessageRoutine(text, seconds));
        }

        GameObject dialogRoot;
        Text dialogName, dialogBody;
        Coroutine dialogCo;

        /// <summary>Talk box at the bottom of the screen. Pass already-fixed Arabic text (ArabicText.Fix), lines split with \n.</summary>
        public void ShowDialog(string speaker, string body, float seconds)
        {
            if (dialogRoot == null) BuildDialog();
            dialogName.text = speaker;
            dialogBody.text = body;
            dialogRoot.SetActive(true);
            if (dialogCo != null) StopCoroutine(dialogCo);
            dialogCo = StartCoroutine(HideDialogLater(seconds));
        }

        public void HideDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }

        IEnumerator HideDialogLater(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HideDialog();
        }

        void BuildDialog()
        {
            var canvas = GetComponentInChildren<Canvas>().transform;
            var rt = Rect(canvas, "Dialog", new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(1300f, 170f));
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.03f, 0.82f);
            bg.raycastTarget = false;
            dialogRoot = rt.gameObject;
            dialogName = Label(rt, "Speaker", new Vector2(1f, 1f), new Vector2(-30f, -16f), new Vector2(600f, 40f), 28, TextAnchor.MiddleRight, new Color(1f, 0.78f, 0.45f));
            dialogName.rectTransform.pivot = new Vector2(1f, 1f);
            dialogBody = Label(rt, "Body", new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(1240f, 120f), 30, TextAnchor.MiddleRight, new Color(0.95f, 0.92f, 0.85f));
            dialogRoot.SetActive(false);
        }

        public void ShowBoss(EnemyAI e)
        {
            boss = e;
            bossName.text = ArabicText.Fix(e.displayName);
            bossRoot.SetActive(true);
            if (AudioManager.Instance != null) AudioManager.Instance.BossMusic(true);
        }

        public void HideBoss()
        {
            boss = null;
            if (bossRoot != null) bossRoot.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.BossMusic(false);
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
                embersText.text = player.Embers.ToString("N0") + "  " + EmbersWord;
                flaskText.text = "[R]   " + player.Flasks + " / " + player.maxFlasks + "  " + FlaskWord;
            }

            if (boss != null)
            {
                if (boss.IsDead || !boss.gameObject.activeInHierarchy) HideBoss();
                else SetFill(bossFill, boss.HealthRatio);
            }

            UpdateCompass();
            UpdateLockMarker();
        }

        void UpdateLockMarker()
        {
            if (lockMarker == null) return;
            var target = camRig != null ? camRig.LockTarget : null;
            if (target == null || Camera.main == null) { lockMarker.gameObject.SetActive(false); return; }
            Vector3 sp = Camera.main.WorldToScreenPoint(target.position + Vector3.up * 0.6f * target.lossyScale.y);
            bool visible = sp.z > 0f;
            lockMarker.gameObject.SetActive(visible);
            if (visible) lockMarker.position = sp;
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
                lostMarker.text = LostWord + "\n" + Mathf.RoundToInt(to.magnitude) + "m";
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
            if (!show) { yield return Fade(deathGroup, 0f, 0.5f); yield break; }
            deathTextRt.localScale = Vector3.one * 0.94f;
            float t = 0f;
            while (t < 3.2f)
            {
                t += Time.unscaledDeltaTime;
                deathGroup.alpha = Mathf.Clamp01(t / 1.4f);
                deathTextRt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.08f, t / 3.2f);
                yield return null;
            }
        }

        /// <summary>Fade the whole screen to black (to = 1) or back (to = 0).</summary>
        public Coroutine FadeBlack(float to, float seconds)
        {
            return StartCoroutine(Fade(blackGroup, to, seconds));
        }

        /// <summary>Souls-style area name when a governorate loads, and Dijla's whisper on the first visit.</summary>
        IEnumerator Arrival()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            int index = Regions.IndexOf(scene);
            if (index < 0) yield break;
            // let the opening story finish first (only Zakho has one)
            while (!SaveSystem.Data.introSeen && FindAnyObjectByType<IntroSequence>() != null) yield return null;
            yield return new WaitForSeconds(1.2f);
            ShowBossTitle(ArabicText.Fix(Regions.Names[index]),
                (index + 1) + " / " + Regions.Count + "   " + ArabicText.Fix("على الطريق الى بغداد"));

            var visited = SaveSystem.Data.visitedRegions;
            if (visited == null || visited.Contains(scene)) yield break;
            visited.Add(scene);
            yield return new WaitForSeconds(3.6f);
            ShowDialog(ArabicText.Fix("دجلة"), ArabicText.Fix(Regions.Whispers[index]), 6f);
        }

        Coroutine titleCo;

        /// <summary>Big name card when a guardian is met for the first time.</summary>
        public void ShowBossTitle(string name, string epithet)
        {
            if (titleCo != null) StopCoroutine(titleCo);
            titleCo = StartCoroutine(BossTitleRoutine(name, epithet));
        }

        IEnumerator BossTitleRoutine(string name, string epithet)
        {
            bossTitle.text = name;
            bossEpithet.text = epithet;
            float t = 0f;
            while (t < 2.6f)
            {
                t += Time.unscaledDeltaTime;
                bossTitleGroup.alpha = Mathf.Clamp01(t / 0.5f);
                bossTitleRt.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.03f, t / 2.6f);
                yield return null;
            }
            yield return Fade(bossTitleGroup, 0f, 0.8f);
        }

        Coroutine victoryCo;

        /// <summary>Big golden "a great enemy has fallen" banner.</summary>
        public void ShowVictory(string subtitle)
        {
            if (victoryCo != null) StopCoroutine(victoryCo);
            victoryCo = StartCoroutine(VictoryRoutine(subtitle));
        }

        IEnumerator VictoryRoutine(string subtitle)
        {
            victorySub.text = subtitle ?? "";
            victoryTextRt.localScale = Vector3.one * 1.12f;
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.unscaledDeltaTime;
                victoryGroup.alpha = Mathf.Clamp01(t / 0.6f);
                victoryTextRt.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, t / 1.2f));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(3f);
            yield return Fade(victoryGroup, 0f, 1.2f);
        }

        void MakeBand(RectTransform parent, Color c)
        {
            var band = Rect(parent, "Band", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            band.anchorMin = new Vector2(0f, 0.5f); band.anchorMax = new Vector2(1f, 0.5f);
            band.sizeDelta = new Vector2(0f, 260f);
            var img = band.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
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

            // Death screen: a dark band across the middle, blood-red title that slowly grows (souls style)
            var deathRt = Rect(root, "Death", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            deathRt.anchorMin = Vector2.zero; deathRt.anchorMax = Vector2.one; deathRt.sizeDelta = Vector2.zero;
            var deathVeil = deathRt.gameObject.AddComponent<Image>();
            deathVeil.color = new Color(0.05f, 0f, 0f, 0.35f);
            deathVeil.raycastTarget = false;
            deathGroup = deathRt.gameObject.AddComponent<CanvasGroup>();
            deathGroup.alpha = 0f;
            deathGroup.blocksRaycasts = false;
            MakeBand(deathRt, new Color(0f, 0f, 0f, 0.82f));
            deathText = Label(deathRt, "DeathText", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 200), 110, TextAnchor.MiddleCenter, new Color(0.62f, 0.05f, 0.04f));
            deathText.text = ArabicText.Fix("هلكت");
            deathText.fontStyle = FontStyle.Bold;
            deathTextRt = deathText.rectTransform;

            // Victory screen: golden title over a band
            var vicRt = Rect(root, "Victory", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            vicRt.anchorMin = Vector2.zero; vicRt.anchorMax = Vector2.one; vicRt.sizeDelta = Vector2.zero;
            victoryGroup = vicRt.gameObject.AddComponent<CanvasGroup>();
            victoryGroup.alpha = 0f;
            victoryGroup.blocksRaycasts = false;
            MakeBand(vicRt, new Color(0.02f, 0.015f, 0f, 0.78f));
            victoryText = Label(vicRt, "VictoryText", new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(1600, 170), 92, TextAnchor.MiddleCenter, new Color(0.98f, 0.8f, 0.42f));
            victoryText.text = ArabicText.Fix("سقط عدو عظيم");
            victoryText.fontStyle = FontStyle.Bold;
            victoryTextRt = victoryText.rectTransform;
            victorySub = Label(vicRt, "VictorySub", new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(1600, 60), 30, TextAnchor.MiddleCenter, new Color(0.9f, 0.82f, 0.68f));

            // Boss title card: name in large letters, epithet beneath, thin gold lines
            var btRt = Rect(root, "BossTitle", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one; btRt.sizeDelta = Vector2.zero;
            bossTitleGroup = btRt.gameObject.AddComponent<CanvasGroup>();
            bossTitleGroup.alpha = 0f;
            bossTitleGroup.blocksRaycasts = false;
            MakeBand(btRt, new Color(0f, 0f, 0f, 0.55f));
            bossEpithet = Label(btRt, "Epithet", new Vector2(0.5f, 0.5f), new Vector2(0, 75), new Vector2(1600, 60), 30, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.6f));
            bossTitle = Label(btRt, "Name", new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1600, 150), 96, TextAnchor.MiddleCenter, new Color(0.96f, 0.9f, 0.8f));
            bossTitle.fontStyle = FontStyle.Bold;
            bossTitleRt = bossTitle.rectTransform;
            for (int i = -1; i <= 1; i += 2)
            {
                var line = Rect(btRt, "Line", new Vector2(0.5f, 0.5f), new Vector2(0, i < 0 ? -90 : 110), new Vector2(700, 2));
                var li = line.gameObject.AddComponent<Image>();
                li.color = new Color(0.9f, 0.7f, 0.35f, 0.8f);
                li.raycastTarget = false;
            }

            // full-screen black for respawn / scene transitions (above everything else)
            var blackRt = Rect(root, "Black", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            blackRt.anchorMin = Vector2.zero; blackRt.anchorMax = Vector2.one; blackRt.sizeDelta = Vector2.zero;
            var blackImg = blackRt.gameObject.AddComponent<Image>();
            blackImg.color = Color.black;
            blackImg.raycastTarget = false;
            blackGroup = blackRt.gameObject.AddComponent<CanvasGroup>();
            blackGroup.alpha = 0f;
            blackGroup.blocksRaycasts = false;

            // lock-on marker
            lockMarker = Rect(root, "LockOn", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
            var lockImg = lockMarker.gameObject.AddComponent<Image>();
            lockImg.color = new Color(1f, 0.85f, 0.55f, 0.95f);
            lockImg.raycastTarget = false;
            blackGroup.transform.SetAsLastSibling();   // the fade covers every HUD element
            lockMarker.localRotation = Quaternion.Euler(0f, 0f, 45f);
            lockMarker.gameObject.SetActive(false);
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
