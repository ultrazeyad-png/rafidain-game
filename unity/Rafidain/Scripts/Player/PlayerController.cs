using UnityEngine;

namespace Rafidain
{
    /// <summary>Movement, sprint, dodge roll with invincibility frames, healing and hit stagger.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 4.8f;
        public float runSpeed = 7.8f;
        public float rotationSpeed = 16f;
        public float acceleration = 40f;
        public float gravity = -22f;
        public float sprintStaminaPerSecond = 12f;

        [Header("Dodge roll")]
        public float dodgeDistance = 5.2f;
        public float dodgeDuration = 0.5f;
        public float dodgeIFrameStart = 0.04f;
        public float dodgeIFrameEnd = 0.36f;
        public float dodgeStamina = 18f;

        [Header("Healing")]
        public float healDuration = 1.0f;
        public float healMoveMultiplier = 0.35f;

        public bool IsDodging { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsHealing { get; private set; }
        public bool IsBlocking { get; private set; }
        public float blockMoveMultiplier = 0.5f;
        public bool IsStaggered => staggerTimer > 0f;
        public float CurrentSpeed { get; private set; }

        public event System.Action OnDodge;
        public event System.Action OnHealStart;
        public event System.Action OnStagger;
        public bool IsBusy => IsDodging || IsHealing || IsStaggered || (combat != null && combat.IsAttacking);

        CharacterController cc;
        PlayerStats stats;
        PlayerCombat combat;
        ThirdPersonCamera camRig;
        Transform cam;

        float verticalVelocity;
        Vector3 dodgeDir;
        float dodgeTimer;
        float healTimer;
        bool healApplied;
        float staggerTimer;
        Vector3 staggerPush;
        Vector3 planarVelocity;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            stats = GetComponent<PlayerStats>();
            combat = GetComponent<PlayerCombat>();
        }

        void Start()
        {
            if (Camera.main != null)
            {
                cam = Camera.main.transform;
                camRig = Camera.main.GetComponent<ThirdPersonCamera>();
            }
        }

        void Update()
        {
            var input = PlayerInputReader.Instance;
            if (input == null || (stats != null && stats.IsDead))
            {
                ApplyGravityOnly();
                return;
            }

            float dt = Time.deltaTime;
            Vector3 moveDir = CameraRelative(input.Move);

            // --- Stagger (after being hit) ---
            if (staggerTimer > 0f)
            {
                staggerTimer -= dt;
                cc.Move((staggerPush * Mathf.Clamp01(staggerTimer / 0.35f) + Vector3.up * verticalVelocity) * dt);
                ApplyGravity(dt);
                return;
            }

            // --- Dodge roll ---
            if (IsDodging)
            {
                dodgeTimer += dt;
                IsInvulnerable = dodgeTimer >= dodgeIFrameStart && dodgeTimer <= dodgeIFrameEnd;
                float speed = dodgeDistance / dodgeDuration;
                cc.Move((dodgeDir * speed + Vector3.up * verticalVelocity) * dt);
                ApplyGravity(dt);
                if (dodgeTimer >= dodgeDuration)
                {
                    IsDodging = false;
                    IsInvulnerable = false;
                }
                return;
            }

            bool canAct = !IsBusy;

            if (canAct && input.Consume(GameAction.Dodge) && stats.TryUseStamina(dodgeStamina))
            {
                IsDodging = true;
                IsBlocking = false;
                dodgeTimer = 0f;
                dodgeDir = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : -transform.forward; // backstep
                if (moveDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dodgeDir);
                planarVelocity = Vector3.zero;
                CurrentSpeed = 0f;
                OnDodge?.Invoke();
                if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.roll, transform.position, 0.8f);
                return;
            }

            if (canAct && input.Consume(GameAction.Heal) && stats.UseFlask())
            {
                IsHealing = true;
                healTimer = 0f;
                healApplied = false;
                OnHealStart?.Invoke();
            }

            if (IsHealing)
            {
                healTimer += dt;
                if (!healApplied && healTimer >= healDuration * 0.5f)
                {
                    stats.HealPercent(stats.flaskHealPercent);
                    healApplied = true;
                }
                if (healTimer >= healDuration) IsHealing = false;
            }

            // --- Normal movement ---
            bool attacking = combat != null && combat.IsAttacking;
            IsBlocking = input.BlockHeld && !attacking && !IsHealing && stats.Stamina > 0.5f;
            IsSprinting = input.SprintHeld && !IsBlocking && moveDir.sqrMagnitude > 0.01f && !attacking && !IsHealing && stats.Stamina > 0.5f;
            if (IsSprinting) stats.DrainStamina(sprintStaminaPerSecond * dt);

            float speedNow = IsSprinting ? runSpeed : walkSpeed;
            if (IsHealing) speedNow *= healMoveMultiplier;
            if (IsBlocking) speedNow *= blockMoveMultiplier;
            if (attacking) speedNow = 0f;

            planarVelocity = Vector3.MoveTowards(planarVelocity, moveDir * speedNow, acceleration * dt);
            CurrentSpeed = planarVelocity.magnitude;
            cc.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            ApplyGravity(dt);

            // --- Facing ---
            Transform lockTarget = camRig != null ? camRig.LockTarget : null;
            Vector3 faceDir = Vector3.zero;
            if (lockTarget != null && !IsSprinting)
            {
                faceDir = lockTarget.position - transform.position;
                faceDir.y = 0f;
            }
            else if (moveDir.sqrMagnitude > 0.01f && !attacking)
            {
                faceDir = moveDir;
            }

            if (faceDir.sqrMagnitude > 0.001f)
            {
                float rotSpeed = attacking ? rotationSpeed * 0.25f : rotationSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceDir), 1f - Mathf.Exp(-rotSpeed * dt));
            }
        }

        public void ApplyHitStagger(Vector3 sourcePosition) => ApplyHitStagger(sourcePosition, 0.35f, 3f);

        public void ApplyHitStagger(Vector3 sourcePosition, float duration, float push)
        {
            IsBlocking = false;
            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;
            staggerPush = away.sqrMagnitude > 0.001f ? away.normalized * push : -transform.forward * push;
            staggerTimer = duration;
            IsHealing = false;
            planarVelocity = Vector3.zero;
            CurrentSpeed = 0f;
            OnStagger?.Invoke();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            verticalVelocity = 0f;
            IsDodging = IsInvulnerable = IsHealing = false;
            staggerTimer = 0f;
        }

        Vector3 CameraRelative(Vector2 input)
        {
            if (cam == null) return new Vector3(input.x, 0f, input.y);
            Vector3 f = cam.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.right; r.y = 0f; r.Normalize();
            return f * input.y + r * input.x;
        }

        void ApplyGravity(float dt)
        {
            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * dt;
        }

        void ApplyGravityOnly()
        {
            if (!cc.enabled) return;
            ApplyGravity(Time.deltaTime);
            cc.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }
    }
}
