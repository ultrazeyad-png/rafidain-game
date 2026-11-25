using UnityEngine;

namespace Rafidain
{
    /// <summary>A weapon lying in the world. Taking it equips it; you can switch weapons at any flame.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public string weaponId = "khanjar";
        public float range = 2f;
        PlayerStats player;

        void Start()
        {
            if (SaveSystem.Data.weapons.Contains(weaponId)) { gameObject.SetActive(false); return; }
            player = FindAnyObjectByType<PlayerStats>();
        }

        void Update()
        {
            if (player == null || player.IsDead) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;
            var def = Weapons.Get(weaponId);
            if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("خذ " + def.name) + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input == null || !input.Consume(GameAction.Interact)) return;

            SaveSystem.Data.weapons.Add(weaponId);
            var combat = player.GetComponent<PlayerCombat>();
            if (combat != null) combat.Equip(weaponId);
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();
            if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 1f);
            if (HUD.Instance != null)
                HUD.Instance.ShowDialog(ArabicText.Fix(def.name),
                    ArabicText.Fix(def.description) + "\n" + ArabicText.Fix("تكدر تبدل سلاحك عند اي شعلة"), 6f);
            gameObject.SetActive(false);
        }
    }
}
