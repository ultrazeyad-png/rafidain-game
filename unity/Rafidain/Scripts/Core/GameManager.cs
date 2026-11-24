using System.Collections;
using UnityEngine;

namespace Rafidain
{
    /// <summary>Death and respawn loop, lost embers, saving and play time.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerStats player;
        public Bonfire defaultBonfire;
        public float respawnDelay = 3.5f;
        public float autosaveInterval = 60f;

        public Bonfire CurrentBonfire { get; private set; }
        public LostEmbers CurrentLostEmbers { get; private set; }

        float autosaveTimer;
        bool respawning;

        void Awake()
        {
            Instance = this;
            SaveSystem.Load();
        }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerStats>();
            var data = SaveSystem.Data;

            CurrentBonfire = defaultBonfire;
            if (!string.IsNullOrEmpty(data.lastBonfireId))
            {
                foreach (var b in FindObjectsByType<Bonfire>())
                    if (b.bonfireId == data.lastBonfireId) { CurrentBonfire = b; break; }
                if (CurrentBonfire != null) TeleportToBonfire(CurrentBonfire);
            }

            if (player != null) player.Embers = data.embers;
            if (data.hasLostEmbers && data.lostEmbersAmount > 0)
                CurrentLostEmbers = LostEmbers.Spawn(data.lostEmbersPosition - Vector3.up * 0.6f, data.lostEmbersAmount);
        }

        void Update()
        {
            SaveSystem.Data.playTimeSeconds += Time.unscaledDeltaTime;
            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= autosaveInterval && !respawning)
            {
                autosaveTimer = 0f;
                SaveNow();
            }
        }

        public void SetRespawn(Bonfire b)
        {
            CurrentBonfire = b;
            var data = SaveSystem.Data;
            data.lastBonfireId = b.bonfireId;
            if (!data.discoveredBonfires.Contains(b.bonfireId)) data.discoveredBonfires.Add(b.bonfireId);
            SaveNow();
        }

        public void OnPlayerDied()
        {
            if (!respawning) StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            respawning = true;
            SaveSystem.Data.deaths++;
            if (HUD.Instance != null) { HUD.Instance.ShowDeath(true); HUD.Instance.HideBoss(); }

            // Old lost embers vanish forever; current embers drop here
            if (CurrentLostEmbers != null) Destroy(CurrentLostEmbers.gameObject);
            CurrentLostEmbers = player.Embers > 0 ? LostEmbers.Spawn(player.transform.position, player.Embers) : null;
            player.Embers = 0;
            SaveNow();

            yield return new WaitForSeconds(respawnDelay);

            ResetAllEnemies();
            if (CurrentBonfire != null) TeleportToBonfire(CurrentBonfire);
            player.RestoreAll();
            if (HUD.Instance != null) HUD.Instance.ShowDeath(false);
            respawning = false;
        }

        public void ResetAllEnemies()
        {
            foreach (var e in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include))
                e.ResetEnemy();
        }

        public void OnLostEmbersRecovered(LostEmbers l)
        {
            if (l == CurrentLostEmbers) CurrentLostEmbers = null;
            SaveNow();
        }

        void TeleportToBonfire(Bonfire b)
        {
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            if (pc != null) pc.Teleport(b.SpawnPosition, b.SpawnRotation);
        }

        public void SaveNow()
        {
            var data = SaveSystem.Data;
            if (player != null) data.embers = player.Embers;
            data.hasLostEmbers = CurrentLostEmbers != null;
            if (CurrentLostEmbers != null)
            {
                data.lostEmbersPosition = CurrentLostEmbers.transform.position;
                data.lostEmbersAmount = CurrentLostEmbers.amount;
            }
            else data.lostEmbersAmount = 0;
            SaveSystem.Save();
        }

        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        void OnApplicationQuit() => SaveNow();
    }
}
