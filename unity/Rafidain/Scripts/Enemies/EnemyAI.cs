using System.Collections;
using System.Collections.Generic;
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

        public enum BossMove { None, Wave, WingGust, ShieldCharge, FireTrail, Spin, ShadowStep }

        [Header("Signature boss move")]
        public BossMove special = BossMove.None;
        public float specialCooldown = 9f;
        float nextSpecial = 5f;
        float fireTrailUntil;
        float nextFirePatch;

        [Header("Ranged (archers)")]
        public bool ranged;
        public float preferredRange = 13f;
        public float drawTime = 1.1f;
        public float arrowSpeed = 26f;
        float nextShot;

        [Header("Poise (stagger)")]
        [Tooltip("Damage soaked before the enemy staggers and can be executed. 0 = auto (bosses get more).")]
        public float poise = 0f;
        public float staggerTime = 1.6f;

        enum State { Idle, Chase, Attack, Return, Dead, Staggered }

        public static readonly List<EnemyAI> All = new List<EnemyAI>();
        float poiseLeft;
        float lastPoiseHit;
        float staggerLeft;
        public bool IsStaggered => state == State.Staggered;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        /// <summary>Nearest staggered enemy Furat can execute (front or back), or null.</summary>
        public static EnemyAI RiposteTarget(Vector3 from, float range)
        {
            EnemyAI best = null;
            float bestD = range;
            foreach (var e in All)
            {
                if (e == null || !e.IsStaggered) continue;
                float d = Vector3.Distance(from, e.transform.position) - 0.5f * e.transform.lossyScale.x;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        float MaxPoise => poise > 0f ? poise : (isBoss ? maxHealth * 0.22f : maxHealth * 0.55f);

        void EnterStagger()
        {
            StopAllCoroutines();
            SetTint(0f, Color.red);
            state = State.Staggered;
            staggerLeft = isBoss ? staggerTime * 1.4f : staggerTime;
            poiseLeft = MaxPoise;
            if (HUD.Instance != null && isBoss) HUD.Instance.ShowMessage(ArabicText.Fix("ترنح الحارس... اضرب الان"), 1.5f);
        }

        string BossEpithet()
        {
            switch (special)
            {
                case BossMove.Wave: return "سيد السد الغارق";
                case BossMove.WingGust: return "الثور المجنح لنينوى";
                case BossMove.ShieldCharge: return "حامي القلعة الذي لا يسقط";
                case BossMove.FireTrail: return "ابن النار الازلية";
                case BossMove.Spin: return "دوامة الملوية";
                case BossMove.ShadowStep: return "ادابا... حارس باب ارشكيكال";
                default: return "حارس الطريق الاول";
            }
        }

        /// <summary>The player's shield caught this attack at the last moment (parry).</summary>
        public void Parried()
        {
            if (state == State.Dead || state == State.Staggered) return;
            lastPoiseHit = Time.time;
            if (isBoss)
            {
                poiseLeft -= MaxPoise * 0.5f;     // two parries break a guardian
                if (poiseLeft > 0f) { StopAllCoroutines(); SetTint(0f, Color.red); state = State.Chase; return; }
            }
            EnterStagger();
        }

        /// <summary>Finds the enemy whose attack came from exactly this point (melee attacks pass their own position).</summary>
        public static EnemyAI AttackerAt(Vector3 source)
        {
            foreach (var e in All)
                if (e != null && e.state != State.Dead && (e.transform.position - source).sqrMagnitude < 0.36f) return e;
            return null;
        }

        /// <summary>Visceral / riposte damage — ends the stagger.</summary>
        public void TakeRiposte(float amount, Vector3 from)
        {
            if (state != State.Staggered) return;
            state = State.Chase;
            poiseLeft = MaxPoise + amount;   // can't be stun-locked straight back into a stagger
            lastPoiseHit = Time.time;
            TakeDamage(amount, from);
        }

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
        /// <summary>For the floating health bar: when this enemy was last hit, and the damage summed over the current combo.</summary>
        public float LastHitTime { get; private set; } = -10f;
        public float RecentDamage { get; private set; }

        public event System.Action OnAttackStart;
        public event System.Action OnHitTaken;
        public event System.Action OnDied;
        public event System.Action OnEngage;
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
            if (Time.time - lastPoiseHit > 3f) poiseLeft = MaxPoise;
            if (state == State.Staggered)
            {
                staggerLeft -= Time.deltaTime;
                if (staggerLeft <= 0f) state = State.Chase;
                return;
            }

            if (player == null || player.IsDead)
            {
                if (state == State.Chase || state == State.Attack) GoHome();
            }

            Vector3 toPlayer = player != null ? player.transform.position - transform.position : Vector3.zero;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            float speedMul = phase2 ? phase2SpeedMultiplier : 1f;

            // the guardian stands and glares while his name is shown
            if (state == State.Chase && Time.time < introHoldUntil)
            {
                Face(toPlayer, 0.5f);
                CurrentSpeed = 0f;
                return;
            }

            switch (state)
            {
                case State.Idle:
                    if (player != null && !player.IsDead && dist < detectRange) Engage();
                    break;

                case State.Chase:
                    if (Vector3.Distance(transform.position, homePosition) > leashRange) { GoHome(); break; }
                    Face(toPlayer);
                    if (special != BossMove.None && dist < 10f && Time.time > nextSpecial)
                    {
                        nextSpecial = Time.time + specialCooldown / speedMul;
                        StartCoroutine(SpecialRoutine());
                        break;
                    }
                    if (special == BossMove.FireTrail && Time.time < fireTrailUntil && Time.time > nextFirePatch)
                    {
                        nextFirePatch = Time.time + 0.35f;
                        DropFire(transform.position - transform.forward * 1.2f);
                    }
                    if (canLeap && dist > 6f && dist < 15f && Time.time > nextLeap)
                    {
                        nextLeap = Time.time + leapCooldown / speedMul;
                        StartCoroutine(LeapRoutine());
                        break;
                    }
                    if (ranged && dist > attackRange + 0.5f)
                    {
                        // archers keep their distance and loose arrows
                        if (dist < preferredRange - 3f) Move(-transform.forward * moveSpeed * 0.8f);
                        else if (dist > preferredRange + 5f) Move(transform.forward * moveSpeed);
                        if (dist < preferredRange + 8f && Time.time > nextShot) StartCoroutine(ShootRoutine());
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

        float introHoldUntil;

        void Engage()
        {
            state = State.Chase;
            OnEngage?.Invoke();
            if (isBoss && HUD.Instance != null) HUD.Instance.ShowBoss(this);
            // first meeting with a guardian: camera turns to him and his name fills the screen (once per guardian)
            string introKey = string.IsNullOrEmpty(bossId) ? displayName : bossId;
            var seen = SaveSystem.Data.bossIntrosSeen;
            if (isBoss && seen != null && !seen.Contains(introKey))
            {
                seen.Add(introKey);
                introHoldUntil = Time.time + 2.8f;
                if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Focus(transform, 2.6f);
                if (HUD.Instance != null) HUD.Instance.ShowBossTitle(ArabicText.Fix(displayName), ArabicText.Fix(BossEpithet()));
            }
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

        // ---------------------------------------------------------------- signature moves

        IEnumerator SpecialRoutine()
        {
            state = State.Attack;
            OnAttackStart?.Invoke();
            Color tell = special == BossMove.Wave ? new Color(0.3f, 0.6f, 1f)
                       : special == BossMove.FireTrail ? new Color(1f, 0.4f, 0.05f)
                       : special == BossMove.ShadowStep ? new Color(0.2f, 0.1f, 0.35f)
                       : new Color(1f, 0.9f, 0.5f);
            float wind = 1.0f, t = 0f;
            while (t < wind)
            {
                t += Time.deltaTime;
                SetTint(t / wind, tell);
                if (player != null) { Vector3 to = player.transform.position - transform.position; to.y = 0f; Face(to, 1.5f); }
                yield return null;
            }
            SetTint(0f, Color.red);
            float s = transform.lossyScale.x;

            switch (special)
            {
                case BossMove.Wave: // Duhok: a ring of floodwater rolls outward — roll through it
                {
                    bool hitDone = false;
                    for (t = 0f; t < 0.9f; t += Time.deltaTime)
                    {
                        float r = Mathf.Lerp(0.5f, 10f, t / 0.9f);
                        HitFX.Ring(transform.position + Vector3.down * 0.9f * s, r, new Color(0.45f, 0.7f, 1f, 0.8f), 24);
                        if (!hitDone && player != null)
                        {
                            float d = Vector3.Distance(Flat(player.transform.position), Flat(transform.position));
                            if (Mathf.Abs(d - r) < 0.9f) hitDone = player.TakeDamage(damage * 1.1f, transform.position);
                        }
                        yield return null;
                    }
                    break;
                }
                case BossMove.WingGust: // Mosul: the stone wings beat — a cone that hurls Furat back
                {
                    HitFX.Dust(transform.position + transform.forward * 3f, 60);
                    if (player != null)
                    {
                        Vector3 to = player.transform.position - transform.position; to.y = 0f;
                        if (to.magnitude < 9f && Vector3.Angle(transform.forward, to) < 50f && player.TakeDamage(damage * 0.6f, transform.position))
                        {
                            var pc = player.GetComponent<PlayerController>();
                            if (pc != null) pc.ApplyHitStagger(transform.position, 0.7f, 10f);
                        }
                    }
                    if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.4f);
                    break;
                }
                case BossMove.ShieldCharge: // Erbil: the citadel charges like a battering ram
                {
                    bool hitDone = false;
                    for (t = 0f; t < 0.55f; t += Time.deltaTime)
                    {
                        Move(transform.forward * 16f);
                        if (!hitDone && player != null && Vector3.Distance(player.transform.position, transform.position) < 2.2f * s)
                            hitDone = player.TakeDamage(damage * 1.3f, transform.position);
                        yield return null;
                    }
                    HitFX.Dust(transform.position, 30);
                    break;
                }
                case BossMove.FireTrail: // Kirkuk: the ground burns wherever he walks for a while
                    fireTrailUntil = Time.time + 5f;
                    HitFX.Ring(transform.position + Vector3.down * 0.9f * s, 2.5f, new Color(1f, 0.45f, 0.1f), 40);
                    DropFire(transform.position);
                    break;
                case BossMove.Spin: // Samarra: whirls like the spiral minaret — get out of reach
                {
                    float nextTick = 0f;
                    for (t = 0f; t < 1.8f; t += Time.deltaTime)
                    {
                        transform.Rotate(0f, 900f * Time.deltaTime, 0f);
                        if (player != null) Move((player.transform.position - transform.position).normalized * 2.5f);
                        if (t > nextTick && player != null && Vector3.Distance(Flat(player.transform.position), Flat(transform.position)) < 3.6f * s * 0.7f)
                        {
                            nextTick = t + 0.4f;
                            player.TakeDamage(damage * 0.45f, transform.position);
                        }
                        yield return null;
                    }
                    break;
                }
                case BossMove.ShadowStep: // Adapa: dissolves into smoke and strikes from behind
                {
                    if (player == null) break;
                    HitFX.Ring(transform.position, 1.5f, new Color(0.15f, 0.05f, 0.25f), 40);
                    if (cc != null) cc.enabled = false;
                    Vector3 behind = player.transform.position - player.transform.forward * 2.2f;
                    behind.y = transform.position.y + (player.transform.position.y - transform.position.y);
                    transform.position = behind;
                    if (cc != null) cc.enabled = true;
                    Vector3 to2 = player.transform.position - transform.position; to2.y = 0f;
                    if (to2.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to2);
                    HitFX.Ring(transform.position, 1.5f, new Color(0.15f, 0.05f, 0.25f), 40);
                    yield return new WaitForSeconds(0.35f);
                    if (InReach(1.2f, 90f)) player.TakeDamage(damage * 1.2f, transform.position);
                    break;
                }
            }

            yield return new WaitForSeconds(recovery);
            if (state == State.Attack) state = State.Chase;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void DropFire(Vector3 at)
        {
            var go = new GameObject("Burning Ground");
            go.transform.position = at + Vector3.down * 0.9f * transform.lossyScale.y;
            var fh = go.AddComponent<FireHazard>();
            fh.radius = 1.3f;
            fh.damagePerTick = 6f;
            HitFX.Ring(go.transform.position, 0.8f, new Color(1f, 0.45f, 0.1f), 12);
            Destroy(go, 5f);
        }

        IEnumerator ShootRoutine()
        {
            state = State.Attack;
            OnAttackStart?.Invoke();
            float t = 0f;
            while (t < drawTime)
            {
                t += Time.deltaTime;
                if (player != null)
                {
                    Vector3 to = player.transform.position - transform.position; to.y = 0f;
                    Face(to, 2f);
                }
                SetTint(t / drawTime * 0.8f, new Color(1f, 0.85f, 0.3f));
                yield return null;
            }
            SetTint(0f, Color.red);
            if (player != null)
            {
                Vector3 from = transform.position + Vector3.up * 0.55f * transform.lossyScale.y + transform.forward * 0.6f;
                var pc = player.GetComponent<PlayerController>();
                Vector3 target = player.transform.position + Vector3.up * 0.3f;
                if (pc != null) target += player.transform.forward * pc.CurrentSpeed * 0.35f; // lead a moving target a little
                Vector3 dir = (target - from);
                float flight = dir.magnitude / arrowSpeed;
                Vector3 v = dir / flight + Vector3.up * 0.5f * 4f * flight; // compensate the arrow's drop
                Material m = null;
                var blade = transform.Find("WeaponPivot/Blade");
                if (blade == null) foreach (var r in GetComponentsInChildren<Renderer>()) if (r.name == "Blade") { m = r.sharedMaterial; break; }
                if (blade != null) m = blade.GetComponent<Renderer>().sharedMaterial;
                Projectile.Fire(from, v, damage, m);
                if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.swing, from, 0.6f, 0.3f);
            }
            nextShot = Time.time + recovery + 1.2f;
            yield return new WaitForSeconds(0.4f);
            if (state == State.Attack) state = State.Chase;
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
            RecentDamage = Time.time - LastHitTime < 1.6f ? RecentDamage + amount : amount;
            LastHitTime = Time.time;
            StartCoroutine(HitFlash());
            if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.hit, transform.position + Vector3.up, 0.9f);
            Vector3 impact = transform.position + Vector3.up * 0.4f * transform.lossyScale.y + (sourcePosition - transform.position).normalized * 0.4f;
            HitFX.Hit(impact, amount >= 40f, new Color(1f, 0.75f, 0.4f));
            OnHitTaken?.Invoke();

            if (state == State.Idle || state == State.Return) Engage();

            // poise: enough damage in a short time staggers (bosses need much more)
            if (poiseLeft <= 0f) poiseLeft = MaxPoise;
            poiseLeft -= amount;
            lastPoiseHit = Time.time;
            if (poiseLeft <= 0f && state != State.Staggered && health > 0f) EnterStagger();

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
            HitFX.BloodPool(transform.position, Mathf.Clamp(transform.lossyScale.x, 0.6f, 2.5f));
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
                    HUD.Instance.ShowVictory(ArabicText.Fix(displayName + "   ·   " + emberReward + " جمرة"));
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
