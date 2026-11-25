using UnityEngine;

namespace Rafidain
{
    [System.Serializable]
    public class MapPieceInfo
    {
        public string id;
        public Vector2 center;   // world x/z
        public float radius = 300f;
        public int seed;
    }

    /// <summary>One per governorate scene: its name, painted map and the two hidden map pieces.</summary>
    public class RegionInfo : MonoBehaviour
    {
        public static RegionInfo Instance { get; private set; }

        public string regionId = "zakho";
        public string regionName = "زاخو";
        public Texture2D mapTexture;
        public float worldSize = 1024f;
        public MapPieceInfo[] pieces = new MapPieceInfo[0];

        void Awake() => Instance = this;
    }
}
