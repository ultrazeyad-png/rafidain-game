using UnityEngine;

namespace Rafidain
{
    /// <summary>
    /// When the guardian wakes and Furat is inside, a wall of mist rises around the arena:
    /// no leaving until one of you falls (souls boss fog). Opens again on death, victory or reset.
    /// </summary>
    public class BossArena : MonoBehaviour
    {
        public EnemyAI boss;
        public float radius = 17.5f;
        public int segments = 18;
        public float wallHeight = 8f;

        GameObject walls;
        ParticleSystem fog;
        PlayerStats player;
        bool sealedIn;

        void Start()
        {
            player = FindAnyObjectByType<PlayerStats>();
            BuildWalls();
            if (boss != null)
            {
                boss.OnEngage += TrySeal;
                boss.OnDied += Open;
                boss.OnReset += Open;
            }
            Open();
        }

        void OnDestroy()
        {
            if (boss == null) return;
            boss.OnEngage -= TrySeal;
            boss.OnDied -= Open;
            boss.OnReset -= Open;
        }

        void Update()
        {
            if (sealedIn && (player == null || player.IsDead)) Open();
        }

        void TrySeal()
        {
            if (player == null || boss == null || boss.IsDead) return;
            Vector3 d = player.transform.position - transform.position; d.y = 0f;
            if (d.magnitude > radius - 0.8f) return;    // only when Furat is already inside
            sealedIn = true;
            walls.SetActive(true);
            fog.Play();
        }

        void Open()
        {
            sealedIn = false;
            if (walls != null) walls.SetActive(false);
            if (fog != null) fog.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void BuildWalls()
        {
            walls = new GameObject("Mist Walls");
            walls.transform.SetParent(transform, false);
            float segLen = 2f * Mathf.PI * radius / segments * 1.15f;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var seg = new GameObject("Wall");
                seg.transform.SetParent(walls.transform, false);
                seg.transform.localPosition = new Vector3(Mathf.Cos(a) * radius, wallHeight * 0.5f, Mathf.Sin(a) * radius);
                seg.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
                var box = seg.AddComponent<BoxCollider>();
                box.size = new Vector3(segLen, wallHeight, 0.6f);
            }

            // pale mist drifting up along the circle
            var fgo = new GameObject("Mist");
            fgo.transform.SetParent(transform, false);
            fgo.transform.localPosition = Vector3.up * 0.3f;
            fgo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            fog = fgo.AddComponent<ParticleSystem>();
            fog.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = fog.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startColor = new Color(0.85f, 0.82f, 0.9f, 0.16f);
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = fog.emission; em.rateOverTime = 70f;
            var shape = fog.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0f;      // emit on the edge only
            var col = fog.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = fgo.GetComponent<ParticleSystemRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null)
            {
                var m = new Material(sh);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                r.sharedMaterial = m;
            }
        }
    }
}
