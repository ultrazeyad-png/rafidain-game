using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Synthesises the game's sounds in the editor (no audio files needed) and saves them as WAV assets:
    /// valley wind, fire crackle, river, sword swing, flesh hit, bronze shield clang, footstep, roll,
    /// ember pickup, death toll and the guardian's drone.
    /// </summary>
    public static class ProceduralAudio
    {
        const string Folder = "Assets/Rafidain/Generated/Audio";
        const int Rate = 44100;

        public static AudioClip Get(string name)
        {
            // real recordings dropped in Assets/Rafidain/Sounds/<name> win over the generated placeholder
            var real = RealAssets.Clips(name);
            if (real.Length > 0) return real[0];

            string path = $"{Folder}/{name}.wav";
            var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (existing != null) return existing;
            RafidainSceneBuilder.EnsureFolder(Folder);

            float[] s;
            switch (name)
            {
                case "wind": s = Wind(12f); break;
                case "fire": s = Fire(6f); break;
                case "river": s = River(8f); break;
                case "swing": s = Swing(); break;
                case "hit": s = Hit(); break;
                case "block": s = Clang(); break;
                case "step": s = Step(); break;
                case "roll": s = Roll(); break;
                case "pickup": s = Pickup(); break;
                case "death": s = Toll(); break;
                case "boss": s = Drone(16f); break;
                default: return null;
            }
            Normalize(s, name == "boss" || name == "wind" ? 0.6f : 0.9f);
            WriteWav(path, s);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp != null)
            {
                bool longClip = name == "wind" || name == "boss" || name == "river" || name == "fire";
                imp.forceToMono = true;
                imp.loadInBackground = longClip;
                var settings = imp.defaultSampleSettings;
                settings.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                imp.defaultSampleSettings = settings;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ------------------------------------------------------------------ sounds

        static float[] Wind(float sec)
        {
            int n = (int)(sec * Rate);
            var s = new float[n];
            var rng = new System.Random(1);
            float brown = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                brown = brown * 0.998f + (float)(rng.NextDouble() * 2 - 1) * 0.04f;
                float gust = 0.45f + 0.35f * Mathf.Sin(t * 0.7f) + 0.2f * Mathf.Sin(t * 1.9f + 1f);
                float cutoff = 0.02f + 0.03f * gust;
                lp += cutoff * (brown - lp);
                s[i] = lp * gust;
            }
            return Loop(s, Rate);
        }

        static float[] Fire(float sec)
        {
            int n = (int)(sec * Rate);
            var s = new float[n];
            var rng = new System.Random(2);
            float rumble = 0f;
            for (int i = 0; i < n; i++)
            {
                float white = (float)(rng.NextDouble() * 2 - 1);
                rumble += 0.01f * (white - rumble);
                s[i] = rumble * 0.8f;
            }
            // crackles: short bright pops
            int pops = (int)(sec * 22);
            for (int p = 0; p < pops; p++)
            {
                int start = rng.Next(n - 2000);
                float amp = 0.2f + (float)rng.NextDouble() * 0.6f;
                int len = 80 + rng.Next(500);
                for (int k = 0; k < len; k++)
                    s[start + k] += (float)(rng.NextDouble() * 2 - 1) * amp * Mathf.Exp(-k / (len * 0.25f));
            }
            return Loop(s, Rate / 2);
        }

        static float[] River(float sec)
        {
            int n = (int)(sec * Rate);
            var s = new float[n];
            var rng = new System.Random(3);
            float lp = 0f, hp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.25f * (white - lp);
                hp = 0.97f * (hp + lp - prev);
                prev = lp;
                float bubble = 0.6f + 0.4f * Mathf.PerlinNoise(t * 3f, 0.5f);
                s[i] = hp * bubble;
            }
            return Loop(s, Rate);
        }

        static float[] Swing()
        {
            int n = (int)(0.32f * Rate);
            var s = new float[n];
            var rng = new System.Random(4);
            float bp = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float env = Mathf.Sin(k * Mathf.PI) * Mathf.Sin(k * Mathf.PI);
                float cutoff = 0.05f + 0.35f * k;
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += cutoff * (white - lp);
                bp += 0.5f * (lp - bp);
                s[i] = (lp - bp) * env * 3f;
            }
            return s;
        }

        static float[] Hit()
        {
            int n = (int)(0.25f * Rate);
            var s = new float[n];
            var rng = new System.Random(5);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float thud = Mathf.Sin(2f * Mathf.PI * (85f - 40f * t) * t) * Mathf.Exp(-t * 18f);
                float slap = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 60f) * 0.6f;
                s[i] = thud + slap;
            }
            return s;
        }

        static float[] Clang()
        {
            int n = (int)(0.9f * Rate);
            var s = new float[n];
            float[] partials = { 410f, 987f, 1523f, 2210f, 3120f };
            float[] decay = { 5f, 7f, 9f, 12f, 16f };
            var rng = new System.Random(6);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int p = 0; p < partials.Length; p++)
                    v += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * Mathf.Exp(-t * decay[p]) / (p + 1);
                v += (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 90f) * 0.5f;
                s[i] = v;
            }
            return s;
        }

        static float[] Step()
        {
            int n = (int)(0.14f * Rate);
            var s = new float[n];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.15f * (white - lp);
                s[i] = (lp * 1.5f + Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.5f) * Mathf.Exp(-t * 35f);
            }
            return s;
        }

        static float[] Roll()
        {
            int n = (int)(0.5f * Rate);
            var s = new float[n];
            var rng = new System.Random(8);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += 0.3f * (white - lp);
                float grit = rng.NextDouble() < 0.02 ? (float)(rng.NextDouble() * 2 - 1) : 0f;
                s[i] = (lp * 0.7f + grit) * Mathf.Sin(k * Mathf.PI) * 0.8f;
            }
            return s;
        }

        static float[] Pickup()
        {
            int n = (int)(0.9f * Rate);
            var s = new float[n];
            float[] notes = { 523f, 659f, 784f, 1047f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * 0.08f;
                    if (t < start) continue;
                    float tt = t - start;
                    v += Mathf.Sin(2f * Mathf.PI * notes[k] * tt) * Mathf.Exp(-tt * 4f) * 0.3f;
                }
                s[i] = v;
            }
            return s;
        }

        static float[] Toll()
        {
            int n = (int)(4f * Rate);
            var s = new float[n];
            float[] partials = { 55f, 110f, 132f, 165f, 220f, 293f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int p = 0; p < partials.Length; p++)
                    v += Mathf.Sin(2f * Mathf.PI * partials[p] * t + p) * Mathf.Exp(-t * (0.8f + p * 0.4f)) / (p * 0.6f + 1f);
                s[i] = v;
            }
            return s;
        }

        // Ominous drone for guardian fights: low fifths with slow beating and a heartbeat-like pulse
        static float[] Drone(float sec)
        {
            int n = (int)(sec * Rate);
            var s = new float[n];
            var rng = new System.Random(9);
            float noiseLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 55.4f * t) * 0.4f
                        + Mathf.Sin(2f * Mathf.PI * 82.4f * t) * 0.3f * (0.6f + 0.4f * Mathf.Sin(t * 0.5f))
                        + Mathf.Sin(2f * Mathf.PI * 41.2f * t) * 0.35f;
                float beat = t % 2f;
                v += Mathf.Sin(2f * Mathf.PI * 48f * beat) * Mathf.Exp(-beat * 9f) * 0.9f;
                float b2 = (t + 0.35f) % 2f;
                v += Mathf.Sin(2f * Mathf.PI * 44f * b2) * Mathf.Exp(-b2 * 11f) * 0.6f;
                noiseLp += 0.004f * ((float)(rng.NextDouble() * 2 - 1) - noiseLp);
                v += noiseLp * 3f;
                s[i] = v;
            }
            return Loop(s, Rate);
        }

        // ------------------------------------------------------------------ helpers

        // crossfade the tail into the head so the clip loops without a click
        static float[] Loop(float[] s, int fade)
        {
            int n = s.Length - fade;
            var o = new float[n];
            Array.Copy(s, o, n);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = s[i] * k + s[n + i] * (1f - k);
            }
            return o;
        }

        static void Normalize(float[] s, float peak)
        {
            float max = 0.0001f;
            foreach (var v in s) max = Mathf.Max(max, Mathf.Abs(v));
            float g = peak / max;
            for (int i = 0; i < s.Length; i++) s[i] *= g;
        }

        static void WriteWav(string path, float[] s)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = s.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (var v in s) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
            }
        }
    }
}
