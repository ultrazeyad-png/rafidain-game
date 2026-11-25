using System.Collections;
using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Plays the game's sounds: one-shots in 3D space, the wind bed on the camera,
    /// and the guardian drone that fades in when a boss engages.
    /// Clips are generated and assigned by the scene builders.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioClip swing, hit, block, step, roll, pickup, death, wind, boss;
        [Range(0f, 1f)] public float masterVolume = 1f;

        AudioSource windSource, bossSource, uiSource;
        Coroutine bossFade;

        void Awake()
        {
            Instance = this;
            masterVolume = PlayerPrefs.GetFloat("rafidain_volume", 1f);
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.spatialBlend = 0f;
            bossSource = gameObject.AddComponent<AudioSource>();
            bossSource.clip = boss;
            bossSource.loop = true;
            bossSource.spatialBlend = 0f;
            bossSource.volume = 0f;
        }

        void Start()
        {
            if (wind != null && Camera.main != null)
            {
                windSource = Camera.main.gameObject.AddComponent<AudioSource>();
                windSource.clip = wind;
                windSource.loop = true;
                windSource.spatialBlend = 0f;
                windSource.volume = 0.35f * masterVolume;
                windSource.Play();
            }
            AudioListener.volume = masterVolume;
        }

        public static void At(AudioClip clip, Vector3 pos, float volume = 1f, float pitchJitter = 0.08f)
        {
            if (clip == null) return;
            var go = new GameObject("SFX");
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 1f;
            src.minDistance = 2f;
            src.maxDistance = 40f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.volume = volume;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.Play();
            Destroy(go, clip.length / Mathf.Max(0.1f, src.pitch) + 0.1f);
        }

        public void Ui(AudioClip clip, float volume = 1f)
        {
            if (clip != null) uiSource.PlayOneShot(clip, volume);
        }

        public void BossMusic(bool on)
        {
            if (boss == null) return;
            if (bossFade != null) StopCoroutine(bossFade);
            bossFade = StartCoroutine(FadeBoss(on ? 0.75f : 0f));
        }

        IEnumerator FadeBoss(float target)
        {
            if (target > 0f && !bossSource.isPlaying) bossSource.Play();
            while (!Mathf.Approximately(bossSource.volume, target))
            {
                bossSource.volume = Mathf.MoveTowards(bossSource.volume, target, Time.unscaledDeltaTime * 0.5f);
                yield return null;
            }
            if (target <= 0f) bossSource.Stop();
        }
    }
}
