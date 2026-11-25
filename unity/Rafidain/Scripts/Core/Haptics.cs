using UnityEngine;

namespace Rafidain
{
    /// <summary>Short phone vibration on heavy moments (taking a hit, guard break, parry, death). Off on PC.</summary>
    public static class Haptics
    {
        static float last;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("rafidain_haptics", 1) == 1;
            set => PlayerPrefs.SetInt("rafidain_haptics", value ? 1 : 0);
        }

        public static void Pulse()
        {
            if (!Enabled || Time.unscaledTime - last < 0.25f) return;
            last = Time.unscaledTime;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
