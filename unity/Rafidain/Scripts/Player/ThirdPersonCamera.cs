using UnityEngine;

namespace Rafidain
{
    /// <summary>Orbit camera with wall collision and souls-style lock-on.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 4.2f;
        public float pivotHeight = 1.5f;
        public float sensitivity = 1.3f;
        public bool invertY;
        public float minPitch = -35f;
        public float maxPitch = 70f;
        public float collisionRadius = 0.25f;
        public float followSmooth = 20f;
        public float lockOnRange = 18f;
        [Tooltip("The lock breaks when the target gets this far away.")]
        public float lockBreakRange = 25f;
        [Tooltip("How hard you flick the stick / mouse sideways to jump to the next enemy.")]
        public float switchFlick = 6f;

        public Transform LockTarget { get; private set; }
        public static ThirdPersonCamera Instance { get; private set; }

        float shake;

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void Awake() => Instance = this;

        EnemyAI lockEnemy;
        Transform focusTarget;
        float focusUntil;

        /// <summary>Turn the camera toward a target for a few seconds (boss intro). Any skip input ends it early.</summary>
        public void Focus(Transform t, float seconds)
        {
            focusTarget = t;
            focusUntil = Time.time + seconds;
        }
        float switchAccum;
        float switchCooldown;
        float blockedTime;
        float yaw;
        float pitch = 12f;
        float currentDistance;
        Vector3 pivot;
        readonly RaycastHit[] hits = new RaycastHit[12];

        void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                pivot = target.position + Vector3.up * pivotHeight;
            }
            currentDistance = distance;
            if (!Application.isMobilePlatform) SetCursorLocked(true);
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            var input = PlayerInputReader.Instance;

            if (input != null)
            {
                if (input.Consume(GameAction.LockOn)) ToggleLock();
            }

            if (lockEnemy != null)
            {
                float d = Vector3.Distance(lockEnemy.transform.position, target.position);
                if (lockEnemy.IsDead || !lockEnemy.gameObject.activeInHierarchy || d > lockBreakRange) ClearLock();
                else
                {
                    // lose the lock if a wall / hill hides the target for a while
                    Vector3 eye = target.position + Vector3.up * pivotHeight;
                    Vector3 aim = lockEnemy.transform.position + Vector3.up * 1f;
                    bool hidden = Physics.Linecast(eye, aim, out var hit, ~0, QueryTriggerInteraction.Ignore)
                                  && !hit.transform.IsChildOf(lockEnemy.transform) && !hit.transform.IsChildOf(target);
                    blockedTime = hidden ? blockedTime + dt : 0f;
                    if (blockedTime > 1.5f) ClearLock();
                }
            }

            // flick sideways while locked to jump to the next enemy
            if (lockEnemy != null && input != null)
            {
                switchCooldown -= dt;
                switchAccum = Mathf.MoveTowards(switchAccum, 0f, dt * 20f) + input.Look.x;
                if (switchCooldown <= 0f && Mathf.Abs(switchAccum) > switchFlick)
                {
                    SwitchTarget(switchAccum > 0f ? 1 : -1);
                    switchAccum = 0f;
                    switchCooldown = 0.35f;
                }
            }

            pivot = Vector3.Lerp(pivot, target.position + Vector3.up * pivotHeight, 1f - Mathf.Exp(-followSmooth * dt));

            if (focusTarget != null && Time.time < focusUntil)
            {
                Vector3 to = focusTarget.position + Vector3.up * 1.6f * Mathf.Max(1f, focusTarget.lossyScale.y) - pivot;
                Quaternion look = Quaternion.LookRotation(to);
                float targetPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, look.eulerAngles.x) - 4f, -20f, 30f);
                yaw = Mathf.LerpAngle(yaw, look.eulerAngles.y, 1f - Mathf.Exp(-3.5f * dt));
                pitch = Mathf.Lerp(pitch, targetPitch, 1f - Mathf.Exp(-3.5f * dt));
            }
            else if (LockTarget != null)
            {
                Vector3 to = LockTarget.position + Vector3.up * 1.0f - pivot;
                Quaternion look = Quaternion.LookRotation(to);
                float targetPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, look.eulerAngles.x) + 10f, -10f, 40f);
                yaw = Mathf.LerpAngle(yaw, look.eulerAngles.y, 1f - Mathf.Exp(-10f * dt));
                pitch = Mathf.Lerp(pitch, targetPitch, 1f - Mathf.Exp(-10f * dt));
            }
            else if (input != null)
            {
                yaw += input.Look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - input.Look.y * sensitivity * (invertY ? -1f : 1f), minPitch, maxPitch);
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;

            float wanted = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, dir, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (c.transform.root == target.root) continue;
                if (c.GetComponentInParent<EnemyAI>() != null) continue;
                if (hits[i].distance > 0f) wanted = Mathf.Min(wanted, hits[i].distance);
            }

            currentDistance = wanted < currentDistance ? wanted : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-5f * dt));
            Vector3 shakeOffset = Vector3.zero;
            if (shake > 0f)
            {
                shakeOffset = Random.insideUnitSphere * shake * 0.4f;
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
            }
            transform.SetPositionAndRotation(pivot + dir * currentDistance + shakeOffset, rot);
        }

        void ToggleLock()
        {
            if (LockTarget != null) { ClearLock(); return; }

            EnemyAI best = null;
            float bestScore = float.MaxValue;
            foreach (var e in FindObjectsByType<EnemyAI>())
            {
                if (e.IsDead) continue;
                Vector3 to = e.transform.position - target.position;
                float dist = to.magnitude;
                if (dist > lockOnRange) continue;
                Vector3 flat = to; flat.y = 0f;
                Vector3 camF = transform.forward; camF.y = 0f;
                float angle = Vector3.Angle(camF, flat);
                if (angle > 70f) continue;
                float score = dist + angle * 0.15f;
                if (score < bestScore) { bestScore = score; best = e; }
            }

            if (best != null)
            {
                lockEnemy = best;
                LockTarget = best.transform;
            }
            else
            {
                // No target: re-centre camera behind Furat, like souls games
                yaw = target.eulerAngles.y;
            }
        }

        /// <summary>Jump to the nearest living enemy on the given side (1 = right, -1 = left) of the current target.</summary>
        void SwitchTarget(int side)
        {
            if (lockEnemy == null) return;
            Vector3 from = lockEnemy.transform.position - target.position; from.y = 0f;
            Vector3 right = transform.right; right.y = 0f; right.Normalize();
            EnemyAI best = null;
            float bestAngle = float.MaxValue;
            foreach (var e in EnemyAI.All)
            {
                if (e == null || e == lockEnemy || e.IsDead) continue;
                Vector3 to = e.transform.position - target.position; to.y = 0f;
                if (to.magnitude > lockOnRange) continue;
                float signed = Vector3.SignedAngle(from, to, Vector3.up);   // + is clockwise = to the right
                if (Mathf.Sign(signed) != side || Mathf.Abs(signed) < 2f) continue;
                if (Mathf.Abs(signed) < bestAngle) { bestAngle = Mathf.Abs(signed); best = e; }
            }
            if (best == null) return;
            lockEnemy = best;
            LockTarget = best.transform;
            blockedTime = 0f;
        }

        void ClearLock()
        {
            blockedTime = 0f;
            switchAccum = 0f;
            lockEnemy = null;
            LockTarget = null;
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
