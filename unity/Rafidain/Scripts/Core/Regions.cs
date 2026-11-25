namespace Rafidain
{
    /// <summary>The road from Zakho to Baghdad through all 18 governorates (19 maps: Zakho and Duhok are both in Duhok governorate).</summary>
    public static class Regions
    {
        public static readonly string[] Scenes =
        {
            "Zakho_Region", "Duhok_Region", "Mosul_Region", "Erbil_Region", "Sulaymaniyah_Region", "Kirkuk_Region",
            "Samarra_Region", "Anbar_Region", "Karbala_Region", "Najaf_Region", "Qadisiyah_Region", "Muthanna_Region",
            "Basra_Region", "Maysan_Region", "DhiQar_Region", "Wasit_Region", "Babil_Region", "Diyala_Region", "Baghdad_Region",
        };

        public static readonly string[] Names =
        {
            "زاخو", "دهوك", "الموصل", "اربيل", "السليمانية", "كركوك",
            "سامراء", "الانبار", "كربلاء", "النجف", "القادسية", "المثنى",
            "البصرة", "ميسان", "ذي قار", "واسط", "بابل", "ديالى", "بغداد",
        };

        /// <summary>Guardians before Baghdad: each one drops a key to the gate of the underworld.</summary>
        public const int KeysNeeded = 18;
        public static int Count => Scenes.Length;

        public static string NameOf(string scene)
        {
            for (int i = 0; i < Scenes.Length; i++) if (Scenes[i] == scene) return Names[i];
            return scene == "MainMenu" ? "وادي الرافدين" : scene;
        }
    }
}
