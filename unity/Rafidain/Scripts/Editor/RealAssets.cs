using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Real art and sound override the procedural placeholders when you drop them in these folders:
    ///   Assets/Rafidain/Nature/Trees   Rocks   Bushes   Palms   (prefabs or models, e.g. Poly Haven / Asset Store)
    ///   Assets/Rafidain/Nature/Grass   (grass / flower textures with alpha, for terrain details)
    ///   Assets/Rafidain/Nature/Sky     (an HDRI, e.g. from Poly Haven, used as the sky of every governorate)
    ///   Assets/Rafidain/Sounds/swing   hit   block   step   roll   pickup   death   wind   boss   fire   river   parry
    ///     (WAV/OGG files: several per folder = random variations each time)
    /// Empty folder = the generated placeholder is used. Rebuild the governorates after adding assets.
    /// </summary>
    public static class RealAssets
    {
        public const string NatureRoot = "Assets/Rafidain/Nature";
        public const string SoundRoot = "Assets/Rafidain/Sounds";

        static readonly Dictionary<string, GameObject[]> prefabCache = new Dictionary<string, GameObject[]>();

        [MenuItem("Rafidain/Real Assets/Create Folders")]
        public static void CreateFolders()
        {
            foreach (var k in new[] { "Trees", "Rocks", "Bushes", "Palms", "Grass", "Sky" }) RafidainSceneBuilder.EnsureFolder($"{NatureRoot}/{k}");
            foreach (var k in new[] { "swing", "hit", "block", "step", "roll", "pickup", "death", "wind", "boss", "fire", "river", "parry" })
                RafidainSceneBuilder.EnsureFolder($"{SoundRoot}/{k}");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Rafidain", "Folders created under Assets/Rafidain/Nature and Assets/Rafidain/Sounds.\n\nDrop real models / sounds in them, then Rafidain > Build ALL Governorates.", "OK");
        }

        [MenuItem("Rafidain/Real Assets/Report")]
        public static void Report()
        {
            ClearCache();
            var lines = new List<string>();
            foreach (var k in new[] { "Trees", "Rocks", "Bushes", "Palms" }) lines.Add($"{k}: {Prefabs(k).Length} models");
            lines.Add($"Grass: {Textures("Grass").Length} textures");
            lines.Add($"Sky: {(Sky() != null ? "yes" : "no")}");
            foreach (var k in new[] { "swing", "hit", "block", "step", "roll", "pickup", "death", "wind", "boss", "fire", "river", "parry" })
                lines.Add($"sound {k}: {Clips(k).Length}");
            EditorUtility.DisplayDialog("Rafidain - real assets", string.Join("\n", lines), "OK");
        }

        public static void ClearCache() => prefabCache.Clear();

        static string[] Guids(string folder, string filter)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new string[0];
            return AssetDatabase.FindAssets(filter, new[] { folder });
        }

        public static GameObject[] Prefabs(string kind)
        {
            if (prefabCache.TryGetValue(kind, out var cached)) return cached;
            string folder = $"{NatureRoot}/{kind}";
            var list = Guids(folder, "t:Prefab").Concat(Guids(folder, "t:Model")).Distinct()
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(go => go != null).ToArray();
            prefabCache[kind] = list;
            return list;
        }

        /// <summary>Places a random real model of this kind; returns null when the folder is empty (use the placeholder).</summary>
        public static GameObject Place(string kind, Transform parent, Vector3 position, float yaw, float scale)
        {
            var all = Prefabs(kind);
            if (all.Length == 0) return null;
            var src = all[Random.Range(0, all.Length)];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            if (go == null) go = Object.Instantiate(src, parent);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = src.transform.localScale * scale;
            go.isStatic = true;
            return go;
        }

        public static Texture2D[] Textures(string kind)
        {
            return Guids($"{NatureRoot}/{kind}", "t:Texture2D")
                .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null).ToArray();
        }

        public static Texture Sky()
        {
            var g = Guids($"{NatureRoot}/Sky", "t:Texture");
            return g.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(g[0]));
        }

        public static AudioClip[] Clips(string name)
        {
            return Guids($"{SoundRoot}/{name}", "t:AudioClip")
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null).OrderBy(c => c.name).ToArray();
        }
    }
}
