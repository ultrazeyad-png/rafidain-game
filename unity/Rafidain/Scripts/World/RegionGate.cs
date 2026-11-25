using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rafidain
{
    /// <summary>Fog gate between governorates. Locked until this region's guardian is defeated.</summary>
    public class RegionGate : MonoBehaviour
    {
        public string targetScene;
        public string label = "الطريق";
        public string requiredBossId;
        public float range = 4.5f;

        PlayerStats player;

        bool Locked => !string.IsNullOrEmpty(requiredBossId) && !SaveSystem.Data.defeatedBosses.Contains(requiredBossId);

        void Start() => player = FindAnyObjectByType<PlayerStats>();

        void Update()
        {
            if (player == null || player.IsDead) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;
            if (HUD.Instance == null) return;

            if (Locked)
            {
                HUD.Instance.SetPrompt(ArabicText.Fix("الضباب لا ينقشع... اهزم الحارس اولا"));
                return;
            }
            HUD.Instance.SetPrompt(ArabicText.Fix(label) + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact)) Travel();
        }

        void Travel()
        {
            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                if (HUD.Instance != null) HUD.Instance.ShowMessage(ArabicText.Fix("هذه المحافظة لم تبن بعد"), 2.5f);
                return;
            }
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
            var d = SaveSystem.Data;
            d.currentScene = targetScene;
            d.lastBonfireId = "";
            d.hasLostEmbers = false;   // embers left behind are lost on the road
            d.lostEmbersAmount = 0;
            SaveSystem.Save();
            SceneLoader.Load(targetScene);
        }
    }
}
