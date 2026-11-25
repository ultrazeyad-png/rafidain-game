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

        [Header("Boss moves")]
        [Tooltip("Leaps through the air and slams down on the player from mid range.")]
        public bool canLeap;
        public float leapCooldown = 7f;
        [Range(0f, 1f)] public float sweepChance = 0.35f;

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
        float nextLeap;

        public bool IsDead => state == State.Dead;
        public float CurrentSpeed { get; private set; }

        public event System.Action OnAttackStart;
        public event System.Action OnHitTaken;
        public event System.Action OnDied;
        public event System.Action OnReset;
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
            CurrentSpeed = 0f;
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
                    if (canLeap && dist > 6f && dist < 15f && Time.time > nextLeap)
                    {
                        nextLeap = Time.time + leapCooldown / speedMul;
                        StartCoroutine(LeapRoutine());
                        break;
                    }
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

            // Bosses sometimes do a wide, slow sweep instead of a straight cut
            bool sweep = isBoss && Random.value < sweepChance;
            float reachMul = sweep ? 1.35f : 1f;
            float angle = sweep ? 170f : attackAngle;
            float windMul = sweep ? 1.25f : 1f;
            if (sweep) hits = 1;

            for (int h = 0; h < hits; h++)
            {
                // Windup with red telegraph glow
                OnAttackStart?.Invoke();
                float w = windup * windMul / speedMul * (h > 0 ? 0.6f : 1f);
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

                if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.swing, transform.position + Vector3.up, 0.8f, 0.15f);
                // Active frames with lunge
                bool landed = false;
                t = 0f;
                while (t < activeTime)
                {
                    t += Time.deltaTime;
                    Move(transform.forward * lungeSpeed);
                    CurrentSpeed = 0f;
                    if (!landed && player != null && InReach(reachMul, angle))
                        landed = player.TakeDamage(damage * (phase2 ? 1.2f : 1f), transform.position);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(recovery / speedMul);
            if (state == State.Attack) state = State.Chase;
        }

        bool InReach(float reachMul = 1f, float angle = -1f)
        {
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            float scaledReach = attackReach * reachMul * transform.lossyScale.x;
            if (angle < 0f) angle = attackAngle;
            return to.magnitude <= scaledReach && Vector3.Angle(transform.forward, to) <= angle;
        }

        IEnumerator LeapRoutine()
        {
            state = State.Attack;
            OnAttackStart?.Invoke();

            // crouch: long orange telegraph
            float t = 0f;
            float wind = 0.85f;
            while (t < wind)
            {
                t += Time.deltaTime;
                if (player != null)
                {
                    Vector3 to = player.transform.position - transform.position; to.y = 0f;
                    Face(to, 1.5f);
                }
                SetTint(t / wind, new Color(1f, 0.55f, 0.1f));
                yield return null;
            }
            SetTint(0f, Color.red);

            // jump arc to where the player stands now
            Vector3 start = transform.position;
            Vector3 land = start;
            if (player != null)
            {
                Vector3 p = player.transform.position;
                Vector3 dir = p - start; dir.y = 0f;
                float halfH = 1f * transform.lossyScale.y;
                land = p - dir.normalized * 1.2f;
                land.y = p.y - 1f + halfH + 0.05f;
            }
            if (cc != null) cc.enabled = false;
            float dur = 0.6f;
            t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                transform.position = Vector3.Lerp(start, land, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 5f;
                yield return null;
            }
            transform.position = land;
            if (cc != null) cc.enabled = true;
            verticalVelocity = 0f;

            // slam
            if (player != null && Vector3.Distance(player.transform.position, transform.position) < 3.8f * transform.lossyScale.x)
                player.TakeDamage(damage * 1.4f, transform.position);
            if (HUD.Instance != null) HUD.Instance.Shake(0.35f);
            HitFX.Dust(transform.position + Vector3.down * 0.9f * transform.lossyScale.y, 40);

            yield return new WaitForSeconds(recovery * 1.3f);
            if (state == State.Attack) state = State.Chase;
        }

        public bool TakeDamage(float amount, Vector3 sourcePosition)
        {
            if (state == State.Dead) return false;
            health -= amount;
            StartCoroutine(HitFlash());
            if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.hit, transform.position + Vector3.up, 0.9f);
            Vector3 impact = transform.position + Vector3.up * 0.4f * transform.lossyScale.y + (sourcePosition - transform.position).normalized * 0.4f;
            HitFX.Hit(impact, amount >= 40f, new Color(1f, 0.75f, 0.4f));
            OnHitTaken?.Invoke();

            if (state == State.Idle || state == State.Return) Engage();

            if (isBoss && !phase2 && HealthRatio <= phase2Threshold)
            {
                phase2 = true;
                if (HUD.Instance != null) HUD.Instance.ShowMessage(ArabicText.Fix(displayName + " يستشيط غضبا"), 2f);
            }

            if (health <= 0f) Die();
            return true;
        }

        void Die()
        {
            StopAllCoroutines();
            state = State.Dead;
            health = 0f;
            CurrentSpeed = 0f;
            OnDied?.Invoke();
            if (player != null) player.AddEmbers(emberReward);

            if (isBoss)
            {
                if (!string.IsNullOrEmpty(bossId) && !SaveSystem.Data.defeatedBosses.Contains(bossId))
                    SaveSystem.Data.defeatedBosses.Add(bossId);
                // every guardian carries one of the five keys of the gate under Baghdad
                string key = bossId + "_key";
                if (!string.IsNullOrEmpty(bossId) && !SaveSystem.Data.keys.Contains(key))
                {
                    SaveSystem.Data.keys.Add(key);
                    if (HUD.Instance != null)
                        HUD.Instance.ShowDialog(ArabicText.Fix("مفتاح الحارس"),
                            ArabicText.Fix("سقط من يد الحارس مفتاح من برونز قديم") + "\n" +
                            SaveSystem.Data.keys.Count + " / 5  " + ArabicText.Fix("مفاتيح باب العالم السفلي"), 6f);
                }
                if (HUD.Instance != null)
                {
                    HUD.Instance.HideBoss();
                    HUD.Instance.ShowMessage(ArabicText.Fix("سقط عدو عظيم"), 4f);
                }
                if (GameManager.Instance != null) GameManager.Instance.SaveNow();
            }

            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            if (cc != null) cc.enabled = false;
            if (GetComponent<EnemyAnimator>() != null) yield return new WaitForSeconds(2.2f); // let the death animation play
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
            OnReset?.Invoke();
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
            CurrentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
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
