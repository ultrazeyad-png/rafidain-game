using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Natural-looking meshes generated in the editor: rocks and lumpy tree canopies
    /// (noise-displaced icospheres), saved as assets so they cost nothing at runtime.
    /// </summary>
    public static class ProceduralMeshes
    {
        const string Folder = "Assets/Rafidain/Generated/Meshes";

        public static Mesh Rock(int seed)
        {
            string path = $"{Folder}/Rock_{seed}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var rng = new System.Random(seed * 7919 + 13);
            var mesh = Icosphere(3, out var verts);
            Vector3 stretch = new Vector3(1f + (float)rng.NextDouble() * 0.6f, 0.55f + (float)rng.NextDouble() * 0.45f, 1f + (float)rng.NextDouble() * 0.5f);
            float o = seed * 17.31f;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                float n = Noise3(v * 1.6f, o) * 0.45f + Noise3(v * 4.2f, o + 50f) * 0.15f + Noise3(v * 9f, o + 90f) * 0.05f;
                // flat-ish facets: quantise a little for a chiselled stone look
                n = Mathf.Round(n * 14f) / 14f;
                v = v * (0.8f + n);
                v = Vector3.Scale(v, stretch);
                if (v.y < -0.25f) v.y = -0.25f + (v.y + 0.25f) * 0.25f; // flattened base sits on ground
                verts[i] = v;
            }
            return Save(mesh, verts, path, flatShade: true);
        }

        public static Mesh Canopy(int seed)
        {
            string path = $"{Folder}/Canopy_{seed}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var mesh = Icosphere(2, out var verts);
            float o = seed * 31.7f + 400f;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                float n = Noise3(v * 2.2f, o) * 0.5f + Noise3(v * 5f, o + 20f) * 0.18f;
                v *= 0.75f + n;
                if (v.y < -0.2f) v.y *= 0.6f;
                verts[i] = v;
            }
            return Save(mesh, verts, path, flatShade: false);
        }

        // ---------------------------------------------------------------

        static Mesh Save(Mesh mesh, List<Vector3> verts, string path, bool flatShade)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                RafidainSceneBuilder.EnsureFolder(Folder);

            var tris = mesh.triangles;
            if (flatShade)
            {
                var fv = new Vector3[tris.Length];
                var ft = new int[tris.Length];
                for (int i = 0; i < tris.Length; i++) { fv[i] = verts[tris[i]]; ft[i] = i; }
                mesh.Clear();
                mesh.vertices = fv;
                mesh.triangles = ft;
            }
            else
            {
                mesh.SetVertices(verts);
            }

            // simple box-projected UVs so textures still read
            var vs = mesh.vertices;
            var uv = new Vector2[vs.Length];
            for (int i = 0; i < vs.Length; i++) uv[i] = new Vector2(vs[i].x + vs[i].z * 0.5f, vs[i].y + vs[i].z * 0.5f);
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static float Noise3(Vector3 p, float o)
        {
            float a = Mathf.PerlinNoise(p.x + o, p.y + o * 0.7f);
            float b = Mathf.PerlinNoise(p.y + o * 1.3f, p.z + o);
            float c = Mathf.PerlinNoise(p.z + o * 0.5f, p.x + o * 1.9f);
            return (a + b + c) / 3f - 0.5f;
        }

        static Mesh Icosphere(int subdivisions, out List<Vector3> verts)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized * 0.5f;

            var tris = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            var cache = new Dictionary<long, int>();
            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = Mid(tris[i], tris[i + 1], verts, cache);
                    int b = Mid(tris[i + 1], tris[i + 2], verts, cache);
                    int c = Mid(tris[i + 2], tris[i], verts, cache);
                    next.AddRange(new[] { tris[i], a, c, tris[i + 1], b, a, tris[i + 2], c, b, a, b, c });
                }
                tris = next;
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            return mesh;
        }

        static int Mid(int a, int b, List<Vector3> verts, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized * 0.5f);
            idx = verts.Count - 1;
            cache[key] = idx;
            return idx;
        }
    }
}
