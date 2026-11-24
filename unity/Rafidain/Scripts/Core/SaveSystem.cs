using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Rafidain
{
    [System.Serializable]
    public class SaveData
    {
        public int embers;
        public string lastBonfireId = "";
        public List<string> discoveredBonfires = new List<string>();
        public List<string> defeatedBosses = new List<string>();
        public List<string> keys = new List<string>();       // the 5 guardian keys
        public List<string> mapPieces = new List<string>();  // 2 per governorate
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

        public static void DeleteSave()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            data = new SaveData();
        }
    }
}
