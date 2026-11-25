using System.Collections;
using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// Impact feel: a split-second hit-stop, sparks/dust bursting from the hit point and camera kick.
    /// Everything is created at runtime and pooled (one particle system for all hits).
    /// </summary>
    public class HitFX : MonoBehaviour
    {
        static HitFX instance;
        ParticleSystem burst;
        Coroutine stopCo;

        static HitFX Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("_HitFX");
            instance = go.AddComponent<HitFX>();
            instance.Build();
            return instance;
        }

        void Build()
        {
            burst = gameObject.AddComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.gravityModifier = 1.2f;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            var em = burst.emission;
            em.rateOverTime = 0f;
            var shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var col = burst.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var m = new Material(shader);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
            }
        }

        /// <summary>Call when a blow lands. heavy = bigger stop, more sparks, stronger kick.</summary>
        public static void Hit(Vector3 point, bool heavy, Color color)
        {
            var fx = Get();
            fx.Emit(point, heavy ? 34 : 16, color);
            fx.Stop(heavy ? 0.11f : 0.055f);
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(heavy ? 0.3f : 0.12f);
        }

        /// <summary>Dust kicked up (rolls, boss landings).</summary>
        public static void Dust(Vector3 point, int count)
        {
            Get().Emit(point, count, new Color(0.65f, 0.58f, 0.48f, 0.6f), 0.6f);
        }

        /// <summary>A ring of particles on the ground (boss shockwaves).</summary>
        public static void Ring(Vector3 center, float radius, Color color, int count = 48)
        {
            var fx = Get();
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                var p = center + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * radius;
                fx.Emit(p, 1, color, 0.35f);
            }
        }

        void Emit(Vector3 point, int count, Color color, float speedMul = 1f)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = point,
                applyShapeToPosition = true,
                startColor = color,
            };
            for (int i = 0; i < count; i++)
            {
                p.velocity = Random.onUnitSphere * Random.Range(2f, 7f) * speedMul + Vector3.up * 1.5f;
                burst.Emit(p, 1);
            }
        }

        void Stop(float seconds)
        {
            if (PauseMenu.IsPaused || WorldMapUI.IsOpen || LevelUpUI.IsOpen) return;
            if (stopCo != null) StopCoroutine(stopCo);
            stopCo = StartCoroutine(StopRoutine(seconds));
        }

        IEnumerator StopRoutine(float seconds)
        {
            Time.timeScale = 0.06f;
            yield return new WaitForSecondsRealtime(seconds);
            if (!PauseMenu.IsPaused && !WorldMapUI.IsOpen && !LevelUpUI.IsOpen) Time.timeScale = 1f;
            stopCo = null;
        }

        // ---------------------------------------------------------------- blood pools

        static Material bloodMat;

        /// <summary>A dark pool spreads where an enemy fell, then slowly sinks into the ground and disappears.</summary>
        public static void BloodPool(Vector3 at, float size = 1f)
        {
            if (!Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider.GetComponentInParent<EnemyAI>() != null || hit.collider.GetComponentInParent<PlayerStats>() != null) return;
            if (bloodMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) return;
                bloodMat = new Material(sh);
                bloodMat.color = new Color(0.18f, 0.02f, 0.015f);
                if (bloodMat.HasProperty("_Smoothness")) bloodMat.SetFloat("_Smoothness", 0.85f);   // looks wet
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Blood";
            Object.Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = bloodMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.position = hit.point + hit.normal * 0.012f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Get().StartCoroutine(Get().BloodRoutine(go.transform, size * Random.Range(0.8f, 1.25f)));
        }

        IEnumerator BloodRoutine(Transform t, float size)
        {
            Vector3 full = new Vector3(size * 1.3f, 0.006f, size);
            Vector3 start = t.position;
            float k = 0f;
            while (k < 1f && t != null)   // spread
            {
                k += Time.deltaTime / 1.6f;
                t.localScale = Vector3.Lerp(new Vector3(0.1f, 0.006f, 0.1f), full, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            yield return new WaitForSeconds(20f);
            k = 0f;
            while (k < 1f && t != null)   // sink away
            {
                k += Time.deltaTime / 4f;
                t.position = start - t.up * 0.03f * k;
                t.localScale = full * (1f - 0.5f * k);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }
    }
}
