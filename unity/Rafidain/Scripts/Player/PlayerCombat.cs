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
            float dmg = riposteDamage * stats.DamageMultiplier + (victim.isBoss ? victim.maxHealth * 0.12f : victim.maxHealth * 0.6f);
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
