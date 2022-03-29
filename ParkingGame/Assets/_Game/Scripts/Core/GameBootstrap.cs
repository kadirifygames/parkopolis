using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Oyunun başlangıç noktası. Sahnedeki tek nesne bu.
    /// Play'e basınca kamera, ışık, efektler, arayüz ve level buradan kurulur.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public LevelController Level { get; private set; }
        public GameUI UI { get; private set; }
        public static GameBootstrap Instance { get; private set; }

        Light sun;
        ColorAdjustments colorAdj;
        Vignette vignette;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            BuildAll(false, null);
            Level.Load(SaveData.Level);
            UI.ShowMainMenu(SaveData.Level);  // açılışta ana menü
            UI.ShowIntro();                   // üstünde Kadirify logosu
        }

        /// <summary>preview = editörde ekran görüntüsü almak için (oyun çalışmadan).</summary>
        public void BuildAll(bool preview, RenderTexture target)
        {
            Instance = this;
            BuildCamera(target);
            BuildLighting();
            BuildPostFx();

            if (!preview)
            {
                new GameObject("Sfx").AddComponent<Sfx>();
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            UI = new GameObject("UI").AddComponent<GameUI>();
            UI.Build(Cam, preview);

            Level = gameObject.AddComponent<LevelController>();
            Level.Init(Cam, UI, this);
            UI.OnPause = paused => Level.Paused = paused;
            UI.OnMenu = inMenu => Level.MenuMode = inMenu;
        }

        void BuildCamera(RenderTexture target)
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Palette.Grass;
            Cam.nearClipPlane = 1f;
            Cam.farClipPlane = 200f;
            Cam.targetTexture = target;
            var data = Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
        }

        void BuildLighting()
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
            sun.color = Palette.Hex("#FFEFD6");
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.3f;

            // Ortam ışığı: üstten gök mavisi, yandan sıcak, alttan çim yeşili
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("#C9DEFF");
            RenderSettings.ambientEquatorColor = Palette.Hex("#E9E1CF");
            RenderSettings.ambientGroundColor = Palette.Hex("#8FA77A");
            RenderSettings.fog = false;

            // Parlak yüzeylerin yansıttığı gökyüzü
            var sky = Mats.SkyCubemap();
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = sky;
            RenderSettings.reflectionIntensity = 1f;
            var probe = new GameObject("SkyProbe").AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = sky;
            probe.size = new Vector3(400, 100, 400);
            probe.importance = 0;
        }

        /// <summary>Şehre göre ışık, ortam rengi, yansıma ve renk tonunu ayarlar.</summary>
        public void ApplyTheme(Theme t)
        {
            sun.color = t.SunColor;
            sun.intensity = t.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(t.SunEuler);
            sun.shadowStrength = t.Night ? 0.7f : 0.5f;
            RenderSettings.ambientSkyColor = t.AmbSky;
            RenderSettings.ambientEquatorColor = t.AmbEquator;
            RenderSettings.ambientGroundColor = t.AmbGround;
            var sky = Mats.SkyCubemap(t.SkyZenith, t.SkyHorizon, t.AmbGround);
            RenderSettings.customReflectionTexture = sky;
            var probe = FindAnyObjectByType<ReflectionProbe>();
            if (probe != null) probe.customBakedTexture = sky;
            Cam.backgroundColor = t.Grass;
            colorAdj.colorFilter.value = t.Filter;
            colorAdj.postExposure.value = t.Exposure;
            vignette.intensity.value = t.Night ? 0.32f : 0.2f;
        }

        /// <summary>Uzaktaki nesneleri hafif bulanıklaştıran efekt (minyatür / diorama görünümü).</summary>
        public static DepthOfField Dof { get; private set; }

        void BuildPostFx()
        {
            // Son işlem efektleri: renkleri canlandırır, parlak yerlere hafif ışıma, kenarlara yumuşak kararma
            var go = new GameObject("PostFX");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.Neutral;

            var color = colorAdj = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.12f;
            color.contrast.value = 14f;
            color.saturation.value = 6f;
            color.colorFilter.value = Palette.Hex("#FFF9F2");
            color.hueShift.value = 0f;

            var smh = profile.Add<ShadowsMidtonesHighlights>(false);
            smh.shadows.overrideState = true;
            smh.shadows.value = new Vector4(0.92f, 0.95f, 1.08f, 0f); // gölgelere hafif mavi ton

            var dof = profile.Add<DepthOfField>(true);
            dof.mode.value = DepthOfFieldMode.Gaussian;
            dof.gaussianStart.value = 30f;
            dof.gaussianEnd.value = 45f;
            dof.gaussianMaxRadius.value = 1.2f;
            dof.highQualitySampling.value = false;
            Dof = dof;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.4f;
            bloom.threshold.value = 1.0f;
            bloom.scatter.value = 0.6f;

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.2f;
            vignette.smoothness.value = 0.5f;
            vignette.color.value = Palette.Hex("#1B2338");

            vol.sharedProfile = profile;
        }
    }
}
