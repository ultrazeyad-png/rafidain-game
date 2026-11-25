using UnityEngine;

namespace Rafidain
{
    /// <summary>A glowing clay tablet: half of this governorate's map. Walk to it and press E.</summary>
    public class MapPiece : MonoBehaviour
    {
        public string pieceId;
        public float range = 2.2f;

        PlayerStats player;
        Transform visual;
        float baseY;

        void Start()
        {
            if (SaveSystem.Data.mapPieces.Contains(pieceId)) { gameObject.SetActive(false); return; }
            player = FindAnyObjectByType<PlayerStats>();
            visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (visual != null) baseY = visual.localPosition.y;
        }

        void Update()
        {
            if (visual != null)
            {
                visual.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
                var p = visual.localPosition; p.y = baseY + Mathf.Sin(Time.time * 1.6f) * 0.08f; visual.localPosition = p;
            }
            if (player == null || player.IsDead) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;

            if (HUD.Instance != null) HUD.Instance.SetPrompt(ArabicText.Fix("التقط اللوح") + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact)) Collect();
        }

        void Collect()
        {
            if (!SaveSystem.Data.mapPieces.Contains(pieceId)) SaveSystem.Data.mapPieces.Add(pieceId);
            if (AudioManager.Instance != null) AudioManager.Instance.Ui(AudioManager.Instance.pickup, 0.9f);
            if (GameManager.Instance != null) GameManager.Instance.SaveNow();

            int found = 0;
            var info = RegionInfo.Instance;
            if (info != null) foreach (var pc in info.pieces) if (SaveSystem.Data.mapPieces.Contains(pc.id)) found++;
            int total = info != null ? info.pieces.Length : 2;

            if (HUD.Instance != null)
                HUD.Instance.ShowDialog(ArabicText.Fix("لوح طيني"),
                    ArabicText.Fix("قطعة من خريطة " + (info != null ? info.regionName : "")) + "\n" +
                    found + " / " + total + "   " + ArabicText.Fix("اضغط M لترى الخريطة"), 5f);
            gameObject.SetActive(false);
        }
    }
}
