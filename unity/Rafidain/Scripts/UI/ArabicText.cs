using System.Collections.Generic;
using System.Text;

namespace Rafidain
{
    /// <summary>
    /// Makes Arabic display correctly in Unity's UI Text: joins letters into their connected forms
    /// (Arabic Presentation Forms-B) and lays the line out right-to-left.
    /// Usage: label.text = ArabicText.Fix("اشتعلت الشعلة");
    /// Keep digits and Latin key hints outside Fix() and place them yourself.
    /// </summary>
    public static class ArabicText
    {
        // isolated, final, initial, medial (0 = form does not exist)
        static readonly Dictionary<char, int[]> Forms = new Dictionary<char, int[]>
        {
            { 'ء', new[] { 0xFE80, 0, 0, 0 } },
            { 'آ', new[] { 0xFE81, 0xFE82, 0, 0 } },
            { 'أ', new[] { 0xFE83, 0xFE84, 0, 0 } },
            { 'ؤ', new[] { 0xFE85, 0xFE86, 0, 0 } },
            { 'إ', new[] { 0xFE87, 0xFE88, 0, 0 } },
            { 'ئ', new[] { 0xFE89, 0xFE8A, 0xFE8B, 0xFE8C } },
            { 'ا', new[] { 0xFE8D, 0xFE8E, 0, 0 } },
            { 'ب', new[] { 0xFE8F, 0xFE90, 0xFE91, 0xFE92 } },
            { 'ة', new[] { 0xFE93, 0xFE94, 0, 0 } },
            { 'ت', new[] { 0xFE95, 0xFE96, 0xFE97, 0xFE98 } },
            { 'ث', new[] { 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C } },
            { 'ج', new[] { 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0 } },
            { 'ح', new[] { 0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4 } },
            { 'خ', new[] { 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8 } },
            { 'د', new[] { 0xFEA9, 0xFEAA, 0, 0 } },
            { 'ذ', new[] { 0xFEAB, 0xFEAC, 0, 0 } },
            { 'ر', new[] { 0xFEAD, 0xFEAE, 0, 0 } },
            { 'ز', new[] { 0xFEAF, 0xFEB0, 0, 0 } },
            { 'س', new[] { 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4 } },
            { 'ش', new[] { 0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8 } },
            { 'ص', new[] { 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC } },
            { 'ض', new[] { 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0 } },
            { 'ط', new[] { 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4 } },
            { 'ظ', new[] { 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8 } },
            { 'ع', new[] { 0xFEC9, 0xFECA, 0xFECB, 0xFECC } },
            { 'غ', new[] { 0xFECD, 0xFECE, 0xFECF, 0xFED0 } },
            { 'ف', new[] { 0xFED1, 0xFED2, 0xFED3, 0xFED4 } },
            { 'ق', new[] { 0xFED5, 0xFED6, 0xFED7, 0xFED8 } },
            { 'ك', new[] { 0xFED9, 0xFEDA, 0xFEDB, 0xFEDC } },
            { 'ل', new[] { 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0 } },
            { 'م', new[] { 0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4 } },
            { 'ن', new[] { 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8 } },
            { 'ه', new[] { 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC } },
            { 'و', new[] { 0xFEED, 0xFEEE, 0, 0 } },
            { 'ى', new[] { 0xFEEF, 0xFEF0, 0, 0 } },
            { 'ي', new[] { 0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4 } },
        };

        // lam + alef ligatures: isolated, final
        static readonly Dictionary<char, int[]> LamAlef = new Dictionary<char, int[]>
        {
            { 'آ', new[] { 0xFEF5, 0xFEF6 } },
            { 'أ', new[] { 0xFEF7, 0xFEF8 } },
            { 'إ', new[] { 0xFEF9, 0xFEFA } },
            { 'ا', new[] { 0xFEFB, 0xFEFC } },
        };

        static bool IsArabic(char c) => Forms.ContainsKey(c);
        static bool JoinsForward(char c) => Forms.TryGetValue(c, out var f) && f[2] != 0;

        public static string Fix(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = Visual(Shape(lines[i]));
            return string.Join("\n", lines);
        }

        static string Shape(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!IsArabic(c)) { sb.Append(c); continue; }

                bool prevJoins = i > 0 && JoinsForward(s[i - 1]);
                char next = i + 1 < s.Length ? s[i + 1] : '\0';

                if (c == 'ل' && LamAlef.TryGetValue(next, out var la))
                {
                    sb.Append((char)(prevJoins ? la[1] : la[0]));
                    i++;
                    continue;
                }

                var f = Forms[c];
                bool nextJoins = IsArabic(next) && f[2] != 0;
                int code;
                if (prevJoins && nextJoins) code = f[3];
                else if (prevJoins) code = f[1] != 0 ? f[1] : f[0];
                else if (nextJoins) code = f[2];
                else code = f[0];
                sb.Append((char)code);
            }
            return sb.ToString();
        }

        static bool IsLtr(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        // Reverse for right-to-left display, keeping digit/Latin runs readable.
        static string Visual(string s)
        {
            var runs = new List<string>();
            var cur = new StringBuilder();
            bool curLtr = false;
            foreach (char c in s)
            {
                bool ltr = IsLtr(c);
                if (cur.Length > 0 && ltr != curLtr)
                {
                    runs.Add(curLtr ? cur.ToString() : Reverse(cur.ToString()));
                    cur.Clear();
                }
                curLtr = ltr;
                cur.Append(c);
            }
            if (cur.Length > 0) runs.Add(curLtr ? cur.ToString() : Reverse(cur.ToString()));
            runs.Reverse();
            return string.Concat(runs);
        }

        static string Reverse(string s)
        {
            var arr = s.ToCharArray();
            System.Array.Reverse(arr);
            for (int i = 0; i < arr.Length; i++)
            {
                switch (arr[i])
                {
                    case '(': arr[i] = ')'; break;
                    case ')': arr[i] = '('; break;
                    case '[': arr[i] = ']'; break;
                    case ']': arr[i] = '['; break;
                }
            }
            return new string(arr);
        }
    }
}
