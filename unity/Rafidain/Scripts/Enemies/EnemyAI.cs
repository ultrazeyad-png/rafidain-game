using System.Collections;
using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Enemy and boss brain: idle → chase → telegraphed attack → recover.
    /// Bosses get a second phase (faster, two-hit combos) below a health threshold.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyAI : MonoBehaviour, IDamageable
    {
        [Header("Identity")]
        public string displayName = "Hollow Soldier";
        public bool isBoss;
        [Tooltip("Unique id for bosses, used so a defeated boss stays dead.")]
        public string bossId = "";

        [Header("Stats")]
        public float maxHealth = 80f;
        public float moveSpeed = 2.8f;
        public float turnSpeed = 6f;
        public float detectRange = 12f;
        public float leashRange = 30f;
        public int emberReward = 50;

        [Header("Attack")]
        public float attackRange = 2.0f;
        public float attackReach = 2.4f;
        public float attackAngle = 70f;
        public float damage = 20f;
        public float windup = 0.7f;
        public float activeTime = 0.15f;
        public float recovery = 0.9f;
        public float lungeSpeed = 6f;

        [Header("Boss phase 2")]
        [Range(0f, 1f)] public float phase2Threshold = 0.5f;
        public float phase2SpeedMultiplier = 1.35f;

        enum State { Idle, Chase, Attack, Return, Dead }

        State state;
        float health;
        bool phase2;
        float verticalVelocity;
        Vector3 homePosition;
        Quaternion homeRotation;
        CharacterController cc;
        PlayerStats player;
        Renderer[] renderers;
        Color[] baseColors;

        public bool IsDead => state == State.Dead;
        public float HealthRatio => maxHealth > 0 ? health / maxHealth : 0f;
        public bool IsPhase2 => phase2;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].material.HasProperty("_BaseColor") || renderers[i].material.HasProperty("_Color")
                    ? renderers[i].material.color : Color.white;
            health = maxHealth;
        }

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            if (IsBossDefeated()) gameObject.SetActive(false);
        }

        bool IsBossDefeated() => isBoss && !string.IsNullOrEmpty(bossId) && SaveSystem.Data.defeatedBosses.Contains(bossId);

        void Update()
        {
            if (state == State.Dead) return;
            ApplyGravity();

            if (player == null || player.IsDead)
            {
                if (state == State.Chase || state == State.Attack) GoHome();
            }

            Vector3 toPlayer = player != null ? player.transform.position - transform.position : Vector3.zero;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            float speedMul = phase2 ? phase2SpeedMultiplier : 1f;

            switch (state)
            {
                case State.Idle:
                    if (player != null && !player.IsDead && dist < detectRange) Engage();
                    break;

                case State.Chase:
                    if (Vector3.Distance(transform.position, homePosition) > leashRange) { GoHome(); break; }
                    Face(toPlayer);
                    if (dist > attackRange) Move(transform.forward * moveSpeed * speedMul);
                    else StartCoroutine(AttackRoutine());
                    break;

                case State.Return:
                    Vector3 toHome = homePosition - transform.position; toHome.y = 0f;
                    if (toHome.magnitude < 0.5f)
                    {
                        state = State.Idle;
                        health = maxHealth;
                        phase2 = false;
                        transform.rotation = homeRotation;
                    }
                    else
                    {
                        Face(toHome);
                        Move(toHome.normalized * moveSpeed);
                    }
                    break;
            }
        }

        void Engage()
        {
            state = State.Chase;
            if (isBoss && HUD.Instance != null) HUD.Instance.ShowBoss(this);
        }

        void GoHome()
        {
            StopAllCoroutines();
            SetTint(0f, Color.red);
            state = State.Return;
            if (isBoss && HUD.Instance != null) HUD.Instance.HideBoss();
        }

        IEnumerator AttackRoutine()
        {
            state = State.Attack;
            float speedMul = phase2 ? phase2SpeedMultiplier : 1f;
            int hits = (isBoss && phase2) ? 2 : 1;

            for (int h = 0; h < hits; h++)
            {
                // Windup with red telegraph glow
                float w = windup / speedMul * (h > 0 ? 0.6f : 1f);
                float t = 0f;
                while (t < w)
                {
                    t += Time.deltaTime;
                    if (player != null)
                    {
                        Vector3 to = player.transform.position - transform.position; to.y = 0f;
                        Face(to, 0.5f);
                    }
                    SetTint(t / w, Color.red);
                    yield return null;
                }
                SetTint(0f, Color.red);

                // Active frames with lunge
                bool landed = false;
                t = 0f;
                while (t < activeTime)
                {
                    t += Time.deltaTime;
                    Move(transform.forward * lungeSpeed);
                    if (!landed && player != null && InReach())
                        landed = player.TakeDamage(damage * (phase2 ? 1.2f : 1f), transform.position);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(recovery / speedMul);
            if (state == State.Attack) state = State.Chase;
        }

        bool InReach()
        {
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            float scaledReach = attackReach * transform.lossyScale.x;
            return to.magnitude <= scaledReach && Vector3.Angle(transform.forward, to) <= attackAngle;
        }

        public bool TakeDamage(float amount, Vector3 sourcePosition)
        {
            if (state == State.Dead) return false;
            health -= amount;
            StartCoroutine(HitFlash());

            if (state == State.Idle || state == State.Return) Engage();

            if (isBoss && !phase2 && HealthRatio <= phase2Threshold)
            {
                phase2 = true;
                if (HUD.Instance != null) HUD.Instance.ShowMessage(displayName + " grows furious!", 2f);
            }

            if (health <= 0f) Die();
            return true;
        }

        void Die()
        {
            StopAllCoroutines();
            state = State.Dead;
            health = 0f;
            if (player != null) player.AddEmbers(emberReward);

            if (isBoss)
            {
                if (!string.IsNullOrEmpty(bossId) && !SaveSystem.Data.defeatedBosses.Contains(bossId))
                    SaveSystem.Data.defeatedBosses.Add(bossId);
                if (HUD.Instance != null)
                {
                    HUD.Instance.HideBoss();
                    HUD.Instance.ShowMessage("GREAT ENEMY FELLED", 4f);
                }
                if (GameManager.Instance != null) GameManager.Instance.SaveNow();
            }

            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            if (cc != null) cc.enabled = false;
            float t = 0f;
            Vector3 start = transform.position;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                transform.position = start + Vector3.down * (t * 1.2f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        /// <summary>Called when the player rests at a bonfire or dies: everything comes back, like souls games.</summary>
        public void ResetEnemy()
        {
            if (IsBossDefeated()) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            StopAllCoroutines();
            if (cc != null) cc.enabled = false;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            if (cc != null) cc.enabled = true;
            health = maxHealth;
            phase2 = false;
            verticalVelocity = 0f;
            state = State.Idle;
            SetTint(0f, Color.red);
        }

        IEnumerator HitFlash()
        {
            SetTint(0.8f, Color.white);
            yield return new WaitForSeconds(0.08f);
            if (state != State.Dead) SetTint(0f, Color.white);
        }

        void SetTint(float amount, Color tint)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].material.color = Color.Lerp(baseColors[i], tint, amount);
        }

        void Face(Vector3 dir, float speedFactor = 1f)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-turnSpeed * speedFactor * Time.deltaTime));
        }

        void Move(Vector3 velocity)
        {
            if (cc != null && cc.enabled) cc.Move(velocity * Time.deltaTime);
        }

        void ApplyGravity()
        {
            if (cc == null || !cc.enabled) return;
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += -22f * Time.deltaTime;
            cc.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
