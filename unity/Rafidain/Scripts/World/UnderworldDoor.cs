using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// The gate of the underworld beneath Baghdad. Needs the five guardian keys.
    /// Opening it sinks the door and wakes Adapa; Ereshkigal speaks from below.
    /// </summary>
    public class UnderworldDoor : MonoBehaviour
    {
        public GameObject boss;
        public Transform door;
        public float range = 6f;
        public int keysNeeded = 5;

        PlayerStats player;
        bool sinking;
        Vector3 doorStart;

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            if (door != null) doorStart = door.localPosition;
            bool opened = SaveSystem.Data.underworldOpened;
            if (opened && door != null) door.gameObject.SetActive(false);
            if (!opened && boss != null) boss.SetActive(false);
        }

        void Update()
        {
            if (sinking && door != null)
            {
                door.localPosition += Vector3.down * 2.2f * Time.deltaTime;
                if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.05f);
                if (door.localPosition.y < doorStart.y - 14f) { door.gameObject.SetActive(false); sinking = false; }
                return;
            }
            if (SaveSystem.Data.underworldOpened || player == null || player.IsDead) return;
            if (Vector3.Distance(player.transform.position, transform.position) > range) return;

            int have = SaveSystem.Data.keys.Count;
            if (have < keysNeeded)
            {
                HUD.Instance?.SetPrompt(have + " / " + keysNeeded + "  " + ArabicText.Fix("الباب يطلب مفاتيح الحراس الخمسة"));
                return;
            }
            HUD.Instance?.SetPrompt(ArabicText.Fix("افتح باب العالم السفلي") + "   [E]");
            var input = PlayerInputReader.Instance;
            if (input != null && input.Consume(GameAction.Interact)) Open();
        }

        void Open()
        {
            SaveSystem.Data.underworldOpened = true;
            SaveSystem.Save();
            sinking = true;
            if (boss != null) boss.SetActive(true);
            if (HUD.Instance != null)
                HUD.Instance.ShowDialog(ArabicText.Fix("صوت من تحت الارض"),
                    ArabicText.Fix("فرات... جئت بمفاتيح الحراس لتسرق مني ما اخذته") + "\n" +
                    ArabicText.Fix("ادابا... لا تدعه يمر"), 7f);
        }
    }
}
