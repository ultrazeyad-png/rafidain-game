using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Rafidain
{
    [System.Serializable]
    public class SaveData
    {
        public int embers;
        public bool introSeen;
        public int level = 1;
        public int vigor, endurance, strength;
        public List<string> sacredOils = new List<string>();
        public string currentScene = "";
        public bool underworldOpened;
        public bool endingSeen;
        public string lastBonfireId = "";
        public List<string> discoveredBonfires = new List<string>();
        public List<string> defeatedBosses = new List<string>();
        public List<string> keys = new List<string>();       // the 5 guardian keys
        public List<string> mapPieces = new List<string>();  // 2 per governorate
        public List<string> weapons = new List<string> { "sword" };
        public string equippedWeapon = "sword";
        public List<string> bossIntrosSeen = new List<string>();
        public List<string> visitedRegions = new List<string>();
        public int ngCycle;
        public int flaskPotency, sharpness, guard;   // bought from the merchants (0..5 each)   // 0 = first journey, 1 = journey+ ...
        public float playTimeSeconds;
        public int deaths;
        public bool hasLostEmbers;
        public Vector3 lostEmbersPosition;
        public int lostEmbersAmount;
    }

    /// <summary>Simple JSON save on the device. Later this same data is sent to the Next Level website profile.</summary>
    public static class SaveSystem
    {
        static SaveData data;
        static string FilePath => Path.Combine(Application.persistentDataPath, "rafidain_save.json");

        public static SaveData Data
        {
            get { if (data == null) Load(); return data; }
        }

        public static void Load()
        {
            try
            {
                data = File.Exists(FilePath)
                    ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath))
                    : new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Rafidain] Save file unreadable, starting fresh: " + e.Message);
                data = new SaveData();
            }
            if (data == null) data = new SaveData();
        }

        public static void Save()
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true)); }
            catch (System.Exception e) { Debug.LogWarning("[Rafidain] Could not save: " + e.Message); }
        }

        /// <summary>Begin the journey again from Zakho, keeping Furat's strength, weapons and oils; the guardians return stronger.</summary>
        public static void StartNewGamePlus()
        {
            var d = Data;
            d.ngCycle++;
            d.defeatedBosses.Clear();
            d.keys.Clear();
            d.mapPieces.Clear();
            d.discoveredBonfires.Clear();
            d.lastBonfireId = "";
            d.currentScene = "Zakho_Region";
            d.underworldOpened = false;
            d.endingSeen = false;
            d.hasLostEmbers = false;
            d.lostEmbersAmount = 0;
            Save();
        }

        public static void DeleteSave()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            data = new SaveData();
        }
    }
}
