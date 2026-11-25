using UnityEngine;

namespace Rafidain
{
    /// <summary>On weaker devices: shorter grass/detail distance, coarser terrain, lower LOD bias, fewer particles.</summary>
    public class PerformanceTuner : MonoBehaviour
    {
        void Start()
        {
            bool low = Application.isMobilePlatform && !DeviceQuality.IsStrongPhone();
            bool phone = Application.isMobilePlatform;
            QualitySettings.lodBias = low ? 0.6f : phone ? 0.85f : 1.2f;

            foreach (var t in FindObjectsByType<Terrain>())
            {
                t.detailObjectDistance = low ? 45f : phone ? 70f : 110f;
                t.detailObjectDensity = low ? 0.5f : phone ? 0.75f : 1f;
                t.heightmapPixelError = low ? 8f : phone ? 5f : 3f;
                t.basemapDistance = low ? 150f : phone ? 300f : 500f;
            }

            if (low)
            {
                foreach (var ps in FindObjectsByType<ParticleSystem>())
                {
                    var main = ps.main;
                    main.maxParticles = Mathf.Max(8, main.maxParticles / 2);
                }
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Point) l.shadows = LightShadows.None;
            }
        }
    }
}
