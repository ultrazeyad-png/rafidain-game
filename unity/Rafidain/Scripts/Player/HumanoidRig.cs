using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Procedural animation for the built-in humanoid bodies (before real Mixamo characters are added):
    /// walking/running stride, arm swing, coat/cape sway, dodge roll, hit flinch, attack wind-up and death fall.
    /// Works for Furat (PlayerController) and enemies (EnemyAI).
    /// </summary>
    public class HumanoidRig : MonoBehaviour
    {
        public Transform body;
        public Transform legL, legR, armL, armR, cape;
        public float strideAngle = 34f;
        public float cyclesPerMeter = 0.33f;
        [Tooltip("Lies on the ground (used by the opening scene: Furat asleep by the flame).")]
        public bool lyingDown;

        PlayerController pc;
        PlayerCombat combat;
        PlayerStats stats;
        EnemyAI ai;

        Vector3 bodyBase;
        Quaternion capeBase = Quaternion.identity;
        float phase;
        float rollT = -1f;
        float hitT;
        float attackT;
        float death;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            stats = GetComponent<PlayerStats>();
            ai = GetComponent<EnemyAI>();
            if (body != null) bodyBase = body.localPosition;
            if (cape != null) capeBase = cape.localRotation;

            if (pc != null)
            {
                pc.OnDodge += () => rollT = 0f;
                pc.OnStagger += () => hitT = 0.3f;
            }
            if (ai != null)
            {
                ai.OnHitTaken += () => hitT = 0.22f;
                ai.OnAttackStart += () => attackT = ai.windup + ai.activeTime;
                ai.OnReset += () => death = 0f;
            }
        }

        void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            float speed = pc != null ? pc.CurrentSpeed : ai != null ? ai.CurrentSpeed : 0f;
            bool dead = lyingDown || (stats != null && stats.IsDead) || (ai != null && ai.IsDead);

            float prevPhase = phase;
            phase += speed * cyclesPerMeter * Mathf.PI * 2f * dt;
            // a footstep every half stride (only Furat, to keep the soundscape clean)
            if (pc != null && speed > 0.8f && Mathf.FloorToInt(prevPhase / Mathf.PI) != Mathf.FloorToInt(phase / Mathf.PI)
                && AudioManager.Instance != null)
                AudioManager.At(AudioManager.Instance.step, transform.position + Vector3.down * 0.9f, 0.45f, 0.12f);
            float amt = Mathf.Clamp01(speed / 4.5f);
            float swing = Mathf.Sin(phase) * strideAngle * amt;

            if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (armL != null)
            {
                bool blocking = pc != null && pc.IsBlocking;
                var t = blocking ? Quaternion.Euler(-80f, 25f, -35f) : Quaternion.Euler(-swing * 0.8f, 0f, -6f);
                armL.localRotation = Quaternion.Slerp(armL.localRotation, t, 1f - Mathf.Exp(-20f * dt));
            }

            // right arm: swings while walking, raised while attacking
            if (armR != null)
            {
                Quaternion target;
                bool playerAttacking = combat != null && combat.IsAttacking;
                if (playerAttacking) target = Quaternion.Euler(-75f, 0f, 8f);
                else if (attackT > 0f)
                {
                    float total = ai != null ? ai.windup + ai.activeTime : 1f;
                    float k = 1f - attackT / total;            // 0 → 1 over the attack
                    target = k < 0.8f ? Quaternion.Euler(-130f * (k / 0.8f), 0f, 10f) : Quaternion.Euler(50f, 0f, 6f);
                }
                else target = Quaternion.Euler(swing * 0.8f, 0f, 6f);
                armR.localRotation = Quaternion.Slerp(armR.localRotation, target, 1f - Mathf.Exp(-18f * dt));
            }
            if (attackT > 0f) attackT -= dt;

            // body: bob + forward lean when running
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * amt;
            Quaternion lean = Quaternion.Euler(amt * 9f, 0f, 0f);

            Quaternion roll = Quaternion.identity;
            Vector3 rollDrop = Vector3.zero;
            if (rollT >= 0f)
            {
                rollT += dt;
                float k = Mathf.Clamp01(rollT / 0.5f);
                roll = Quaternion.Euler(360f * Mathf.SmoothStep(0f, 1f, k), 0f, 0f);
                rollDrop = Vector3.down * Mathf.Sin(k * Mathf.PI) * 0.45f;
                if (k >= 1f) rollT = -1f;
            }

            // staggered enemy: sinks to one knee, open for execution
            if (ai != null && ai.IsStaggered)
            {
                body.localRotation = Quaternion.Euler(28f, 0f, 0f);
                body.localPosition = bodyBase + Vector3.down * 0.35f;
                return;
            }

            Quaternion flinch = Quaternion.identity;
            if (hitT > 0f)
            {
                hitT -= dt;
                flinch = Quaternion.Euler(-14f * (hitT / 0.3f), 0f, 0f);
            }

            death = Mathf.MoveTowards(death, dead ? 1f : 0f, dt * (dead ? 1.6f : 4f));
            Quaternion fall = Quaternion.Euler(-82f * Mathf.SmoothStep(0f, 1f, death), 0f, 0f);
            Vector3 fallDrop = Vector3.down * 0.7f * death;

            body.localRotation = fall * flinch * roll * lean;
            body.localPosition = bodyBase + Vector3.up * bob + rollDrop + fallDrop;

            if (cape != null)
                cape.localRotation = capeBase * Quaternion.Euler(amt * 28f + Mathf.Sin(phase * 2f + 1f) * 4f * amt + Mathf.Sin(Time.time * 1.7f) * 2f, 0f, 0f);
        }
    }
}
