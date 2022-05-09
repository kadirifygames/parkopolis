using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Hava durumu efektleri: kar, yağmur ve martılar.
    /// Kar ve yağmur, sahnenin üstündeki görünmez bir kutudan sürekli parçacık düşürür.
    /// </summary>
    public static class WeatherFx
    {
        public static void Create(Transform world, Theme t, RoadLayout L)
        {
            switch (t.Weather)
            {
                case Weather.Snow: Snow(world, L); break;
                case Weather.Rain: Rain(world, L); break;
                case Weather.Seagulls: Seagulls(world, L); break;
            }
        }

        static ParticleSystem Emitter(Transform world, string name, RoadLayout L, float height, float lifetime, int rate, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(world, false);
            go.transform.localPosition = new Vector3(0, height, L.walkHalfZ * 0.3f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;          // oyun açıldığında zaten yağıyor olsun
            main.startLifetime = lifetime;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;

            // Yatay, ince bir kutu; aşağı doğru fırlatır
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.rotation = new Vector3(90f, 0, 0);
            shape.scale = new Vector3(L.walkHalfX * 2f + 8f, L.walkHalfZ * 2f + 14f, 0.1f);
            return ps;
        }

        static void Finish(ParticleSystem ps)
        {
            if (Application.isPlaying) ps.Play();
            else ps.Simulate(8f, true, true); // editör ekran görüntüsü için önceden "yağdır"
        }

        static void Snow(Transform world, RoadLayout L)
        {
            var ps = Emitter(world, "Snow", L, 10f, 9f, 90, 1200);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new Color(1f, 1f, 1f, 0.95f);
            // Rüzgârla sağa sola savrulma
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.2f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mats.Particle(false);
            Finish(ps);
        }

        static void Rain(Transform world, RoadLayout L)
        {
            var ps = Emitter(world, "Rain", L, 12f, 0.9f, 450, 900);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 19f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new Color(0.78f, 0.86f, 1f, 0.5f);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle(false);
            r.renderMode = ParticleSystemRenderMode.Stretch; // damlalar uzun çizgi gibi görünsün
            r.lengthScale = 2f;
            r.velocityScale = 0.035f;
            Finish(ps);
        }

        static void Seagulls(Transform world, RoadLayout L)
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 4; i++)
            {
                var bird = new GameObject("Seagull").transform;
                bird.SetParent(world, false);
                var white = Mats.Lit(Color.white, 0.3f);
                var gray = Mats.Lit(Palette.Hex("#AEB6C2"), 0.3f);
                Shape.Part("Body", bird, MeshFactory.LowPolyBlob(0.12f, 2, 0f, 0.8f), white, Vector3.zero, Quaternion.Euler(0, 0, 0));
                Shape.Box("Beak", bird, new Vector3(0.04f, 0.04f, 0.1f), 0.01f, Mats.Lit(Palette.UiGold, 0.3f), new Vector3(0, 0, 0.14f));
                var wings = new Transform[2];
                for (int s = 0; s < 2; s++)
                {
                    var pivot = new GameObject("Wing").transform;
                    pivot.SetParent(bird, false);
                    float side = s == 0 ? -1f : 1f;
                    Shape.Box("Feathers", pivot, new Vector3(0.42f, 0.02f, 0.16f), 0.01f, white, new Vector3(side * 0.22f, 0, 0));
                    Shape.Box("Tip", pivot, new Vector3(0.12f, 0.022f, 0.12f), 0.01f, gray, new Vector3(side * 0.4f, 0, -0.01f));
                    wings[s] = pivot;
                }
                var b = bird.gameObject.AddComponent<Bird>();
                b.Setup(new Vector3((float)rng.NextDouble() * 10f - 5f, 3.2f + (float)rng.NextDouble() * 1.5f, L.walkHalfZ + 1f + (float)rng.NextDouble() * 3f),
                    3f + (float)rng.NextDouble() * 3f, 0.35f + (float)rng.NextDouble() * 0.25f, (float)rng.NextDouble() * 6f, wings);
            }
        }
    }

    /// <summary>Daire çizerek uçan, kanat çırpan kuş.</summary>
    public class Bird : MonoBehaviour
    {
        Vector3 center;
        float radius, speed, phase;
        Transform[] wings;

        public void Setup(Vector3 c, float r, float s, float p, Transform[] w)
        {
            center = c; radius = r; speed = s; phase = p; wings = w;
            Update();
        }

        void Update()
        {
            float a = (Application.isPlaying ? Time.time : 0f) * speed + phase;
            var pos = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2f) * 0.08f, Mathf.Sin(a)) * radius;
            pos.y = center.y + Mathf.Sin(a * 2f) * 0.3f;
            transform.localPosition = pos;
            transform.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
            float flap = Mathf.Sin((Application.isPlaying ? Time.time : phase) * 9f + phase) * 28f;
            if (wings != null)
            {
                wings[0].localRotation = Quaternion.Euler(0, 0, -flap);
                wings[1].localRotation = Quaternion.Euler(0, 0, flap);
            }
        }
    }
}
