using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Picks a graphics quality level for the device before the first scene loads:
    /// PC and strong phones get the high level, weaker phones get the light level.
    /// The player's own choice (saved in PlayerPrefs) always wins.
    /// </summary>
    public static class DeviceQuality
    {
        const string PrefKey = "rafidain_quality";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            int saved = PlayerPrefs.GetInt(PrefKey, -1);
            int level = (saved >= 0 && saved < QualitySettings.names.Length) ? saved : Detect();
            QualitySettings.SetQualityLevel(level, true);

            if (Application.isMobilePlatform)
                Application.targetFrameRate = level == HighIndex() ? 60 : 30;

            Debug.Log($"[Rafidain] Quality level: {QualitySettings.names[level]}");
        }

        public static void SetManual(int level)
        {
            PlayerPrefs.SetInt(PrefKey, level);
            QualitySettings.SetQualityLevel(level, true);
        }

        public static bool IsStrongPhone()
        {
            // ~8 GB RAM and 8 cores is roughly a recent mid/high-end Android phone
            return SystemInfo.systemMemorySize >= 7000 && SystemInfo.processorCount >= 8;
        }

        static int Detect()
        {
            if (!Application.isMobilePlatform) return HighIndex();
            return IsStrongPhone() ? HighIndex() : LowIndex();
        }

        static int HighIndex()
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i].ToLowerInvariant();
                if (n.Contains("pc") || n.Contains("ultra") || n.Contains("high")) return i;
            }
            return names.Length - 1;
        }

        static int LowIndex()
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i].ToLowerInvariant();
                if (n.Contains("mobile") || n.Contains("low") || n.Contains("performant")) return i;
            }
            return 0;
        }
    }
}
