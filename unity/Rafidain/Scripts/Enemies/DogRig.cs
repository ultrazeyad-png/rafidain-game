using UnityEngine;

namespace Rafidain
{
    /// <summary>Procedural gallop for the feral dogs: legs swing in pairs, body bobs, head dips before the bite.</summary>
    public class DogRig : MonoBehaviour
    {
        public Transform body, head, tail;
        public Transform legFL, legFR, legBL, legBR;
        public float strideLength = 1.1f;

        EnemyAI ai;
        float phase;
        float deathT;
        Vector3 bodyBase, headBase;
        float lastAttack = -10f;

        void Awake()
        {
            ai = GetComponent<EnemyAI>();
            if (body != null) bodyBase = body.localPosition;
            if (head != null) headBase = head.localPosition;
            if (ai != null) ai.OnAttackStart += () => lastAttack = Time.time;
            if (ai != null) ai.OnReset += () => deathT = 0f;
        }

        void Update()
        {
            if (ai == null || body == null) return;
            float dt = Time.deltaTime;

            if (ai.IsDead)
            {
                // roll onto the side
                deathT = Mathf.Min(1f, deathT + dt * 2.5f);
                body.localRotation = Quaternion.Euler(0f, 0f, 85f * deathT);
                body.localPosition = bodyBase + Vector3.down * 0.25f * deathT;
                return;
            }
            body.localRotation = Quaternion.identity;

            float speed = ai.CurrentSpeed;
            phase += speed / Mathf.Max(0.1f, strideLength) * Mathf.PI * 2f * dt;
            float amt = Mathf.Clamp01(speed / 3f);
            float swing = Mathf.Sin(phase) * 38f * amt;
            if (legFL != null) legFL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legBR != null) legBR.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legFR != null) legFR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (legBL != null) legBL.localRotation = Quaternion.Euler(-swing, 0f, 0f);

            body.localPosition = bodyBase + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.06f * amt;

            if (head != null)
            {
                float sinceAttack = Time.time - lastAttack;
                float dip = sinceAttack < 0.5f ? Mathf.Sin(sinceAttack / 0.5f * Mathf.PI) : 0f;   // crouch, then snap
                head.localPosition = headBase + new Vector3(0f, -0.12f * dip, 0.1f * dip);
            }
            if (tail != null) tail.localRotation = Quaternion.Euler(-30f, Mathf.Sin(Time.time * (speed > 0.5f ? 14f : 4f)) * 25f, 0f);
        }
    }
}
