using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Opening of the game (first time only): the story told over black, then the camera finds
    /// Furat asleep beside the flame, and he wakes with a start. Press any attack/roll key to skip.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        public float lineSeconds = 4.2f;

        static readonly string[] Story =
        {
            "زاخو... اقصى الشمال",
            "صحا رجل عند شعلة لا يعرفها",
            "لا يذكر من اين جاء... ولا الى اين يمضي",
            "لا يذكر الا اسما واحدا... دجلة",
            "وصوت من تحت الارض يناديه جنوبا... نحو بغداد",
        };

        CanvasGroup group;
        Text line;
        bool skip;

        void Start()
        {
            if (SaveSystem.Data.introSeen) return;
            StartCoroutine(Play());
        }

        void Update()
        {
            var input = PlayerInputReader.Instance;
            if (input == null) return;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) skip = true;
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) skip = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.touchCount > 0) skip = true;
#endif
        }

        IEnumerator Play()
        {
            var player = FindAnyObjectByType<PlayerStats>();
            var reader = PlayerInputReader.Instance;
            var cam = Camera.main;
            var camRig = cam != null ? cam.GetComponent<ThirdPersonCamera>() : null;
            var rig = player != null ? player.GetComponent<HumanoidRig>() : null;
            var bonfire = FindNearestBonfire(player);

            if (reader != null) reader.enabled = false;
            if (camRig != null) camRig.enabled = false;
            if (rig != null) rig.lyingDown = true;
            BuildOverlay();

            // 1. the story over black
            group.alpha = 1f;
            foreach (var s in Story)
            {
                if (skip) break;
                line.text = ArabicText.Fix(s);
                yield return FadeText(0f, 1f, 1f);
                float t = 0f;
                while (t < lineSeconds - 2f && !skip) { t += Time.deltaTime; yield return null; }
                yield return FadeText(1f, 0f, 1f);
            }
            line.text = "";

            // 2. slow camera move onto Furat asleep by the flame
            if (cam != null && player != null)
            {
                Vector3 focus = player.transform.position + Vector3.up * 0.3f;
                Vector3 from = focus + new Vector3(5f, 3.5f, -6f);
                Vector3 to = focus + new Vector3(1.6f, 1.2f, -2.6f);
                if (bonfire != null) focus = Vector3.Lerp(focus, bonfire.transform.position + Vector3.up * 0.5f, 0.25f);
                StartCoroutine(FadeGroup(1f, 0f, 2.5f));
                float t = 0f, dur = skip ? 0.1f : 6f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / dur);
                    cam.transform.position = Vector3.Lerp(from, to, k);
                    cam.transform.rotation = Quaternion.LookRotation(focus - cam.transform.position);
                    yield return null;
                }
            }
            group.alpha = 0f;

            // 3. he wakes with a start
            if (rig != null) rig.lyingDown = false;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.25f);
            if (HUD.Instance != null) HUD.Instance.ShowMessage(ArabicText.Fix("فرات"), 2.5f);
            yield return new WaitForSeconds(0.6f);

            if (camRig != null) camRig.enabled = true;
            if (reader != null) reader.enabled = true;
            SaveSystem.Data.introSeen = true;
            SaveSystem.Save();
            Destroy(group.gameObject, 0.1f);
        }

        static Bonfire FindNearestBonfire(PlayerStats player)
        {
            if (player == null) return null;
            Bonfire best = null;
            float bestD = float.MaxValue;
            foreach (var b in FindObjectsByType<Bonfire>())
            {
                float d = Vector3.Distance(b.transform.position, player.transform.position);
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        void BuildOverlay()
        {
            var go = new GameObject("Intro Canvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            group = go.AddComponent<CanvasGroup>();

            var bg = new GameObject("Black", typeof(RectTransform)).GetComponent<RectTransform>();
            bg.SetParent(go.transform, false);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.sizeDelta = Vector2.zero;
            bg.gameObject.AddComponent<Image>().color = Color.black;

            var tr = new GameObject("Line", typeof(RectTransform)).GetComponent<RectTransform>();
            tr.SetParent(go.transform, false);
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(1600, 200);
            line = tr.gameObject.AddComponent<Text>();
            line.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            line.fontSize = 46;
            line.alignment = TextAnchor.MiddleCenter;
            line.color = new Color(0.92f, 0.86f, 0.74f, 0f);
            line.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        IEnumerator FadeText(float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                var c = line.color; c.a = Mathf.Lerp(from, to, t / time); line.color = c;
                yield return null;
            }
        }

        IEnumerator FadeGroup(float from, float to, float time)
        {
            float t = 0f;
            var bg = group.GetComponentInChildren<Image>();
            while (t < time)
            {
                t += Time.deltaTime;
                if (bg != null) { var c = bg.color; c.a = Mathf.Lerp(from, to, t / time); bg.color = c; }
                yield return null;
            }
        }
    }
}
