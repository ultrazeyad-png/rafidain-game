using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Drives a real enemy model's Animator from EnemyAI.
    /// Added automatically by "Rafidain > Setup Enemy Characters (Mixamo)".
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAnimator : MonoBehaviour
    {
        public Animator animator;
        EnemyAI ai;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");

        void Awake()
        {
            ai = GetComponent<EnemyAI>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            ai.OnAttackStart += () => Trigger(AttackId);
            ai.OnHitTaken += () => { if (!ai.IsDead) Trigger(HitId); };
            ai.OnDied += () => { if (animator != null) animator.SetBool(DeadId, true); };
            ai.OnReset += () =>
            {
                if (animator == null) return;
                animator.SetBool(DeadId, false);
                animator.Rebind();
            };
        }

        void Update()
        {
            if (animator != null) animator.SetFloat(SpeedId, ai.CurrentSpeed, 0.1f, Time.deltaTime);
        }

        void Trigger(int id)
        {
            if (animator != null && animator.isActiveAndEnabled) animator.SetTrigger(id);
        }
    }
}
