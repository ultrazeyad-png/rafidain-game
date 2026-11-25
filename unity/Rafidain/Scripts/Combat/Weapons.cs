using UnityEngine;

namespace Rafidain
{
    /// <summary>All of Furat's weapons. Each has its own light / heavy attack timing and its own look.</summary>
    public static class Weapons
    {
        public class Def
        {
            public string id, name, description;
            public PlayerCombat.AttackData light, heavy;
            public float bladeLength, bladeWidth;
            public bool maceHead;
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
            riposteMul = 1.35f,
        };

        public static readonly Def Mace = new Def
        {
            id = "mace", name = "مكوار كركوك", description = "مكوار ثقيل من حجر النار... بطيء، يكسر الصد والتوازن",
            light = new PlayerCombat.AttackData { damage = 34f, stamina = 20f, windup = 0.32f, active = 0.14f, recovery = 0.45f, range = 1.4f, radius = 1.0f, lunge = 0.8f },
            heavy = new PlayerCombat.AttackData { damage = 78f, stamina = 38f, windup = 0.85f, active = 0.18f, recovery = 0.8f, range = 1.6f, radius = 1.4f, lunge = 1.2f },
            bladeLength = 1.0f, bladeWidth = 0.09f,
            maceHead = true,
        };

        public static readonly Def[] All = { Sword, Khanjar, Mace };

        public static Def Get(string id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return Sword;
        }
    }
}
