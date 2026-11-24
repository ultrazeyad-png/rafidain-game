using UnityEngine;

namespace Rafidain
{
    /// <summary>The flame (bonfire): rest to refill health and flasks, set respawn, revive enemies.</summary>
    public class Bonfire : MonoBehaviour
    {
        public string bonfireId = "zakho_gate";
        public string displayName = "Zakho - Old Gate";
        public float interactRange = 2.5f;
        public Light fireLight;
        public Transform spawnPoint;

        PlayerStats player;
        float baseIntensity = 1f;
        float noiseSeed;

        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * 1.5f + Vector3.up * 1f;
        public Quaternion SpawnRotation => Quaternion.LookRotation(-transform.forward);
        public bool IsDiscovered => SaveSystem.Data.discoveredBonfires.Contains(bonfireId);

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            if (fireLight != null) baseIntensity = fireLight.intensity;
            noiseSeed = Random.value * 100f;
        }

        void Update()
        {
            if (fireLight != null)
                fireLight.intensity = baseIntensity * (0.8f + Mathf.PerlinNoise(Time.time * 4f, noiseSeed) * 0.4f);

            if (player == null || player.IsDead) return;
            if (Vector3.Distance(player.transform.position, transform.position) > interactRange) return;

            if (HUD.Instance != null) HUD.Instance.SetPrompt(IsDiscovered ? "[E] Rest at the flame" : "[E] Kindle the flame");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact)) Rest();
        }

        public void Rest()
        {
            bool firstTime = !IsDiscovered;
            player.RestoreAll();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetRespawn(this);
                GameManager.Instance.ResetAllEnemies();
            }
            if (HUD.Instance != null)
                HUD.Instance.ShowMessage(firstTime ? "FLAME KINDLED\n" + displayName : "Rested: " + displayName, 2.5f);
        }
    }
}
