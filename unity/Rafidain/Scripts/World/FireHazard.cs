using UnityEngine;

namespace Rafidain
{
    /// <summary>Kirkuk's eternal fire: burning ground that hurts whoever stands in it.</summary>
    public class FireHazard : MonoBehaviour
    {
        public float radius = 2.2f;
        public float damagePerTick = 7f;
        public float tick = 0.5f;

        PlayerStats player;
        float next;
        Light glow;
        float baseIntensity;

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            glow = GetComponentInChildren<Light>();
            if (glow != null) baseIntensity = glow.intensity;
        }

        void Update()
        {
            if (glow != null) glow.intensity = baseIntensity * (0.75f + Mathf.PerlinNoise(Time.time * 5f, transform.position.x) * 0.5f);
            if (player == null || player.IsDead || Time.time < next) return;
            Vector3 d = player.transform.position - transform.position; d.y = 0f;
            if (d.magnitude > radius) return;
            next = Time.time + tick;
            player.TakeDamage(damagePerTick, player.transform.position - player.transform.forward);
        }
    }
}
