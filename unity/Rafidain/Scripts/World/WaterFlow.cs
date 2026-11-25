using UnityEngine;

namespace Rafidain
{
    /// <summary>Moving water: two layers of ripples drifting in different directions, plus a gentle swell.</summary>
    public class WaterFlow : MonoBehaviour
    {
        public Vector2 flowA = new Vector2(0.012f, 0.006f);
        public Vector2 flowB = new Vector2(-0.007f, 0.01f);
        public float swell = 0.05f;

        Material mat;
        Vector3 basePos;
        static readonly int DetailST = Shader.PropertyToID("_DetailAlbedoMap_ST");

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null) mat = r.material;
            basePos = transform.position;
        }

        void Update()
        {
            if (mat == null) return;
            float t = Time.time;
            mat.mainTextureOffset = flowA * t;
            if (mat.HasProperty(DetailST))
            {
                var st = mat.GetVector(DetailST);
                mat.SetVector(DetailST, new Vector4(st.x, st.y, flowB.x * t, flowB.y * t));
            }
            transform.position = basePos + Vector3.up * Mathf.Sin(t * 0.6f) * swell;
        }
    }
}
