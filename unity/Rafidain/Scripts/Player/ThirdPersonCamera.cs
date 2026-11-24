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
        public float minPitch = -35f;
        public float maxPitch = 70f;
        public float collisionRadius = 0.25f;
        public float followSmooth = 20f;
        public float lockOnRange = 18f;

        public Transform LockTarget { get; private set; }
        public static ThirdPersonCamera Instance { get; private set; }

        float shake;

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void Awake() => Instance = this;

        EnemyAI lockEnemy;
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
                if (input.PausePressed && !Application.isMobilePlatform)
                    SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
                if (input.Consume(GameAction.LockOn)) ToggleLock();
            }

            if (lockEnemy != null)
            {
                float d = Vector3.Distance(lockEnemy.transform.position, target.position);
                if (lockEnemy.IsDead || !lockEnemy.gameObject.activeInHierarchy || d > lockOnRange * 1.3f) ClearLock();
            }

            pivot = Vector3.Lerp(pivot, target.position + Vector3.up * pivotHeight, 1f - Mathf.Exp(-followSmooth * dt));

            if (LockTarget != null)
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
                pitch = Mathf.Clamp(pitch - input.Look.y * sensitivity, minPitch, maxPitch);
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

        void ClearLock()
        {
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
