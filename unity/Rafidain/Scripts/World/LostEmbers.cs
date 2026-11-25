using UnityEngine;

namespace Rafidain
{
    /// <summary>Embers dropped where Furat died. Walk into them to recover. Die again first and they are gone.</summary>
    public class LostEmbers : MonoBehaviour
    {
        public int amount;
        public float pickupRange = 1.6f;

        PlayerStats player;
        Vector3 basePos;

        public static LostEmbers Spawn(Vector3 position, int amount)
        {
            var go = new GameObject("LostEmbers");
            go.transform.position = position + Vector3.up * 0.6f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Glow";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.3f;
            var r = visual.GetComponent<Renderer>();
            r.material.color = new Color(1f, 0.45f, 0.1f);
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 4f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.15f);
            light.range = 5f;
            light.intensity = 3f;

            var l = go.AddComponent<LostEmbers>();
            l.amount = amount;
            return l;
        }

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            basePos = transform.position;
        }

        void Update()
        {
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.15f;
            if (player == null || player.IsDead) return;

            if (Vector3.Distance(player.transform.position + Vector3.up * 0.6f, basePos) <= pickupRange)
            {
                player.AddEmbers(amount);
                if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 0.8f);
                if (HUD.Instance != null) HUD.Instance.ShowMessage("+" + amount + "  " + ArabicText.Fix("استعدت جمرك"), 2f);
                if (GameManager.Instance != null) GameManager.Instance.OnLostEmbersRecovered(this);
                Destroy(gameObject);
            }
        }
    }
}
