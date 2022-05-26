using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Kadirify.Parkopolis.EditorTools
{
    /// <summary>
    /// Editör araçları. Unity'nin üst menüsünde "Kadirify" altında görünür.
    /// Bu dosya sadece editörde çalışır, oyunun içine girmez ("Editor" klasöründe olduğu için).
    /// </summary>
    [InitializeOnLoad]
    public static class KadirifySetup
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/Game.unity";
        const string AssetsPath = Root + "/Resources/GameAssets.asset";
        const string MatFolder = Root + "/Materials";
        const string ArtFolder = Root + "/Art";

        public const string CompanyName = "Kadirify";
        public const string ProductName = "Parkopolis";
        public const string BundleId = "com.kadirify.parkopolis";

        // Proje açılınca oyun sahnesini otomatik aç ve Play'in her zaman oyunu başlatmasını sağla
        static KadirifySetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) return;
                // Oyun adı değiştiyse proje ayarlarını güncelle
                if (PlayerSettings.productName != ProductName) ConfigurePlayer();
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                if (scene == null) return;
                EditorSceneManager.playModeStartScene = scene;
                if (!SessionState.GetBool("kadirify_opened", false))
                {
                    SessionState.SetBool("kadirify_opened", true);
                    var active = SceneManager.GetActiveScene();
                    if (active.path != ScenePath && !active.isDirty) EditorSceneManager.OpenScene(ScenePath);
                }
            };
        }

        [MenuItem("Kadirify/Projeyi Kur", priority = 1)]
        public static void Setup()
        {
            EnsureFolder(MatFolder);
            EnsureFolder(Root + "/Resources");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(ArtFolder);
            CreateGameAssets();
            ConfigurePlayer();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Kadirify] Kurulum tamam. Play'e basarak oyunu başlatabilirsin.");
        }

        [MenuItem("Kadirify/Ekran Görüntüsü Al", priority = 20)]
        public static void CaptureMenu()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Screenshots"));
            Directory.CreateDirectory(dir);
            var restore = EditorSceneManager.GetActiveScene().path;
            Capture(Mathf.Max(1, PlayerPrefs.GetInt("level", 1)), Path.Combine(dir, "screenshot.png"), 1080, 1920);
            if (!string.IsNullOrEmpty(restore)) EditorSceneManager.OpenScene(restore);
            EditorUtility.RevealInFinder(dir);
        }

        [MenuItem("Kadirify/Kayıtları Sıfırla (Level 1)", priority = 40)]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("[Kadirify] Kayıtlar silindi. Oyun Level 1'den başlayacak.");
        }

        // Test için: bir sonraki Play'de istenen şehirden başla
        [MenuItem("Kadirify/Şehre Git/1. İstanbul (Level 1)", priority = 50)] static void City1() => GoTo(1);
        [MenuItem("Kadirify/Şehre Git/2. Antalya (Level 6)", priority = 51)] static void City2() => GoTo(6);
        [MenuItem("Kadirify/Şehre Git/3. Kapadokya (Level 11)", priority = 52)] static void City3() => GoTo(11);
        [MenuItem("Kadirify/Şehre Git/4. Erzurum (Level 16)", priority = 53)] static void City4() => GoTo(16);
        [MenuItem("Kadirify/Şehre Git/5. Karadeniz (Level 21)", priority = 54)] static void City5() => GoTo(21);
        [MenuItem("Kadirify/Şehre Git/6. İstanbul Gece (Level 26)", priority = 55)] static void City6() => GoTo(26);

        // Test kısayolları: günlük sistemleri beklemeden denemek için
        [MenuItem("Kadirify/Test/Günlük Ödülü Sıfırla", priority = 70)]
        static void TestResetDaily() { Progression.ResetDaily(); Debug.Log("[Kadirify] Günlük ödül tekrar alınabilir."); }

        [MenuItem("Kadirify/Test/Görevleri Yenile", priority = 71)]
        static void TestResetMissions() { Progression.ResetMissions(); Debug.Log("[Kadirify] Görevler sıfırlandı."); }

        [MenuItem("Kadirify/Test/Görevleri Tamamla", priority = 72)]
        static void TestCompleteMissions()
        {
            foreach (MissionType t in System.Enum.GetValues(typeof(MissionType))) Progression.Track(t, 999);
            Debug.Log("[Kadirify] Bugünkü görevler tamamlandı, menüden ödülleri alabilirsin.");
        }

        [MenuItem("Kadirify/Test/Günün Otoparkını Sıfırla", priority = 75)]
        static void TestResetDailyPuzzle() { Progression.ResetDailyPuzzle(); Debug.Log("[Kadirify] Günün Otoparkı tekrar oynanabilir."); }

        [MenuItem("Kadirify/Test/+1000 Coin", priority = 73)]
        static void TestCoins() { SaveData.Coins += 1000; Debug.Log("[Kadirify] +1000 coin."); }

        static void GoTo(int level)
        {
            PlayerPrefs.SetInt("level", level);
            PlayerPrefs.Save();
            Debug.Log($"[Kadirify] Play'e basınca Level {level} ({Theme.For(level).Name}) açılacak.");
        }

        /// <summary>İlk 60 leveli üretip çözülebilir olduklarını kontrol eder.</summary>
        [MenuItem("Kadirify/Levelleri Test Et", priority = 35)]
        public static void VerifyLevels()
        {
            int bad = 0;
            var sb = new System.Text.StringBuilder();
            for (int level = 1; level <= 60; level++)
            {
                var l = LevelGenerator.Create(level);
                int rounds = Solver.Rounds(l);
                if (rounds < 0) bad++;
                sb.Append($"L{level}:{l.width}x{l.height} araba={l.cars.Count} tur={rounds}  ");
                if (level % 5 == 0) sb.AppendLine();
            }
            Debug.Log("[Kadirify] Level testi:\n" + sb + (bad == 0 ? "\nHEPSİ ÇÖZÜLEBİLİR ✔" : $"\nÇÖZÜLEMEYEN LEVEL SAYISI: {bad}"));
        }

        // ---------------- Android derleme ----------------

        /// <summary>Telefona yüklenebilir APK dosyası üretir: D:\game\Builds\Parkopolis.apk</summary>
        [MenuItem("Kadirify/Android APK Oluştur", priority = 60)]
        public static void BuildAndroid()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));
            Directory.CreateDirectory(dir);
            string apk = Path.Combine(dir, ProductName + ".apk");
            ConfigurePlayer();

            EditorUserBuildSettings.buildAppBundle = false; // .aab değil, doğrudan yüklenebilir .apk
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Kadirify] Android derleme: {s.result}, süre {s.totalTime}, boyut {s.totalSize / (1024 * 1024)} MB, hata {s.totalErrors} → {apk}");
            if (Application.isBatchMode && s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
            if (!Application.isBatchMode && s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorUtility.RevealInFinder(apk);
        }

        /// <summary>
        /// Google Play'e yüklenecek paket (.aab). Önce Player Settings → Publishing Settings'te
        /// kendi imza anahtarını (keystore) oluşturup seçmen gerekir.
        /// </summary>
        [MenuItem("Kadirify/Google Play Paketi (AAB) Oluştur", priority = 61)]
        public static void BuildGooglePlay()
        {
            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                EditorUtility.DisplayDialog("İmza anahtarı gerekli",
                    "Google Play paketi için önce kendi imza anahtarını oluştur:\n\nEdit → Project Settings → Player → Android → Publishing Settings → Keystore Manager",
                    "Tamam");
                return;
            }
            ConfigurePlayer();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));
            Directory.CreateDirectory(dir);
            string aab = Path.Combine(dir, $"{ProductName}-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}.aab");
            EditorUserBuildSettings.buildAppBundle = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = aab,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });
            EditorUserBuildSettings.buildAppBundle = false;
            var s = report.summary;
            Debug.Log($"[Kadirify] Google Play paketi: {s.result} → {aab}");
            if (s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorUtility.RevealInFinder(aab);
        }


        [MenuItem("Kadirify/Test/Garajı Sıfırla", priority = 74)]
        public static void ResetGarage()
        {
            foreach (var m in Garage.Models) PlayerPrefs.DeleteKey("g_own_" + m.model);
            foreach (var p in Garage.Paints) PlayerPrefs.DeleteKey("g_paint_" + p.id);
            PlayerPrefs.DeleteKey("g_paint_sel");
            PlayerPrefs.Save();
            Debug.Log("[Kadirify] Garaj sıfırlandı.");
        }

        // ---------------- Kurulum adımları ----------------

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Material SaveMaterial(string name, string shaderName, System.Action<Material> configure)
        {
            string path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(mat, path);
            }
            configure?.Invoke(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        static void CreateGameAssets()
        {
            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, AssetsPath);
            }
            ga.lit = SaveMaterial("M_Lit", "Universal Render Pipeline/Lit", null);
            ga.emissive = SaveMaterial("M_Emissive", "Universal Render Pipeline/Lit", m =>
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            ga.unlit = SaveMaterial("M_Unlit", "Universal Render Pipeline/Unlit", null);
            ga.transparent = SaveMaterial("M_Transparent", "Universal Render Pipeline/Unlit", MakeTransparent);
            ga.particles = SaveMaterial("M_Particles", "Universal Render Pipeline/Particles/Unlit", MakeTransparent);
            EditorUtility.SetDirty(ga);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            // Google Play: 31 Ağustos 2026'dan itibaren yeni uygulamalar en az Android 16 (API 36) hedeflemeli
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.SplashScreen.backgroundColor = Palette.Hex("#0F172A");
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        }

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            cam.AddComponent<UniversalAdditionalCameraData>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 18, -11), Quaternion.Euler(58, 0, 0));
            new GameObject("Game").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        // ---------------- Ekran görüntüsü ----------------

        /// <summary>Oyunu çalıştırmadan bir leveli kurup kameradan resim alır.</summary>
        public static void Capture(int level, string file, int width, int height, System.Action<GameBootstrap> setup = null)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            var boot = new GameObject("Game").AddComponent<GameBootstrap>();
            boot.BuildAll(true, rt);
            boot.Level.Load(level);
            setup?.Invoke(boot);
            Canvas.ForceUpdateCanvases();
            RenderToFile(boot.Cam, rt, file);
            boot.Cam.targetTexture = null;
            rt.Release();
        }

        static void RenderToFile(Camera cam, RenderTexture rt, string file)
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
                RenderPipeline.SubmitRenderRequest(cam, request);
            else
                cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[Kadirify] Kaydedildi: " + file);
        }

        // ---------------- Uygulama ikonu ----------------

        [MenuItem("Kadirify/Uygulama İkonunu Yeniden Üret", priority = 30)]
        public static void CreateIcon()
        {
            EnsureFolder(ArtFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();

            var camGo = new GameObject("IconCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Hex("#4D96FF");
            cam.fieldOfView = 26f;
            cam.targetTexture = rt;
            cam.transform.SetPositionAndRotation(new Vector3(2.6f, 3.0f, -3.2f), Quaternion.Euler(35f, -38f, 0f));
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            var vol = camGo.AddComponent<Volume>();
            vol.isGlobal = true;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            prof.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
            var ca = prof.Add<ColorAdjustments>(true);
            ca.saturation.value = 10f;
            ca.contrast.value = 10f;
            ca.colorFilter.value = Color.white;
            vol.sharedProfile = prof;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = Mats.SkyCubemap();

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(45f, -60f, 0f);
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("#D6E6FF");
            RenderSettings.ambientEquatorColor = Palette.Hex("#E9E1CF");
            RenderSettings.ambientGroundColor = Palette.Hex("#8FA77A");

            // Zemin parçası + kırmızı araba
            var root = new GameObject("IconScene").transform;
            Shape.Box("Pad", root, new Vector3(3.2f, 0.2f, 3.2f), 0.3f, Mats.Lit(Palette.Lot, 0.2f), new Vector3(0, -0.1f, 0), null, false);
            Shape.Paint("Line", root, new Vector3(0.08f, 0.01f, 2.6f), Mats.Lit(Palette.Line, 0.3f), new Vector3(-0.75f, 0.005f, 0));
            Shape.Paint("Line", root, new Vector3(0.08f, 0.01f, 2.6f), Mats.Lit(Palette.Line, 0.3f), new Vector3(0.75f, 0.005f, 0));
            var carGo = new GameObject("Car");
            carGo.transform.SetParent(root, false);
            carGo.transform.rotation = Quaternion.Euler(0, 20, 0);
            CarBuilder.Build(carGo.transform, 2, 1f, Palette.Cars[0], 0);

            string path = ArtFolder + "/AppIcon.png";
            RenderToFile(cam, rt, Path.GetFullPath(path));
            cam.targetTexture = null;
            rt.Release();
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
        }
    }
}
