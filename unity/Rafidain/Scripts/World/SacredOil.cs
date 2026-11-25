using UnityEngine;

namespace Rafidain
{
    /// <summary>A jar of sacred oil hidden in each governorate: one more healing flask for the rest of the journey.</summary>
    public class SacredOil : MonoBehaviour
    {
        public string oilId;
        public float range = 2f;
        PlayerStats player;

        void Start()
        {
            if (SaveSystem.Data.sacredOils.Contains(oilId)) { gameObject.SetActive(false); return; }
            player = FindAnyObjectByType<PlayerStats>();
        }

        void Update()
        {
            transform.Rotate(0f, 30f * Time.deltaTime, 0f);
            if (player == null || player.IsDead) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;
            if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("خذ جرة الزيت") + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input == null || !input.Consume(GameAction.Interact)) return;

            SaveSystem.Data.sacredOils.Add(oilId);
            player.ApplyLevels();
            player.RestoreAll();
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
            if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 1f);
            if (HUD.Instance != null)
                HUD.Instance.ShowDialog(ArabicText.Fix("زيت مقدس"),
                    ArabicText.Fix("زيت من معابد اشور القديمة") + "\n" +
                    player.maxFlasks + "  " + ArabicText.Fix("عدد قواريرك الان"), 5f);
            gameObject.SetActive(false);
        }
    }
}
