using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Souls-style floating health bars over ordinary enemies, shown for a few seconds after they are hit,
    /// with the damage of the current combo next to the bar. Bosses use the big bar at the bottom instead.
    /// Creates itself; nothing to place in the scene.
    /// </summary>
    public class EnemyBars : MonoBehaviour
    {
        const float ShowSeconds = 4f;
        const int PoolSize = 10;

        class Bar
        {
            public RectTransform root, fill, trail;
            public Text number;
            public CanvasGroup group;
            public float trailRatio = 1f;
            public EnemyAI owner;
        }

        readonly List<Bar> bars = new List<Bar>();
        readonly List<EnemyAI> shown = new List<EnemyAI>();
        Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindAnyObjectByType<EnemyBars>() != null) return;
            var go = new GameObject("_EnemyBars");
            DontDestroyOnLoad(go);
            go.AddComponent<EnemyBars>();
        }

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;   // under the HUD
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            for (int i = 0; i < PoolSize; i++) bars.Add(MakeBar());
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            shown.Clear();
            if (cam != null && !PauseMenu.IsPaused && !WorldMapUI.IsOpen)
            {
                foreach (var e in EnemyAI.All)
                {
                    if (e == null || e.isBoss || e.IsDead || !e.gameObject.activeInHierarchy) continue;
                    if (Time.time - e.LastHitTime > ShowSeconds) continue;
                    shown.Add(e);
                    if (shown.Count >= PoolSize) break;
                }
            }

            for (int i = 0; i < bars.Count; i++)
            {
                var b = bars[i];
                if (i >= shown.Count) { b.root.gameObject.SetActive(false); b.owner = null; continue; }
                var e = shown[i];
                if (b.owner != e) { b.owner = e; b.trailRatio = e.HealthRatio; }

                Vector3 head = e.transform.position + Vector3.up * (1.35f * e.transform.lossyScale.y + 0.25f);
                Vector3 sp = cam.WorldToScreenPoint(head);
                if (sp.z <= 0f) { b.root.gameObject.SetActive(false); continue; }
                b.root.gameObject.SetActive(true);
                b.root.position = sp;

                float ratio = e.HealthRatio;
                b.trailRatio = Mathf.MoveTowards(b.trailRatio, ratio, Time.deltaTime * (Time.time - e.LastHitTime > 0.6f ? 0.8f : 0f));
                if (b.trailRatio < ratio) b.trailRatio = ratio;
                b.fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
                b.trail.anchorMax = new Vector2(Mathf.Clamp01(b.trailRatio), 1f);

                float since = Time.time - e.LastHitTime;
                b.number.text = since < 1.6f ? Mathf.RoundToInt(e.RecentDamage).ToString() : "";
                b.group.alpha = since > ShowSeconds - 0.6f ? Mathf.Clamp01((ShowSeconds - since) / 0.6f) : 1f;
            }
        }

        Bar MakeBar()
        {
            var b = new Bar();
            b.root = new GameObject("Bar", typeof(RectTransform)).GetComponent<RectTransform>();
            b.root.SetParent(transform, false);
            b.root.sizeDelta = new Vector2(120, 9);
            b.group = b.root.gameObject.AddComponent<CanvasGroup>();
            b.group.blocksRaycasts = false;
            var bg = b.root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            bg.raycastTarget = false;
            b.trail = Fill(b.root, new Color(0.95f, 0.75f, 0.35f, 0.9f));
            b.fill = Fill(b.root, new Color(0.62f, 0.08f, 0.06f, 1f));

            var nrt = new GameObject("Damage", typeof(RectTransform)).GetComponent<RectTransform>();
            nrt.SetParent(b.root, false);
            nrt.anchorMin = nrt.anchorMax = new Vector2(1f, 0.5f);
            nrt.pivot = new Vector2(0f, 0.5f);
            nrt.anchoredPosition = new Vector2(8f, 0f);
            nrt.sizeDelta = new Vector2(80, 30);
            b.number = nrt.gameObject.AddComponent<Text>();
            b.number.font = font;
            b.number.fontSize = 22;
            b.number.color = new Color(1f, 0.93f, 0.8f);
            b.number.alignment = TextAnchor.MiddleLeft;
            b.number.raycastTarget = false;
            var outline = nrt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);

            b.root.gameObject.SetActive(false);
            return b;
        }

        static RectTransform Fill(RectTransform parent, Color c)
        {
            var rt = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return rt;
        }
    }
}
