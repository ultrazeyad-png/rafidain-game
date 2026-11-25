using UnityEngine;

namespace Rafidain
{
    /// <summary>A merchant sitting in the town square. Walk up and press E to trade embers for upgrades.</summary>
    public class Merchant : MonoBehaviour
    {
        public string merchantName = "عطار";
        public string greeting = "الجمر عندك... والزيت عندي";
        public float range = 2.6f;
        PlayerStats player;

        void Start() => player = FindAnyObjectByType<PlayerStats>();

        void Update()
        {
            if (player == null || player.IsDead || MerchantUI.IsOpen) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;
            Vector3 to = player.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-4f * Time.deltaTime));
            if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("تاجر") + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact)) MerchantUI.Open(merchantName, greeting);
        }
    }
}
