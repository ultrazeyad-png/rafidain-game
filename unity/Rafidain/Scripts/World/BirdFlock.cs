using UnityEngine;

namespace Rafidain
{
    /// <summary>A flock circling the sky (crows over towns, vultures over the guardians' arenas). Built at runtime, very cheap.</summary>
    public class BirdFlock : MonoBehaviour
    {
        public int count = 9;
        public float radius = 30f;
        public float height = 35f;
        public float speed = 0.25f;
        public float birdSize = 0.6f;
        public Color color = new Color(0.06f, 0.05f, 0.05f);

        Transform[] birds;
        Transform[] wingsL, wingsR;
        float[] phase, rad, alt, flap;

        void Start()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = shader != null ? new Material(shader) : null;
            if (mat != null) mat.color = color;

            birds = new Transform[count];
            wingsL = new Transform[count];
            wingsR = new Transform[count];
            phase = new float[count]; rad = new float[count]; alt = new float[count]; flap = new float[count];
            for (int i = 0; i < count; i++)
            {
                var b = new GameObject("Bird").transform;
                b.SetParent(transform, false);
                wingsL[i] = Wing(b, -1f, mat);
                wingsR[i] = Wing(b, 1f, mat);
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(b, false);
                body.transform.localScale = new Vector3(0.18f, 0.12f, 0.6f) * birdSize;
                if (mat != null) body.GetComponent<Renderer>().sharedMaterial = mat;
                birds[i] = b;
                phase[i] = Random.value * Mathf.PI * 2f;
                rad[i] = radius * Random.Range(0.6f, 1.2f);
                alt[i] = height + Random.Range(-6f, 6f);
                flap[i] = Random.Range(5f, 8f);
            }
        }

        Transform Wing(Transform bird, float side, Material mat)
        {
            var pivot = new GameObject("Wing").transform;
            pivot.SetParent(bird, false);
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(w.GetComponent<Collider>());
            w.transform.SetParent(pivot, false);
            w.transform.localPosition = new Vector3(side * 0.45f, 0f, 0f) * birdSize;
            w.transform.localScale = new Vector3(0.9f, 0.03f, 0.35f) * birdSize;
            if (mat != null) w.GetComponent<Renderer>().sharedMaterial = mat;
            return pivot;
        }

        void Update()
        {
            if (birds == null) return;
            float t = Time.time;
            for (int i = 0; i < birds.Length; i++)
            {
                float a = phase[i] + t * speed * (30f / rad[i]);
                var p = new Vector3(Mathf.Cos(a) * rad[i], alt[i] + Mathf.Sin(t * 0.7f + i) * 2f, Mathf.Sin(a) * rad[i]);
                var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                birds[i].localPosition = p;
                birds[i].localRotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, 0f, -18f);
                // flap, then glide
                float cycle = Mathf.Repeat(t * 0.3f + i * 0.17f, 1f);
                float f = cycle < 0.4f ? Mathf.Sin(t * flap[i]) * 35f : 6f;
                wingsL[i].localRotation = Quaternion.Euler(0f, 0f, f);
                wingsR[i].localRotation = Quaternion.Euler(0f, 0f, -f);
            }
        }
    }
}
