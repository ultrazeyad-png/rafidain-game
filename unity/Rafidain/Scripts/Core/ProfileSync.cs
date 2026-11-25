using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Rafidain
{
    /// <summary>
    /// Sends the player's progress to the Next Level website profile.
    /// DISABLED by default: nothing is sent until an endpoint is set,
    /// either in code (DefaultEndpoint) or at runtime via ProfileSync.SetEndpoint(url).
    /// Payload: see README section "ربط البروفايل بموقع Next Level".
    /// </summary>
    public static class ProfileSync
    {
        /// <summary>Put the site URL here when the API is ready, e.g. "https://nextlevel.example/api/rafidain/profile".</summary>
        public const string DefaultEndpoint = "";
        public const int ApiVersion = 1;
        const float MinInterval = 60f;

        const string UrlKey = "rafidain_sync_url";
        const string IdKey = "rafidain_player_id";
        const string LinkKey = "rafidain_link_code";

        static float lastSent = -999f;
        static Runner runner;

        public static string Endpoint => PlayerPrefs.GetString(UrlKey, DefaultEndpoint);
        public static bool Enabled => !string.IsNullOrEmpty(Endpoint);
        public static string LastStatus { get; private set; } = "off";

        /// <summary>Anonymous device id (created once).</summary>
        public static string PlayerId
        {
            get
            {
                string id = PlayerPrefs.GetString(IdKey, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(IdKey, id);
                    PlayerPrefs.Save();
                }
                return id;
            }
        }

        /// <summary>Code the player copies from their Next Level profile page to link this device.</summary>
        public static string LinkCode
        {
            get => PlayerPrefs.GetString(LinkKey, "");
            set { PlayerPrefs.SetString(LinkKey, value ?? ""); PlayerPrefs.Save(); }
        }

        public static void SetEndpoint(string url)
        {
            PlayerPrefs.SetString(UrlKey, url ?? "");
            PlayerPrefs.Save();
        }

        [Serializable]
        class Payload
        {
            public int apiVersion;
            public string playerId;
            public string linkCode;
            public string gameVersion;
            public string platform;
            public long sentAtUnix;
            public int level;
            public int vigor, endurance, strength;
            public int embers;
            public int deaths;
            public float playTimeSeconds;
            public string currentScene;
            public string[] defeatedBosses;
            public string[] keys;
            public int mapPieces;
            public int sacredOils;
            public int discoveredBonfires;
            public bool endingSeen;
        }

        /// <summary>Called after every save. Throttled; pass force on quit.</summary>
        public static void Push(bool force = false)
        {
            if (!Enabled) { LastStatus = "off"; return; }
            if (!force && Time.realtimeSinceStartup - lastSent < MinInterval) return;
            lastSent = Time.realtimeSinceStartup;

            var d = SaveSystem.Data;
            var p = new Payload
            {
                apiVersion = ApiVersion,
                playerId = PlayerId,
                linkCode = LinkCode,
                gameVersion = Application.version,
                platform = Application.platform.ToString(),
                sentAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                level = d.level,
                vigor = d.vigor, endurance = d.endurance, strength = d.strength,
                embers = d.embers,
                deaths = d.deaths,
                playTimeSeconds = d.playTimeSeconds,
                currentScene = d.currentScene,
                defeatedBosses = d.defeatedBosses.ToArray(),
                keys = d.keys.ToArray(),
                mapPieces = d.mapPieces.Count,
                sacredOils = d.sacredOils.Count,
                discoveredBonfires = d.discoveredBonfires.Count,
                endingSeen = d.endingSeen,
            };
            string json = JsonUtility.ToJson(p);

            if (runner == null)
            {
                var go = new GameObject("_ProfileSync");
                go.hideFlags = HideFlags.HideInHierarchy;
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            runner.StartCoroutine(Send(Endpoint, json));
        }

        static IEnumerator Send(string url, string json)
        {
            LastStatus = "sending";
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    LastStatus = "ok";
                else
                {
                    LastStatus = "error: " + req.error;
                    Debug.LogWarning("[Rafidain] Profile sync failed: " + req.error);
                }
            }
        }

        class Runner : MonoBehaviour { }
    }
}
