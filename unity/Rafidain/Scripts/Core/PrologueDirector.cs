using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Opening cinematic (new game only): Baghdad by night. Furat steals the key of the underworld gate from
    /// Adapa's altar; Adapa shatters it into eighteen shards, scatters them over the governorates,
    /// wipes Furat's memory and casts him north. Then the game begins in Zakho, where he wakes remembering nothing.
    /// Esc / Enter / tap skips.
    /// </summary>
    public class PrologueDirector : MonoBehaviour
    {
        [Header("Cast")]
        public Transform furat;
        public Transform adapa;
        public Transform key;
        public Light keyLight;
        public Light sealLight;
        [Header("Places")]
        public Transform cityShotFrom, cityShotLook;
        public Transform gateShotFrom, gateShotLook;
        public Transform walkStart, walkEnd;
        public string nextScene = "Zakho_Region";

        Camera cam;
        Text subtitle;
        CanvasGroup subGroup;
        Image white, black;
        bool skipping, done;

        void Start()
        {
            cam = Camera.main;
            BuildOverlay();
            if (adapa != null) adapa.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartCoroutine(Play());
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) Skip();
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) Skip();
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.startButton.wasPressedThisFrame) Skip();
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.touchCount > 0) Skip();
#endif
        }

        void Skip()
        {
            if (skipping || done) return;
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(Finish(0.6f));
        }

        IEnumerator Play()
        {
            black.color = Color.black;
            if (AudioManager.Instance != null) AudioManager.Instance.BossMusic(true);

            // 1. Baghdad by night, slow drift over the Tigris
            StartCoroutine(FadeImage(black, 0f, 3f));
            StartCoroutine(Glide(cityShotFrom.position, cityShotFrom.position + cityShotFrom.right * 14f + Vector3.up * 2f, cityShotLook.position, 11f));
            yield return Say("بغداد... قبل ان ينسى فرات كل شيء", 4.5f);
            yield return Say("تحت المدينة باب الى العالم السفلي... وخلفه دجلة، اخذتها ارشكيكال", 5.5f);

            // 2. the gate and its altar
            StartCoroutine(Glide(gateShotFrom.position, gateShotFrom.position + gateShotFrom.forward * 4f, gateShotLook.position, 9f));
            yield return Say("ومفتاح الباب في يد ادابا... حارس ارشكيكال", 4.5f);

            // 3. Furat creeps in and takes the key
            Vector3 side = walkEnd.position + (walkEnd.right * 4.5f) + Vector3.up * 1.6f;
            StartCoroutine(Follow(furat, side, 7f));
            yield return Walk(furat, walkStart.position, walkEnd.position, 1.6f);
            StartCoroutine(Say("فسرقه فرات... ليفتح الباب ويرجعها", 3.6f));
            var rig = furat.GetComponent<HumanoidRig>();
            if (rig != null) rig.PlayReach(1f);
            yield return new WaitForSeconds(0.7f);
            if (key != null)
            {
                key.SetParent(furat, true);
                key.localPosition = new Vector3(0.45f, 0.6f, 0.45f);
            }
            if (keyLight != null) keyLight.intensity *= 2.5f;
            yield return new WaitForSeconds(2.6f);

            // 4. Adapa rises behind him
            if (adapa != null)
            {
                adapa.gameObject.SetActive(true);
                adapa.position = furat.position - furat.forward * 5f + Vector3.up * 0.4f;
                adapa.rotation = Quaternion.LookRotation(Flat(furat.position - adapa.position));
                HitFX.Ring(adapa.position + Vector3.down * 1.2f, 3f, new Color(0.5f, 0.2f, 0.9f), 48);
                if (sealLight != null) sealLight.color = new Color(0.6f, 0.2f, 1f);
                if (ThirdPersonCamera.Instance == null && cam != null) StartCoroutine(Shake(0.35f, 0.8f));
                StartCoroutine(Glide(cam.transform.position, adapa.position + adapa.forward * 6f + adapa.right * 2.5f + Vector3.up * 1.2f, adapa.position + Vector3.up * 2.2f, 2.5f));
                yield return Say("لكن ادابا لا ينام", 3f);
                furat.rotation = Quaternion.LookRotation(Flat(adapa.position - furat.position));
                var arig = adapa.GetComponent<HumanoidRig>();
                if (arig != null) arig.PlayReach(1f);
                yield return new WaitForSeconds(0.8f);

                // 5. the key shatters into eighteen shards
                yield return Shatter();
                yield return Say("فكسر المفتاح ثماني عشرة قطعة... ونثرها على محافظات العراق، في يد حراس لا يموتون", 5.5f);

                // 6. Furat is struck with forgetting
                if (rig != null) rig.lyingDown = true;
                StartCoroutine(Shake(0.5f, 0.6f));
                StartCoroutine(FadeImage(white, 1f, 2.2f));
                yield return Say("ومحا ذاكرة فرات... ورماه بعيدا، الى اقصى الشمال", 4.5f);
            }

            yield return Finish(1.5f);
        }

        IEnumerator Shatter()
        {
            Vector3 c = key != null ? key.position : furat.position + Vector3.up;
            if (key != null) key.gameObject.SetActive(false);
            HitFX.Hit(c, true, new Color(1f, 0.8f, 0.35f));
            var gold = key != null ? key.GetComponentInChildren<Renderer>()?.sharedMaterial : null;
            for (int i = 0; i < 18; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(shard.GetComponent<Collider>());
                shard.transform.position = c;
                shard.transform.localScale = new Vector3(0.12f, 0.12f, 0.3f);
                if (gold != null) shard.GetComponent<Renderer>().sharedMaterial = gold;
                var trail = shard.AddComponent<TrailRenderer>();
                trail.time = 0.8f;
                trail.startWidth = 0.12f; trail.endWidth = 0f;
                trail.sharedMaterial = gold;
                float a = i / 18f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0.9f + Random.value * 0.6f, Mathf.Sin(a)).normalized;
                StartCoroutine(Fly(shard.transform, dir * Random.Range(18f, 26f)));
            }
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Fly(Transform t, Vector3 velocity)
        {
            for (float k = 0f; k < 2.5f && t != null; k += Time.deltaTime)
            {
                t.position += velocity * Time.deltaTime;
                t.Rotate(400f * Time.deltaTime, 300f * Time.deltaTime, 0f);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        IEnumerator Finish(float fade)
        {
            done = true;
            StartCoroutine(FadeImage(black, 1f, fade));
            subGroup.alpha = 0f;
            yield return new WaitForSeconds(fade + 0.3f);
            if (AudioManager.Instance != null) AudioManager.Instance.BossMusic(false);
            SceneLoader.Load(nextScene);
        }

        // ---------------------------------------------------------------- helpers

        IEnumerator Say(string text, float seconds)
        {
            subtitle.text = ArabicText.Fix(text);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { subGroup.alpha = t / 0.6f; yield return null; }
            subGroup.alpha = 1f;
            yield return new WaitForSeconds(Mathf.Max(0f, seconds - 1.2f));
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { subGroup.alpha = 1f - t / 0.6f; yield return null; }
            subGroup.alpha = 0f;
        }

        IEnumerator Glide(Vector3 from, Vector3 to, Vector3 look, float seconds)
        {
            if (cam == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                cam.transform.position = Vector3.Lerp(from, to, k);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(look - cam.transform.position), 1f - Mathf.Exp(-4f * Time.deltaTime));
                yield return null;
            }
        }

        IEnumerator Follow(Transform who, Vector3 camPos, float seconds)
        {
            if (cam == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                cam.transform.position = Vector3.Lerp(cam.transform.position, camPos, 1f - Mathf.Exp(-1.5f * Time.deltaTime));
                Vector3 look = who.position + Vector3.up * 1.2f;
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(look - cam.transform.position), 1f - Mathf.Exp(-3f * Time.deltaTime));
                yield return null;
            }
        }

        IEnumerator Walk(Transform who, Vector3 from, Vector3 to, float speed)
        {
            var rig = who.GetComponent<HumanoidRig>();
            who.position = from;
            who.rotation = Quaternion.LookRotation(Flat(to - from));
            float dist = Vector3.Distance(from, to);
            for (float d = 0f; d < dist; d += speed * Time.deltaTime)
            {
                who.position = Vector3.Lerp(from, to, d / dist);
                if (rig != null) rig.cinematicSpeed = speed;
                yield return null;
            }
            who.position = to;
            if (rig != null) rig.cinematicSpeed = 0f;
        }

        IEnumerator Shake(float amount, float seconds)
        {
            if (cam == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                cam.transform.position += Random.insideUnitSphere * amount * (1f - t / seconds) * 0.15f;
                yield return null;
            }
        }

        IEnumerator FadeImage(Image img, float to, float seconds)
        {
            Color c = img.color;
            float from = c.a;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                c.a = Mathf.Lerp(from, to, t / seconds);
                img.color = c;
                yield return null;
            }
            c.a = to;
            img.color = c;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 0.0001f ? Vector3.forward : v; }

        void BuildOverlay()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Cinematic Canvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // letterbox bars
            for (int i = 0; i < 2; i++)
            {
                var bar = new GameObject("Bar", typeof(RectTransform)).GetComponent<RectTransform>();
                bar.SetParent(go.transform, false);
                bar.anchorMin = new Vector2(0f, i == 0 ? 0f : 1f);
                bar.anchorMax = new Vector2(1f, i == 0 ? 0f : 1f);
                bar.pivot = new Vector2(0.5f, i == 0 ? 0f : 1f);
                bar.sizeDelta = new Vector2(0f, 120f);
                bar.gameObject.AddComponent<Image>().color = Color.black;
            }

            var srt = new GameObject("Subtitle", typeof(RectTransform)).GetComponent<RectTransform>();
            srt.SetParent(go.transform, false);
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 60f);
            srt.sizeDelta = new Vector2(1700f, 100f);
            subtitle = srt.gameObject.AddComponent<Text>();
            subtitle.font = font;
            subtitle.fontSize = 36;
            subtitle.color = new Color(0.95f, 0.9f, 0.8f);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            subGroup = srt.gameObject.AddComponent<CanvasGroup>();
            subGroup.alpha = 0f;

            var hint = new GameObject("Skip", typeof(RectTransform)).GetComponent<RectTransform>();
            hint.SetParent(go.transform, false);
            hint.anchorMin = hint.anchorMax = new Vector2(1f, 1f);
            hint.pivot = new Vector2(1f, 1f);
            hint.anchoredPosition = new Vector2(-30f, -40f);
            hint.sizeDelta = new Vector2(400f, 40f);
            var ht = hint.gameObject.AddComponent<Text>();
            ht.font = font; ht.fontSize = 22; ht.color = new Color(1f, 1f, 1f, 0.45f);
            ht.alignment = TextAnchor.MiddleRight;
            ht.text = ArabicText.Fix("تخطي") + "  [Esc]";

            white = FullScreen(go.transform, new Color(1f, 1f, 1f, 0f));
            black = FullScreen(go.transform, Color.black);
        }

        static Image FullScreen(Transform parent, Color c)
        {
            var rt = new GameObject("Fade", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }
    }
}
