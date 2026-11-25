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

        /// <summary>What Dijla whispers from beneath the earth the first time Furat reaches each governorate.</summary>
        public static readonly string[] Whispers =
        {
            "فرات... انا هنا، تحت الارض. اتبع النهر",
            "الماء المحبوس خلف السد يشبهني... لا تتأخر",
            "نينوى رأت ملوكا كثيرين... وانت لست ملكا، انت زوجي",
            "القلعة صمدت سبعة الاف عام... اصمد مثلها",
            "الجبال باردة يا فرات... وانا ابرد",
            "النار لا تنطفئ هنا... وكذلك انتظاري",
            "انظر من فوق الملوية... سترى الطريق الطويل امامك",
            "الفرات اسمك ودجلة اسمي... ولا يلتقي النهران الا في الجنوب",
            "الصبر هنا له معنى... تعلمه",
            "البحر صار ملحا من طول الانتظار... لا تجعلني ملحا",
            "الحقول تعطي من يصبر... وانا صبرت",
            "كلكامش بحث عن الخلود... وانا لا اريد الا ان تعود",
            "هنا يلتقي النهران يا فرات... هنا كان يجب ان نلتقي",
            "الاهوار تحفظ اسرار السومريين... وتحفظ صوتي",
            "هنا كتب اول حرف... اكتب اسمي على قلبك",
            "دجلة يلتف هنا كالذراع... كأنه يعانقك عني",
            "بابل سقطت ثم بقيت... لا تسقط",
            "اشم رائحة البرتقال... بغداد قريبة",
            "انا تحتك الان يا فرات... افتح الباب",
        };

        public static int IndexOf(string scene)
        {
            for (int i = 0; i < Scenes.Length; i++) if (Scenes[i] == scene) return i;
            return -1;
        }

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
