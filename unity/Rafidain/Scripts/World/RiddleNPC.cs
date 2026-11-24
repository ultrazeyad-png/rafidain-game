using UnityEngine;

namespace Rafidain
{
    /// <summary>A wanderer who speaks in riddles that point toward a hidden map piece. Lines are shown one per E press.</summary>
    public class RiddleNPC : MonoBehaviour
    {
        public string speaker = "عابر سبيل";
        [TextArea] public string[] lines;   // each entry may contain \n for a second line
        public float range = 3f;

        PlayerStats player;
        int index;

        void Start() => player = FindAnyObjectByType<PlayerStats>();

        void Update()
        {
            if (player == null || player.IsDead || lines == null || lines.Length == 0) return;
            float d = Vector3.Distance(player.transform.position, transform.position);
            if (d > range) { if (index > 0 && d > range * 2f) index = 0; return; }

            // face the player
            Vector3 to = player.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-4f * Time.deltaTime));

            if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("تحدث") + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact))
            {
                if (HUD.Instance != null)
                    HUD.Instance.ShowDialog(ArabicText.Fix(speaker), ArabicText.Fix(lines[index]), 7f);
                index = (index + 1) % lines.Length;
            }
        }
    }
}
