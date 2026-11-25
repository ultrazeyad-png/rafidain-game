using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// A glowing clay tablet on the ground left by those who walked the road before (souls-style message).
    /// Walk up and press E to read it.
    /// </summary>
    public class GroundMessage : MonoBehaviour
    {
        public string title = "نقش على الطين";
        [TextArea] public string body;
        public float range = 2.2f;
        public Light glow;

        PlayerStats player;
        float baseIntensity;
        bool read;

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            if (glow != null) baseIntensity = glow.intensity;
        }

        void Update()
        {
            if (glow != null)
                glow.intensity = baseIntensity * (read ? 0.45f : 0.75f + 0.25f * Mathf.Sin(Time.time * 2.4f + transform.position.x));

            if (player == null || player.IsDead || string.IsNullOrEmpty(body)) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;

            if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("اقرأ النقش") + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact))
            {
                read = true;
                if (HUD.Instance != null) HUD.Instance.ShowDialog(ArabicText.Fix(title), ArabicText.Fix(body), 6f);
                if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 0.4f);
            }
        }
    }
}
