using UnityEngine;

namespace Rafidain
{
    /// <summary>An arrow: flies with a slight drop, hurts Furat (blockable from the front, dodgeable), sticks in the ground.</summary>
    public class Projectile : MonoBehaviour
    {
        public float damage = 16f;
        public Vector3 velocity;
        public float gravity = 4f;
        public float life = 4f;
        public float hitRadius = 0.6f;

        PlayerStats player;
        bool stuck;

        public static void Fire(Vector3 from, Vector3 velocity, float damage, Material mat)
        {
            var go = new GameObject("Arrow");
            go.transform.position = from;
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(go.transform, false);
            shaft.transform.localScale = new Vector3(0.035f, 0.035f, 0.85f);
            if (mat != null) shaft.GetComponent<Renderer>().sharedMaterial = mat;
            var p = go.AddComponent<Projectile>();
            p.velocity = velocity;
            p.damage = damage;
            go.transform.rotation = Quaternion.LookRotation(velocity);
        }

        void Start() => player = FindAnyObjectByType<PlayerStats>();

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (stuck) return;

            velocity += Vector3.down * gravity * Time.deltaTime;
            Vector3 step = velocity * Time.deltaTime;

            // hit the ground / walls
            if (Physics.Raycast(transform.position, step.normalized, out var hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerStats>() == null && hit.collider.GetComponentInParent<EnemyAI>() == null)
            {
                transform.position = hit.point;
                stuck = true;
                life = 6f;
                return;
            }

            transform.position += step;
            transform.rotation = Quaternion.LookRotation(velocity);

            if (player != null && !player.IsDead)
            {
                Vector3 chest = player.transform.position + Vector3.up * 0.3f;
                if (Vector3.Distance(transform.position, chest) < hitRadius)
                {
                    // source is where the arrow came from, so a raised shield facing the archer blocks it
                    player.TakeDamage(damage, transform.position - velocity.normalized * 3f);
                    Destroy(gameObject);
                }
            }
        }
    }
}
