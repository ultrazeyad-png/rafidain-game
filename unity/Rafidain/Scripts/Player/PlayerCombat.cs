using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rafidain
{
    /// <summary>Light and heavy attacks with windup / active / recovery timing, like souls games.</summary>
    public class PlayerCombat : MonoBehaviour
    {
        [System.Serializable]
        public class AttackData
        {
            public float damage = 22f;
            public float stamina = 14f;
            public float windup = 0.18f;
            public float active = 0.12f;
            public float recovery = 0.3f;
            public float range = 1.3f;
            public float radius = 0.9f;
            public float lunge = 1.0f;
        }

        public AttackData lightAttack = new AttackData();
        public AttackData heavyAttack = new AttackData
        {
            damage = 48f, stamina = 28f, windup = 0.55f, active = 0.15f, recovery = 0.6f, range = 1.5f, radius = 1.1f, lunge = 1.6f
        };

        [Tooltip("Pivot that holds the weapon; swung during attacks until real animations are added.")]
        public Transform weaponPivot;

        [Tooltip("True when the weapon is held by an animated character's hand (no code swing).")]
        public bool animatedWeapon;

        public bool IsAttacking { get; private set; }
        public event System.Action<bool> OnAttack; // true = heavy

        PlayerStats stats;
        PlayerController controller;
        CharacterController cc;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        readonly Collider[] overlap = new Collider[16];

        void Awake()
        {
            stats = GetComponent<PlayerStats>();
            controller = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();
        }

        public Weapons.Def Current { get; private set; } = Weapons.Sword;
        Transform maceHead;

        void Start()
        {
            var d = SaveSystem.Data;
            if (d.weapons == null || d.weapons.Count == 0) d.weapons = new List<string> { "sword" };
            Equip(string.IsNullOrEmpty(d.equippedWeapon) ? "sword" : d.equippedWeapon);
        }

        /// <summary>Switch weapon: new attack timings and a new look for the blade.</summary>
        public void Equip(string id)
        {
            Current = Weapons.Get(id);
            lightAttack = Current.light;
            heavyAttack = Current.heavy;
            SaveSystem.Data.equippedWeapon = Current.id;

            if (weaponPivot == null) return;
            var blade = weaponPivot.Find("Blade");
            if (blade != null)
            {
                blade.localScale = new Vector3(Current.bladeWidth, Current.maceHead ? Current.bladeWidth : 0.12f, Current.bladeLength);
                blade.localPosition = new Vector3(0f, 0f, Current.bladeLength * 0.5f);
            }
            // the head on the end of the shaft (mace ball, axe blade, spear point, sickle hook)
            if (maceHead != null) { Destroy(maceHead.gameObject); maceHead = null; }
            var shape = Current.head;
            if (shape == Weapons.Head.None && Current.maceHead) shape = Weapons.Head.Mace;
            if (shape != Weapons.Head.None && blade != null)
            {
                var go = GameObject.CreatePrimitive(shape == Weapons.Head.Mace ? PrimitiveType.Sphere : PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.name = "WeaponHead";
                maceHead = go.transform;
                maceHead.SetParent(weaponPivot, false);
                var r = blade.GetComponent<Renderer>();
                if (r != null) go.GetComponent<Renderer>().sharedMaterial = r.sharedMaterial;
                float L = Current.bladeLength;
                switch (shape)
                {
                    case Weapons.Head.Mace:
                        maceHead.localPosition = new Vector3(0f, 0f, L); maceHead.localScale = Vector3.one * 0.3f; break;
                    case Weapons.Head.Axe:
                        maceHead.localPosition = new Vector3(0f, 0.16f, L * 0.88f); maceHead.localScale = new Vector3(0.04f, 0.38f, 0.26f); break;
                    case Weapons.Head.Spear:
                        maceHead.localPosition = new Vector3(0f, 0f, L + 0.15f); maceHead.localScale = new Vector3(0.1f, 0.03f, 0.34f);
                        maceHead.localRotation = Quaternion.Euler(0f, 45f, 0f); break;
                    case Weapons.Head.Sickle:
                        maceHead.localPosition = new Vector3(0f, 0.18f, L + 0.12f); maceHead.localScale = new Vector3(0.04f, 0.1f, 0.5f);
                        maceHead.localRotation = Quaternion.Euler(-55f, 0f, 0f); break;
                }
            }
        }

        /// <summary>Next weapon the player owns (used at the flame).</summary>
        public void CycleWeapon()
        {
            var owned = SaveSystem.Data.weapons;
            if (owned == null || owned.Count == 0) return;
            int i = owned.IndexOf(Current.id);
            Equip(owned[(i + 1) % owned.Count]);
        }

        void Update()
        {
            var input = PlayerInputReader.Instance;
            if (input == null || stats.IsDead || IsAttacking) return;
            if (controller != null && (controller.IsDodging || controller.IsHealing || controller.IsStaggered)) return;

            // a staggered enemy in reach can be executed (Bloodborne visceral)
            var victim = EnemyAI.RiposteTarget(transform.position, 2.6f);
            if (victim != null)
            {
                if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("اعدام") + "   [LMB]");
                if (input.Consume(GameAction.Attack) || input.Consume(GameAction.HeavyAttack))
                {
                    StartCoroutine(Riposte(victim));
                    return;
                }
            }

            if (input.Consume(GameAction.HeavyAttack)) TryAttack(heavyAttack);
            else if (input.Consume(GameAction.Attack)) TryAttack(lightAttack);
        }

        [Header("Visceral")]
        public float riposteDamage = 140f;

        IEnumerator Riposte(EnemyAI victim)
        {
            IsAttacking = true;
            OnAttack?.Invoke(true);
            Vector3 to = victim.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
            float t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; SetSwing(Mathf.Lerp(0f, -1f, t / 0.3f)); yield return null; }
            float dmg = (riposteDamage * stats.DamageMultiplier + (victim.isBoss ? victim.maxHealth * 0.12f : victim.maxHealth * 0.6f)) * Current.riposteMul;
            victim.TakeRiposte(dmg, transform.position);
            HitFX.Hit(victim.transform.position + Vector3.up * 0.5f, true, new Color(0.8f, 0.1f, 0.06f));
            t = 0f;
            while (t < 0.15f) { t += Time.deltaTime; SetSwing(Mathf.Lerp(-1f, 1f, t / 0.15f)); yield return null; }
            yield return new WaitForSeconds(0.45f);
            EndAttack();
        }

        void TryAttack(AttackData a)
        {
            if (!stats.TryUseStamina(a.stamina)) return;
            OnAttack?.Invoke(a == heavyAttack);
            if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.swing, transform.position, a == heavyAttack ? 1f : 0.7f);
            StartCoroutine(AttackRoutine(a));
        }

        IEnumerator AttackRoutine(AttackData a)
        {
            IsAttacking = true;
            hitThisSwing.Clear();

            // Windup: weapon raised
            float t = 0f;
            while (t < a.windup)
            {
                t += Time.deltaTime;
                SetSwing(Mathf.Lerp(0f, -1f, t / a.windup));
                if (stats.IsDead) { EndAttack(); yield break; }
                yield return null;
            }

            // Active: swing + hit check + small lunge
            t = 0f;
            while (t < a.active)
            {
                t += Time.deltaTime;
                SetSwing(Mathf.Lerp(-1f, 1f, t / a.active));
                if (cc != null && cc.enabled) cc.Move(transform.forward * (a.lunge / a.active) * Time.deltaTime);
                DoHit(a);
                yield return null;
            }

            // Recovery
            t = 0f;
            while (t < a.recovery)
            {
                t += Time.deltaTime;
                SetSwing(Mathf.Lerp(1f, 0f, t / a.recovery));
                yield return null;
            }

            EndAttack();
        }

        void EndAttack()
        {
            SetSwing(0f);
            IsAttacking = false;
        }

        void DoHit(AttackData a)
        {
            Vector3 center = transform.position + transform.forward * a.range;
            int count = Physics.OverlapSphereNonAlloc(center, a.radius, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = overlap[i];
                if (col.transform.root == transform.root) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead || hitThisSwing.Contains(target)) continue;
                hitThisSwing.Add(target);
                target.TakeDamage(a.damage * stats.DamageMultiplier, transform.position);
            }
        }

        // -1 = raised, 0 = rest, 1 = follow-through
        void SetSwing(float s)
        {
            if (weaponPivot == null || animatedWeapon) return;
            weaponPivot.localRotation = Quaternion.Euler(20f, s * 75f, 0f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * lightAttack.range, lightAttack.radius);
        }
    }
}
