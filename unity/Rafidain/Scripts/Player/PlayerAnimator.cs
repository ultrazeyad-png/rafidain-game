using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Drives a real (Mixamo / humanoid) character's Animator from Furat's gameplay state.
    /// Added automatically by "Rafidain > Setup Furat Character".
    /// Animator parameters: Speed (float), Attack, Heavy, Dodge, Hit, Heal (triggers), Dead (bool).
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public Animator animator;
        public float speedDamp = 0.08f;

        PlayerController controller;
        PlayerCombat combat;
        PlayerStats stats;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int HeavyId = Animator.StringToHash("Heavy");
        static readonly int DodgeId = Animator.StringToHash("Dodge");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int HealId = Animator.StringToHash("Heal");
        static readonly int DeadId = Animator.StringToHash("Dead");

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            stats = GetComponent<PlayerStats>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void OnEnable()
        {
            if (controller != null)
            {
                controller.OnDodge += HandleDodge;
                controller.OnHealStart += HandleHeal;
                controller.OnStagger += HandleHit;
            }
            if (combat != null) combat.OnAttack += HandleAttack;
        }

        void OnDisable()
        {
            if (controller != null)
            {
                controller.OnDodge -= HandleDodge;
                controller.OnHealStart -= HandleHeal;
                controller.OnStagger -= HandleHit;
            }
            if (combat != null) combat.OnAttack -= HandleAttack;
        }

        void Update()
        {
            if (animator == null) return;
            float speed = controller != null ? controller.CurrentSpeed : 0f;
            animator.SetFloat(SpeedId, speed, speedDamp, Time.deltaTime);
            animator.SetBool(DeadId, stats != null && stats.IsDead);
        }

        void HandleAttack(bool heavy) { if (animator != null) animator.SetTrigger(heavy ? HeavyId : AttackId); }
        void HandleDodge() { if (animator != null) animator.SetTrigger(DodgeId); }
        void HandleHeal() { if (animator != null) animator.SetTrigger(HealId); }
        void HandleHit() { if (animator != null) animator.SetTrigger(HitId); }
    }
}
