using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rafidain
{
    /// <summary>Plays once after Adapa falls: the ending told over black.</summary>
    public class EndingSequence : MonoBehaviour
    {
        public string finalBossId = "boss_adapa";

        static readonly string[] Lines =
        {
            "سقط ادابا... وسكت الباب",
            "ومن العتمة خرجت ارشكيكال، ملكة العالم السفلي",
            "قالت: كل من يعبر الباب يدفع ثمنا",
            "فمد فرات يده... ولمس يد دجلة",
            "ولكن الحكاية لم تنته بعد",
        };

        bool played;

        void Update()
        {
            if (played) return;
            var d = SaveSystem.Data;
            if (!d.defeatedBosses.Contains(finalBossId) || d.endingSeen) return;
            played = true;
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            yield return new WaitForSeconds(4f);
            var go = new GameObject("Ending Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var bgRt = new GameObject("Black", typeof(RectTransform)).GetComponent<RectTransform>();
            bgRt.SetParent(go.transform, false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            var tRt = new GameObject("Line", typeof(RectTransform)).GetComponent<RectTransform>();
            tRt.SetParent(go.transform, false);
            tRt.sizeDelta = new Vector2(1700, 200);
            var text = tRt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 46;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = new Color(0.92f, 0.86f, 0.74f, 0f);

            for (float t = 0; t < 2f; t += Time.deltaTime) { bg.color = new Color(0, 0, 0, t / 2f); yield return null; }
            foreach (var l in Lines)
            {
                text.text = ArabicText.Fix(l);
                for (float t = 0; t < 1f; t += Time.deltaTime) { var c = text.color; c.a = t; text.color = c; yield return null; }
                yield return new WaitForSeconds(3f);
                for (float t = 0; t < 1f; t += Time.deltaTime) { var c = text.color; c.a = 1f - t; text.color = c; yield return null; }
            }
            text.text = ArabicText.Fix("وادي الرافدين") + "\n" + ArabicText.Fix("يتبع...");
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { var c = text.color; c.a = t / 1.5f; text.color = c; yield return null; }
            SaveSystem.Data.endingSeen = true;
            SaveSystem.Save();
        }
    }
}
