using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Parçacık efektleri (Particle System): duman, kıvılcım, konfeti.
    /// Her efekt için küçük bir nesne oluşturur, parçacıkları fırlatır ve iş bitince siler.
    /// </summary>
    public static class Fx
    {
        static ParticleSystem Create(string name, Vector3 pos, bool square, float lifetime)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 0;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle(square);
            r.renderMode = ParticleSystemRenderMode.Billboard;
            Object.Destroy(go, lifetime + 0.6f);
            return ps;
        }

        /// <summary>Çarpma anında küçük toz bulutu ve parlak kıvılcımlar.</summary>
        public static void Impact(Vector3 pos)
        {
            var ps = Create("Impact", pos, false, 0.5f);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f), new Color(1f, 1f, 1f));
            main.gravityModifier = 0.6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            ps.Play();
            ps.Emit(12);
        }

        /// <summary>Araba hareket ederken arkasından çıkan egzoz dumanı.</summary>
        public static void Puff(Vector3 pos, int count = 6)
        {
            var ps = Create("Puff", pos, false, 0.7f);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = new Color(0.95f, 0.95f, 0.95f, 0.75f);
            main.gravityModifier = -0.15f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0.8f, 0), new GradientAlphaKey(0f, 1) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            ps.Play();
            ps.Emit(count);
        }

        /// <summary>Level bitince patlayan renkli konfeti.</summary>
        public static void Confetti(Vector3 pos)
        {
            var ps = Create("Confetti", pos, true, 2.4f);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
            main.gravityModifier = 1.1f;
            var grad = new Gradient();
            grad.SetKeys(new[]
            {
                new GradientColorKey(Palette.Cars[0], 0f), new GradientColorKey(Palette.Cars[2], 0.25f),
                new GradientColorKey(Palette.Cars[3], 0.5f), new GradientColorKey(Palette.Cars[5], 0.75f),
                new GradientColorKey(Palette.Cars[7], 1f)
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.5f;
            shape.rotation = new Vector3(-90, 0, 0);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var vel = ps.limitVelocityOverLifetime;
            vel.enabled = true;
            vel.limit = 7f;
            vel.dampen = 0.15f;
            ps.Play();
            ps.Emit(140);
        }
    }
}
