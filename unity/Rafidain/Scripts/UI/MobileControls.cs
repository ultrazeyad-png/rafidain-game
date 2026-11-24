using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// On-screen touch controls built from code: joystick on the left, drag-to-look on the right,
    /// and action buttons. Shown automatically on phones. Tick "Show In Editor" to test on PC.
    /// </summary>
    public class MobileControls : MonoBehaviour
    {
        public bool showInEditor = false;

        Font font;

        void Awake()
        {
            bool show = Application.isMobilePlatform || (Application.isEditor && showInEditor);
            EnsureEventSystem();
            if (!show) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void Build()
        {
            var canvasGo = new GameObject("Mobile Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // Look area: right half of the screen (created first so buttons sit on top)
            var look = NewRect(root, "LookArea");
            look.anchorMin = new Vector2(0.5f, 0f);
            look.anchorMax = new Vector2(1f, 1f);
            look.offsetMin = look.offsetMax = Vector2.zero;
            var lookImg = look.gameObject.AddComponent<Image>();
            lookImg.color = new Color(0, 0, 0, 0);
            look.gameObject.AddComponent<TouchLookArea>();

            // Joystick (bottom-left)
            var stick = NewRect(root, "Joystick");
            stick.anchorMin = stick.anchorMax = new Vector2(0, 0);
            stick.anchoredPosition = new Vector2(260, 240);
            stick.sizeDelta = new Vector2(280, 280);
            stick.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.12f);
            var knob = NewRect(stick, "Knob");
            knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(120, 120);
            var knobImg = knob.gameObject.AddComponent<Image>();
            knobImg.color = new Color(1, 1, 1, 0.35f);
            knobImg.raycastTarget = false;
            var js = stick.gameObject.AddComponent<VirtualJoystick>();
            js.knob = knob;
            js.radius = 120f;

            // Action buttons (bottom-right)
            Button(root, "ATTACK", GameAction.Attack, new Vector2(-230, 230), 190, new Color(0.6f, 0.15f, 0.1f, 0.55f));
            Button(root, "HEAVY", GameAction.HeavyAttack, new Vector2(-420, 150), 130, new Color(0.5f, 0.1f, 0.1f, 0.5f));
            Button(root, "ROLL", GameAction.Dodge, new Vector2(-230, 450), 140, new Color(0.2f, 0.3f, 0.5f, 0.5f));
            Button(root, "HEAL", GameAction.Heal, new Vector2(-560, 90), 110, new Color(0.6f, 0.45f, 0.1f, 0.5f));
            Button(root, "USE", GameAction.Interact, new Vector2(-420, 340), 110, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            Button(root, "LOCK", GameAction.LockOn, new Vector2(-110, 620), 110, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            var sprint = Button(root, "RUN", GameAction.Dodge, new Vector2(400, 470), 120, new Color(0.25f, 0.4f, 0.25f, 0.5f), leftSide: true);
            sprint.holdToSprint = true;
            var block = Button(root, "BLOCK", GameAction.Dodge, new Vector2(-600, 260), 120, new Color(0.45f, 0.35f, 0.15f, 0.5f));
            block.holdToBlock = true;
        }

        MobileButton Button(Transform root, string label, GameAction action, Vector2 pos, float size, Color color, bool leftSide = false)
        {
            var rt = NewRect(root, label);
            rt.anchorMin = rt.anchorMax = leftSide ? new Vector2(0, 0) : new Vector2(1, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            rt.gameObject.AddComponent<Image>().color = color;

            var textRt = NewRect(rt, "Label");
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var t = textRt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = label;
            t.fontSize = Mathf.RoundToInt(size * 0.2f);
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1, 1, 1, 0.9f);
            t.raycastTarget = false;

            var b = rt.gameObject.AddComponent<MobileButton>();
            b.action = action;
            return b;
        }

        static RectTransform NewRect(Transform parent, string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
    }
}
