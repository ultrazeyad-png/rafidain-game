using UnityEngine;

namespace Rafidain
{
    /// <summary>All of Furat's weapons. Each has its own light / heavy attack timing and its own look.</summary>
    public static class Weapons
    {
        public enum Head { None, Mace, Axe, Spear, Sickle }

        public class Def
        {
            public string id, name, description;
            public PlayerCombat.AttackData light, heavy;
            public float bladeLength, bladeWidth;
            public bool maceHead;
            public Head head = Head.None;     // extra shape on the end of the shaft
            public string region = "";        // where it is found
            public float riposteMul = 1f;
        }

        public static readonly Def Sword = new Def
        {
            id = "sword", name = "سيف فرات", description = "سيف من برونز اشور... متوازن",
            light = new PlayerCombat.AttackData(),
            heavy = new PlayerCombat.AttackData { damage = 48f, stamina = 28f, windup = 0.55f, active = 0.15f, recovery = 0.6f, range = 1.5f, radius = 1.1f, lunge = 1.6f },
            bladeLength = 1.1f, bladeWidth = 0.07f,
        };

        public static readonly Def Khanjar = new Def
        {
            id = "khanjar", name = "خنجر الموصل", description = "خنجر معقوف سريع... ضربه قليل لكن لا يتوقف، والاعدام به قاتل",
            light = new PlayerCombat.AttackData { damage = 14f, stamina = 9f, windup = 0.09f, active = 0.09f, recovery = 0.17f, range = 1.0f, radius = 0.75f, lunge = 1.3f },
            heavy = new PlayerCombat.AttackData { damage = 30f, stamina = 18f, windup = 0.32f, active = 0.12f, recovery = 0.38f, range = 1.3f, radius = 0.85f, lunge = 2.4f },
            bladeLength = 0.55f, bladeWidth = 0.05f,
            riposteMul = 1.35f, region = "mosul",
        };

        public static readonly Def Mace = new Def
        {
            id = "mace", name = "مكوار كركوك", description = "مكوار ثقيل من حجر النار... بطيء، يكسر الصد والتوازن",
            light = new PlayerCombat.AttackData { damage = 34f, stamina = 20f, windup = 0.32f, active = 0.14f, recovery = 0.45f, range = 1.4f, radius = 1.0f, lunge = 0.8f },
            heavy = new PlayerCombat.AttackData { damage = 78f, stamina = 38f, windup = 0.85f, active = 0.18f, recovery = 0.8f, range = 1.6f, radius = 1.4f, lunge = 1.2f },
            bladeLength = 1.0f, bladeWidth = 0.09f,
            maceHead = true, head = Head.Mace, region = "kirkuk",
        };

        public static readonly Def Abbasid = new Def
        {
            id = "abbasid", name = "سيف سامراء العباسي", description = "سيف من ايام كانت سامراء عاصمة الدنيا... اسرع من سيفك واحد",
            light = new PlayerCombat.AttackData { damage = 25f, stamina = 13f, windup = 0.15f, active = 0.11f, recovery = 0.26f, range = 1.4f, radius = 0.9f, lunge = 1.2f },
            heavy = new PlayerCombat.AttackData { damage = 52f, stamina = 27f, windup = 0.5f, active = 0.14f, recovery = 0.55f, range = 1.6f, radius = 1.1f, lunge = 1.8f },
            bladeLength = 1.2f, bladeWidth = 0.06f, region = "samarra",
        };

        public static readonly Def Staff = new Def
        {
            id = "staff", name = "عصا الراعي", description = "عصا من بلوط جبال السليمانية... طويلة وخفيفة، تبعد الاعداء عنك",
            light = new PlayerCombat.AttackData { damage = 15f, stamina = 10f, windup = 0.14f, active = 0.14f, recovery = 0.24f, range = 2.0f, radius = 1.2f, lunge = 0.6f },
            heavy = new PlayerCombat.AttackData { damage = 34f, stamina = 22f, windup = 0.45f, active = 0.2f, recovery = 0.5f, range = 2.2f, radius = 1.6f, lunge = 0.8f },
            bladeLength = 1.9f, bladeWidth = 0.06f, region = "sulaymaniyah",
        };

        public static readonly Def Spear = new Def
        {
            id = "spear", name = "رمح الوركاء", description = "رمح سومري من ايام كلكامش... اطول سلاح، يطعن من بعيد لكن بخط ضيق",
            light = new PlayerCombat.AttackData { damage = 22f, stamina = 14f, windup = 0.2f, active = 0.12f, recovery = 0.32f, range = 2.4f, radius = 0.55f, lunge = 1.4f },
            heavy = new PlayerCombat.AttackData { damage = 50f, stamina = 28f, windup = 0.55f, active = 0.14f, recovery = 0.6f, range = 2.8f, radius = 0.6f, lunge = 2.6f },
            bladeLength = 2.2f, bladeWidth = 0.045f, head = Head.Spear, region = "muthanna",
        };

        public static readonly Def Sickle = new Def
        {
            id = "sickle", name = "منجل اور", description = "سيف معقوف مثل الهلال من مقابر اور الملكية... ضربة عريضة تلم اكثر من عدو",
            light = new PlayerCombat.AttackData { damage = 20f, stamina = 12f, windup = 0.14f, active = 0.13f, recovery = 0.28f, range = 1.3f, radius = 1.4f, lunge = 1.0f },
            heavy = new PlayerCombat.AttackData { damage = 44f, stamina = 25f, windup = 0.48f, active = 0.18f, recovery = 0.55f, range = 1.5f, radius = 1.8f, lunge = 1.4f },
            bladeLength = 0.8f, bladeWidth = 0.05f, head = Head.Sickle, region = "dhiqar",
        };

        public static readonly Def Axe = new Def
        {
            id = "axe", name = "فأس بابل", description = "فأس حرب من برونز بابل... ثقيل وضربته الثقيلة تكسر التوازن",
            light = new PlayerCombat.AttackData { damage = 30f, stamina = 18f, windup = 0.26f, active = 0.13f, recovery = 0.4f, range = 1.4f, radius = 1.1f, lunge = 0.9f },
            heavy = new PlayerCombat.AttackData { damage = 70f, stamina = 34f, windup = 0.75f, active = 0.17f, recovery = 0.72f, range = 1.6f, radius = 1.3f, lunge = 1.3f },
            bladeLength = 1.1f, bladeWidth = 0.07f, head = Head.Axe, region = "babil",
        };

        public static readonly Def[] All = { Sword, Khanjar, Mace, Abbasid, Staff, Spear, Sickle, Axe };

        /// <summary>The weapon waiting in this governorate, or null.</summary>
        public static Def InRegion(string regionId)
        {
            foreach (var d in All) if (d.region == regionId) return d;
            return null;
        }

        public static Def Get(string id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return Sword;
        }
    }
}
