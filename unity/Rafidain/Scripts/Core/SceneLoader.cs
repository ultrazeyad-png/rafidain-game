using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>
    /// Loading screen between governorates: the region's name, a line of lore or advice, and a slow ember bar.
    /// Use SceneLoader.Load(sceneName) instead of SceneManager.LoadScene.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        static SceneLoader busy;

        static readonly string[] Tips =
        {
            "ارفع درعك في اخر لحظة... فالحارس الذي يضربك في الفراغ يترنح",
            "الجمر الذي يسقط عند هلاكك ينتظرك مرة واحدة فقط",
            "الشعلة تعيد صحتك... وتعيد اعداءك ايضا",
            "الكلاب السائبة سريعة لكنها هشة... لا تهرب منها، واجهها",
            "الرماة يخافون من يقترب... تدحرج نحوهم لا بعيدا عنهم",
            "قطع الخريطة لا تكون على الطريق ابدا",
            "من زاخو الى بغداد... ثماني عشرة محافظة، ولكل واحدة حارس",
            "في كل محافظة جرة زيت مقدس... تزيد قواريرك واحدة",
            "الخنجر سريع والاعدام به قاتل... والمكوار بطيء لكنه يكسر التوازن",
            "ثماني عشرة قطعة من المفتاح... اجمعها تفتح الباب تحت بغداد، وخلفه دجلة",
            "كل حارس يغضب حين ينتصف عمره... احذر ضرباته المزدوجة",
            "اقرأ الالواح المضيئة على الارض... تركها من مشى الطريق قبلك",
        };

        public static string RegionName(string scene) => Regions.NameOf(scene);

        public static void Load(string scene)
        {
            if (busy != null) return;
            var go = new GameObject("_Loading");
            DontDestroyOnLoad(go);
            busy = go.AddComponent<SceneLoader>();
            busy.StartCoroutine(busy.Run(scene));
        }

        CanvasGroup group;
        RectTransform fill;

        IEnumerator Run(string scene)
        {
            Build(RegionName(scene), Tips[Random.Range(0, Tips.Length)]);
            Time.timeScale = 1f;
            yield return FadeTo(1f, 0.6f);

            var op = SceneManager.LoadSceneAsync(scene);
            if (op == null) { busy = null; Destroy(gameObject); yield break; }
            op.allowSceneActivation = false;
            float shown = 0f, minTime = 2.2f, t = 0f;
            while (op.progress < 0.9f || t < minTime)
            {
                t += Time.unscaledDeltaTime;
                float target = Mathf.Min(op.progress / 0.9f, t / minTime);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
                fill.anchorMax = new Vector2(shown, 1f);
                yield return null;
            }
            fill.anchorMax = Vector2.one;
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            yield return null;
            yield return FadeTo(0f, 0.9f);
            busy = null;
            Destroy(gameObject);
        }

        IEnumerator FadeTo(float to, float seconds)
        {
            float from = group.alpha, t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            group.alpha = to;
        }

        void Build(string region, string tip)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;

            var bg = Rect("Black", Vector2.zero, Vector2.zero);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.015f, 0.01f);

            Label(ArabicText.Fix(region), new Vector2(0, 80), 96, new Color(0.95f, 0.8f, 0.5f), font).fontStyle = FontStyle.Bold;
            Label(ArabicText.Fix(tip), new Vector2(0, -60), 30, new Color(0.85f, 0.8f, 0.72f), font);

            var bar = Rect("Bar", new Vector2(0, -380), new Vector2(520, 4));
            bar.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            fill = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            fill.SetParent(bar, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = new Color(1f, 0.6f, 0.25f, 0.9f);
        }

        RectTransform Rect(string name, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Text Label(string text, Vector2 pos, int size, Color color, Font font)
        {
            var rt = Rect("Text", pos, new Vector2(1700, 140));
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }
    }
}
