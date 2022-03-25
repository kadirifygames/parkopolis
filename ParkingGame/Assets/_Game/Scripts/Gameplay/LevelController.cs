using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Oyunun beyni: leveli kurar, dokunmaları okur, hamleleri uygular, kazanmayı kontrol eder.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        public const float Cell = 1f;
        public const float CarY = 0.07f;
        public const int HintCost = 30;
        public const int CraneCost = 80;
        public const int CrashPenalty = 5;   // yolda çarpışma cezası (coin)
        public const int MaxMistakes = 4;    // önü kapalı arabaya dokunma hakkı; fazlası = level baştan
        int mistakes;
        int crashesThisLevel;

        // ---- Günün Otoparkı ----
        bool dailyMode;
        float elapsed;
        int moves;
        readonly List<char> events = new List<char>(); // G = çıktı, R = hata, O = yolda çarpışma

        // ---- Mekanik durumu ----
        float timeLeft;                          // boss süresi
        const float GateInterval = 3.5f;         // kapı kaç saniyede bir renk değiştirir
        int[] gateColors;                        // kapının döndüğü renk numaraları
        int gateIndex;
        float gateTimer;
        readonly List<Renderer> gateLights = new List<Renderer>();
        Renderer gateGlow;

        public int LevelNumber { get; private set; }

        Camera cam;
        GameUI ui;
        GameBootstrap boot;
        Theme theme;
        LevelLayout layout;
        int[,] grid;
        RoadLayout road;
        Transform world;
        readonly List<Car> cars = new List<Car>();
        int parked;
        bool won;

        // Kombo: arka arkaya hızlı çıkışlar
        const float ComboWindow = 1.8f;
        float lastExitTime = -10f;
        int combo;
        int comboBonus;
        static readonly string[] ComboWords = { "", "", "x2", "x3 SÜPER!", "x4 HARİKA!", "x5 MÜKEMMEL!", "EFSANE!" };
        static readonly string[] Reactions = { "BİİP!", "HEY!", "#@!", "DİKKAT!", "OFF!" };
        bool craneMode;
        HintArrow hintArrow;
        Car hintCar;

        // dokunma durumu
        Car downCar;
        Vector2 downPos;
        bool dragging;

        public bool Paused { get; set; }

        // Menü modu: dokunmalar oyuna gitmez, kamera arkada yavaşça sallanır
        bool menuMode;
        Vector3 camBasePos;
        Quaternion camBaseRot;

        public bool MenuMode
        {
            get => menuMode;
            set
            {
                menuMode = value;
                if (!value && cam != null) cam.transform.SetPositionAndRotation(camBasePos, camBaseRot);
            }
        }

        void SwayCamera()
        {
            float a = Mathf.Sin(Time.unscaledTime * 0.25f) * 7f;
            Vector3 pivot = road != null ? road.framing.center : Vector3.zero;
            var rot = Quaternion.Euler(0, a, 0);
            cam.transform.SetPositionAndRotation(pivot + rot * (camBasePos - pivot), rot * camBaseRot);
        }

        public void Init(Camera camera, GameUI gameUi, GameBootstrap bootstrap)
        {
            cam = camera;
            ui = gameUi;
            boot = bootstrap;
            ui.OnHint = UseHint;
            ui.OnCrane = ToggleCrane;
            ui.OnRestart = () => { if (dailyMode) LoadDaily(); else Load(LevelNumber); };
            ui.OnNext = () => Load(LevelNumber + 1);
            ui.OnRetry = () => { if (dailyMode) LoadDaily(); else Load(LevelNumber); };
            ui.OnDailyPuzzle = () => { LoadDaily(); ui.HideMenus(); };
            ui.OnDailyExit = () => { Load(SaveData.Level); ui.ShowMainMenu(SaveData.Level); };
            ui.OnGarageChanged = () => Load(LevelNumber); // yeni boya/araba hemen görünsün
            Progression.OnCompleted = def => ui.ShowMissionDone(def); // "=" ile: her Play'de tek abone
        }

        // ---------------- Level kurulumu ----------------

        public void Load(int level) => LoadInternal(level, 0, false);

        /// <summary>
        /// Günün Otoparkı: tarih tohum olarak kullanılır, herkes aynı otoparkı oynar.
        /// 2. turdaki normal zorlukta bir levelden (o günün şehrinde) üretilir.
        /// </summary>
        public void LoadDaily()
        {
            int seed = Progression.DailyPuzzleSeed;
            int region = seed % Theme.All.Length;
            int pseudoLevel = Theme.LevelsPerRegion * Theme.All.Length + region * Theme.LevelsPerRegion + 2;
            LoadInternal(pseudoLevel, seed, true);
        }

        void LoadInternal(int level, int salt, bool daily)
        {
            StopAllCoroutines();
            dailyMode = daily;
            elapsed = 0f;
            moves = 0;
            events.Clear();
            Clear();
            LevelNumber = level;
            won = false;
            Time.timeScale = 1f;
            mistakes = 0;
            crashesThisLevel = 0;
            combo = 0;
            comboBonus = 0;
            lastExitTime = -10f;
            craneMode = false;
            layout = LevelGenerator.Create(level, salt);
            grid = Grid.Build(layout);

            theme = Theme.For(level);
            world = new GameObject("World").transform;
            road = EnvironmentBuilder.Build(world, layout.width, layout.height, Cell, level * 31 + 7, theme);
            if (boot != null) boot.ApplyTheme(theme);
            WeatherFx.Create(world, theme, road);

            foreach (var o in layout.obstacles)
                CarBuilder.TrafficCone(world, CellToWorld(o.x, o.y));
            BuildIce();
            SetupGates();
            timeLeft = layout.timeLimit;

            for (int i = 0; i < layout.cars.Count; i++)
            {
                var spec = layout.cars[i];
                var go = new GameObject($"Car_{i}");
                go.transform.SetParent(world, false);
                go.transform.position = CarCenter(spec);
                Vector3 facing = (spec.horizontal ? Vector3.right : Vector3.forward) * spec.ForwardSign;
                go.transform.rotation = Quaternion.LookRotation(facing);
                var car = go.AddComponent<Car>();
                car.Init(i, spec, Cell, level);
                cars.Add(car);
            }
            parked = cars.Count;

            CameraRig.Fit(cam, road.framing);
            camBasePos = cam.transform.position;
            camBaseRot = cam.transform.rotation;
            ui.SetLevel(level);
            ui.SetRegion(theme.Name, Theme.RegionIndex(level), theme.Night);
            ui.SetDailyMode(daily, level);
            ui.SetDifficulty(layout.difficulty);
            ui.SetCoins(SaveData.Coins);
            ui.SetCraneMode(false);
            ui.HideWin();
            ui.HideFail();
            ui.SetHearts(MaxMistakes, false);
            ui.SetTimer(layout.boss, timeLeft);
            RefreshGateUi();

            if (level == 1)
            {
                ShowHint();
                ui.ShowToast("Arabaya dokun veya kaydır!\nÖnü açık olan araba çıkar.", 0f);
            }
            else if (!daily && Theme.IsRegionStart(level))
            {
                ui.ShowRegionBanner(theme.Name);
                ui.HideToast();
            }
            else if (layout.boss)
            {
                ui.ShowRegionBanner("BOSS LEVEL!", "3 KAT ÖDÜL • SÜRELİ");
                ui.HideToast();
            }
            else
            {
                ui.HideToast();
            }
            if (Application.isPlaying) StartCoroutine(IntroDrop());
            ShowMechanicTip();
        }

        /// <summary>Level açılışında arabalar yukarıdan tek tek "pıt pıt" düşer.</summary>
        IEnumerator IntroDrop()
        {
            var targets = new List<Vector3>();
            foreach (var c in cars)
            {
                targets.Add(c.transform.position);
                c.transform.position += Vector3.up * 6f;
            }
            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                var target = targets[i];
                var from = c.transform.position;
                StartCoroutine(Tween.Run(0.35f, k =>
                {
                    if (c != null) c.transform.position = Vector3.LerpUnclamped(from, target, k);
                }, Ease.OutBack));
                yield return new WaitForSeconds(0.04f);
            }
        }

        void Clear()
        {
            cars.Clear();
            hintArrow = null;
            hintCar = null;
            downCar = null;
            if (world == null) return;
            if (Application.isPlaying) Destroy(world.gameObject);
            else DestroyImmediate(world.gameObject);
            world = null;
        }

        Vector3 CellToWorld(float x, float y) =>
            new Vector3((x - (layout.width - 1) / 2f) * Cell, CarY, (y - (layout.height - 1) / 2f) * Cell);

        Vector3 CarCenter(CarSpec s) => s.horizontal
            ? CellToWorld(s.x + (s.length - 1) / 2f, s.y)
            : CellToWorld(s.x, s.y + (s.length - 1) / 2f);

        // ---------------- Dokunma ----------------

        void Update()
        {
            if (menuMode && cam != null) { SwayCamera(); return; }
            if (Paused || won || cam == null) return;
            CheckRoadCrashes();
            TickMechanics();
            var pointer = Pointer.current;
            if (pointer == null) return;
            Vector2 pos = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                downCar = null;
                if (IsOverUI(pos)) return;
                downCar = PickCar(pos);
                downPos = pos;
                dragging = downCar != null;
            }
            else if (dragging && pointer.press.isPressed && !craneMode)
            {
                // Yeterince kaydırınca parmağı kaldırmayı beklemeden hareket ettir
                if ((pos - downPos).magnitude > DragThreshold)
                {
                    Act(downCar, downPos, pos);
                    dragging = false;
                    downCar = null;
                }
            }
            else if (pointer.press.wasReleasedThisFrame)
            {
                if (dragging && downCar != null) Act(downCar, downPos, pos);
                dragging = false;
                downCar = null;
            }
        }

        static float DragThreshold => Mathf.Max(18f, Screen.height * 0.025f);

        bool IsOverUI(Vector2 pos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = pos };
            var hits = new List<RaycastResult>();
            es.RaycastAll(data, hits);
            return hits.Count > 0;
        }

        Car PickCar(Vector2 screenPos)
        {
            var ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 300f))
            {
                var car = hit.collider.GetComponentInParent<Car>();
                if (car != null && !car.Gone) return car;
            }
            return null;
        }

        void Act(Car car, Vector2 from, Vector2 to)
        {
            if (car == null || car.Gone) return;
            if (craneMode)
            {
                Crane(car);
                return;
            }
            // Arabalar sadece ileri (burnunun baktığı yöne) gider: dokunma da kaydırma da aynı sonucu verir
            Move(car, car.Spec.ForwardSign);
        }

        Vector3 GroundPoint(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0, CarY, 0));
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.zero;
        }

        // ---------------- Hamleler ----------------

        void Move(Car car, int sign)
        {
            if (car.Busy || car.Gone) return;
            ClearHint();
            moves++;
            var probe = Grid.Look(grid, car.Spec, sign);

            if (probe.exits)
            {
                Grid.Mark(grid, car.Spec, Grid.Empty);
                parked--;
                Vector3 p = car.transform.position;
                Vector3 lane = car.Spec.horizontal
                    ? new Vector3(sign * road.laneX, CarY, p.z)
                    : new Vector3(p.x, CarY, sign * road.laneZ);
                if (layout.colorGates)
                    car.ExitGated(lane, RoadPath.BuildGated(lane, road, CarY), CanPassGate, road.barrier, road.walkHalfZ + 0.9f);
                else
                    car.Exit(lane, RoadPath.Build(lane, road, CarY), road.barrier, road.walkHalfZ + 0.9f);
                events.Add('G');
                OnCarExited(p);
                AfterCarLeft();
            }
            else
            {
                Car hit = probe.blocker >= 0 ? cars[probe.blocker] : null;
                if (TrySlideOnIce(car, sign, probe, hit)) return;
                float travel = probe.steps * Cell + 0.1f;
                car.Bump(sign, travel, hit);
                combo = 0;
                RegisterMistake();
                // Sürücü tepkisi: çarpılan arabanın üstünde konuşma balonu
                Vector3 at = hit != null ? hit.transform.position : car.transform.position + car.Axis * sign * (travel + 1f);
                string word = Reactions[Random.Range(0, Reactions.Length)];
                StartCoroutine(Tween.Delay(0.08f + travel / 16f, () => ui.Popup(at + Vector3.up * 1.1f, word, Palette.UiDark, 40, true)));
                if (LevelNumber == 1) StartCoroutine(Tween.Delay(0.6f, () => { if (!won) ShowHint(); }));
            }
        }

        // ---------------- Mekanikler ----------------

        void TickMechanics()
        {
            elapsed += Time.deltaTime;
            if (layout.boss && timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;
                ui.SetTimer(true, timeLeft);
                if (timeLeft <= 0f) StartCoroutine(FailRoutine(true));
            }
            if (layout.colorGates)
            {
                gateTimer += Time.deltaTime;
                if (gateTimer >= GateInterval)
                {
                    gateTimer = 0f;
                    gateIndex = (gateIndex + 1) % gateColors.Length;
                    PaintGate();
                    Sfx.Play(Sfx.Sound.Pop, 0.4f, 0.7f);
                }
                RefreshGateUi();
            }
        }

        // ---- Buzlu zemin ----

        void BuildIce()
        {
            if (layout.ice.Count == 0) return;
            var ice = Mats.Lit(Palette.Hex("#CDEBFF"), 0.97f, 0.25f);
            var crack = Mats.Lit(Color.white, 0.8f);
            foreach (var c in layout.ice)
            {
                Vector3 p = CellToWorld(c.x, c.y);
                p.y = CarY + 0.006f;
                Shape.Box("Ice", world, new Vector3(0.94f, 0.012f, 0.94f), 0.08f, ice, p, null, false);
                float yaw = (c.x * 37 + c.y * 61) % 180;
                var cr = Shape.Paint("Crack", world, new Vector3(0.42f, 0.004f, 0.025f), crack, p + new Vector3(0.1f, 0.008f, -0.05f));
                cr.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            }
        }

        bool OnIce(CarSpec s)
        {
            for (int i = 0; i < s.length; i++) if (layout.IsIce(s.Cell(i))) return true;
            return false;
        }

        /// <summary>
        /// Buzdaki araba önü kapalıysa engele kadar kayar ve orada kalır (kalp gitmez).
        /// Kayma levelin çözümünü imkânsız yapacaksa izin verilmez: araba normal şekilde geri seker.
        /// </summary>
        bool TrySlideOnIce(Car car, int sign, Grid.Probe probe, Car hit)
        {
            if (probe.steps <= 0 || !OnIce(car.Spec)) return false;
            var moved = car.Spec;
            if (moved.horizontal) moved.x += sign * probe.steps; else moved.y += sign * probe.steps;

            // Kaymadan sonra level hâlâ çözülebilir mi?
            var test = new LevelLayout { width = layout.width, height = layout.height, obstacles = layout.obstacles };
            foreach (var c in cars)
                if (c != null && !c.Gone) test.cars.Add(c == car ? moved : c.Spec);
            if (Solver.Rounds(test) < 0) return false;

            Grid.Mark(grid, car.Spec, Grid.Empty);
            Grid.Mark(grid, moved, car.Id);
            car.Slide(moved, CarCenter(moved), hit);
            combo = 0;
            ui.Popup(car.transform.position + Vector3.up * 1.2f, "KAYDI!", Palette.Hex("#3A9BFF"), 42);
            return true;
        }

        // ---- Renkli kapı ----

        void SetupGates()
        {
            gateLights.Clear();
            gateGlow = null;
            if (!layout.colorGates) return;

            // Levelde gerçekten bulunan renkler (en çok 3)
            var present = new List<int>();
            foreach (var s in layout.cars)
                if (!present.Contains(s.colorIndex)) present.Add(s.colorIndex);
            gateColors = present.ToArray();
            gateIndex = 0;
            gateTimer = 0f;

            // Çıkış yolunun üstünde renkli kemer
            float x = road.exitX, z = road.walkHalfZ + 0.25f, half = road.exitHalf + 0.2f;
            var metal = Mats.Lit(Palette.Metal, 0.5f, 0.3f);
            foreach (float s in new[] { -1f, 1f })
            {
                Shape.Box("ArchPost", world, new Vector3(0.18f, 1.9f, 0.18f), 0.04f, metal, new Vector3(x + s * half, 0.13f + 0.95f, z));
                var lamp = Shape.Part("ArchLamp", world, MeshFactory.LowPolyBlob(0.17f, 3, 0f, 1f), metal, new Vector3(x + s * half, 2.15f, z));
                gateLights.Add(lamp.GetComponent<Renderer>());
            }
            var beam = Shape.Box("ArchBeam", world, new Vector3(half * 2f + 0.3f, 0.3f, 0.2f), 0.08f, metal, new Vector3(x, 1.95f, z));
            gateLights.Add(beam.GetComponent<Renderer>());
            gateGlow = Shape.Part("GateGlow", world, MeshFactory.Quad(half * 2f + 1f, 2.4f), Mats.Glow(Color.white, 0.5f),
                new Vector3(x, 0.075f, z - 0.3f), null, false).GetComponent<Renderer>();
            PaintGate();
        }

        Color GateColor(int i) => Garage.CarColor(gateColors[i % gateColors.Length]);

        void PaintGate()
        {
            if (gateColors == null || gateColors.Length == 0) return;
            var c = GateColor(gateIndex);
            foreach (var r in gateLights) if (r != null) r.sharedMaterial = Mats.Emissive(c, 2.2f);
            if (gateGlow != null) gateGlow.sharedMaterial = Mats.Glow(c, 0.55f);
        }

        void RefreshGateUi()
        {
            if (!layout.colorGates || gateColors == null || gateColors.Length == 0) { ui.SetGate(false, Color.white, Color.white, 0); return; }
            ui.SetGate(true, GateColor(gateIndex), GateColor(gateIndex + 1), gateTimer / GateInterval);
        }

        /// <summary>Araba kapıdan geçebilir mi? Rengi kapının şu anki rengiyle aynıysa.</summary>
        bool CanPassGate(Car car) => gateColors == null || gateColors.Length == 0 || car.Spec.colorIndex == gateColors[gateIndex];

        // ---- İlk kez görülen mekanik için kısa açıklama ----

        void ShowMechanicTip()
        {
            if (!Application.isPlaying) return;
            string key = null, text = null;
            if (layout.ice.Count > 0) { key = "tip_ice"; text = "BUZLU ZEMİN!\nBuzdaki araba engele kadar kayar ve orada kalır."; }
            else if (layout.colorGates) { key = "tip_gate"; text = "RENKLİ KAPI!\nSadece kapının rengindeki araba çıkar,\ndiğerleri bir tur daha atar."; }
            else if (layout.boss) { key = "tip_boss"; text = "BOSS LEVEL!\nSüre bitmeden bitir, 3 kat ödül kazan."; }
            if (key == null || PlayerPrefs.GetInt(key, 0) == 1) return;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            ui.ShowToast(text, 4f);
        }

        // ---------------- Hak sistemi ----------------

        /// <summary>Önü kapalı arabaya dokunmak bir hak götürür. 4 hak bittikten sonraki hata = level baştan.</summary>
        void RegisterMistake()
        {
            mistakes++;
            events.Add('R');
            ui.SetHearts(Mathf.Max(0, MaxMistakes - mistakes), true);
            if (mistakes > MaxMistakes)
            {
                StartCoroutine(FailRoutine());
            }
            else if (mistakes == MaxMistakes)
            {
                ui.ShowToast("Dikkat! Son hakkın.\nBir hata daha yaparsan level baştan başlar.", 2.5f);
            }
        }

        IEnumerator FailRoutine(bool timeout = false)
        {
            won = true; // girişi kilitle
            ClearHint();
            craneMode = false;
            ui.SetCraneMode(false);
            ui.HideToast();
            yield return new WaitForSeconds(0.7f);
            Sfx.Play(Sfx.Sound.Bump, 1f, 0.6f);
            if (timeout) ui.ShowFail("SÜRE DOLDU!", "Boss leveli süre bitmeden tamamla.\nTekrar dene!");
            else ui.ShowFail();
        }

        // ---------------- Yolda çarpışma ----------------

        readonly Vector3[] ptsA = new Vector3[3], ptsB = new Vector3[3];

        /// <summary>
        /// Yoldaki her araba çiftine bakar: ön/orta/arka noktalarından herhangi ikisi çok yakınsa çarpışma.
        /// </summary>
        void CheckRoadCrashes()
        {
            for (int i = 0; i < cars.Count; i++)
            {
                var a = cars[i];
                if (a == null || !a.CanCrash) continue;
                for (int j = i + 1; j < cars.Count; j++)
                {
                    var b = cars[j];
                    if (b == null || !b.CanCrash) continue;
                    if ((a.transform.position - b.transform.position).sqrMagnitude > 9f) continue; // uzaklar, hızlı geç

                    a.Points(ptsA);
                    b.Points(ptsB);
                    float best = float.MaxValue;
                    Vector3 hitA = default, hitB = default;
                    foreach (var pa in ptsA)
                    foreach (var pb in ptsB)
                    {
                        float dd = (pa - pb).sqrMagnitude;
                        if (dd < best) { best = dd; hitA = pa; hitB = pb; }
                    }
                    if (best > 0.62f * 0.62f) continue;

                    // b, a'nın önündeyse a arkadan çarpmıştır
                    bool aIsRear = Vector3.Dot(b.transform.position - a.transform.position, a.transform.forward) > 0f;
                    a.Crash((a.transform.position - b.transform.position).normalized, b, aIsRear);
                    b.Crash((b.transform.position - a.transform.position).normalized, a, !aIsRear);
                    OnRoadCrash((hitA + hitB) * 0.5f);
                }
            }
        }

        void OnRoadCrash(Vector3 at)
        {
            combo = 0;
            crashesThisLevel++;
            events.Add('O');
            SaveData.Coins -= CrashPenalty;
            ui.BumpCoins(SaveData.Coins);
            Sfx.Play(Sfx.Sound.Bump, 1f);
            Sfx.Play(Sfx.Sound.Horn, 0.6f);
            Fx.Impact(at + Vector3.up * 0.3f);
            ui.Popup(at + Vector3.up * 1.4f, "ÇARPIŞMA!", Palette.Hex("#FF4D4D"), 50);
            StartCoroutine(Tween.Delay(0.18f, () => ui.Popup(at + Vector3.up * 0.7f, $"-{CrashPenalty}", Palette.Hex("#FF4D4D"), 46)));

            // İlk çarpışmada oyuncuya kuralı öğret
            if (PlayerPrefs.GetInt("crashTip", 0) == 0)
            {
                PlayerPrefs.SetInt("crashTip", 1);
                ui.ShowToast($"Yolda çarpışma: -{CrashPenalty} coin!\nHızlı arabayı yavaşın arkasından salma.", 3.5f);
            }
        }

        /// <summary>Her çıkışta +1 coin; hızlı art arda çıkışlarda kombo yazısı, bonus ve tizleşen ses.</summary>
        void OnCarExited(Vector3 pos)
        {
            combo = Time.time - lastExitTime <= ComboWindow ? combo + 1 : 1;
            lastExitTime = Time.time;

            SaveData.Coins += 1;
            ui.BumpCoins(SaveData.Coins);
            Progression.Track(MissionType.Cars);
            if (combo == 3) Progression.Track(MissionType.Combo);
            ui.Popup(pos + Vector3.up * 0.9f, "+1", Palette.UiGold, 46);

            if (combo >= 2)
            {
                comboBonus += combo - 1;
                string text = ComboWords[Mathf.Min(combo, ComboWords.Length - 1)];
                Color c = combo >= 4 ? Palette.Hex("#FF5A5F") : combo == 3 ? Palette.UiGoldDark : Palette.UiBlue;
                StartCoroutine(Tween.Delay(0.12f, () => ui.Popup(pos + Vector3.up * 1.8f, text, c, 54 + combo * 4)));
                Sfx.Play(Sfx.Sound.Coin, 0.5f, 1f + (combo - 1) * 0.12f);
            }
        }

        void AfterCarLeft()
        {
            if (parked <= 0)
            {
                StartCoroutine(WinRoutine());
                return;
            }
            if (LevelNumber == 1) StartCoroutine(Tween.Delay(0.5f, () => { if (!won) ShowHint(); }));
        }

        IEnumerator WinRoutine()
        {
            won = true;
            ui.HideToast();
            craneMode = false;
            ui.SetCraneMode(false);

            // Final: ağır çekim + kamera hafifçe yaklaşır
            float baseFov = cam.fieldOfView;
            Time.timeScale = 0.3f;
            yield return Tween.Run(0.25f, k => cam.fieldOfView = Mathf.Lerp(baseFov, baseFov - 3f, k), Ease.OutQuad, true);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Tween.Run(0.4f, k =>
            {
                Time.timeScale = Mathf.Lerp(0.3f, 1f, k);
                cam.fieldOfView = Mathf.Lerp(baseFov - 3f, baseFov, k);
            }, Ease.InOutQuad, true);
            Time.timeScale = 1f;

            yield return new WaitForSeconds(0.35f);
            if (dailyMode) { FinishDaily(); yield break; }
            int reward = 20 + LevelNumber * 2 + comboBonus;
            if (layout.boss) reward *= 3;
            else if (layout.difficulty == Difficulty.Hard) reward = Mathf.RoundToInt(reward * 1.5f); // ZOR level bonusu
            SaveData.Level = LevelNumber + 1;

            // Görev takibi
            Progression.Track(MissionType.Levels);
            if (crashesThisLevel == 0) Progression.Track(MissionType.NoCrash);
            if (mistakes == 0) Progression.Track(MissionType.Perfect);

            // Sandık ilerlemesi: her 3 levelde bir sandık
            int chest = SaveData.ChestProgress + 1;
            bool chestReady = chest >= SaveData.ChestEvery;
            SaveData.ChestProgress = chestReady ? 0 : chest;
            ui.SetChestProgress(chestReady ? SaveData.ChestEvery : chest, chestReady);
            SaveData.Coins += reward;
            Sfx.Play(Sfx.Sound.Win);
            Fx.Confetti(new Vector3(0, 0.5f, 0));
            string next = Theme.IsRegionStart(LevelNumber + 1) ? Theme.For(LevelNumber + 1).Name : null;
            ui.ShowWin(LevelNumber, reward, SaveData.Coins, next);
        }

        /// <summary>Günün Otoparkı bitti: sonucu kaydet, paylaşılacak metni (emoji karelerle) hazırla.</summary>
        void FinishDaily()
        {
            bool first = Progression.CompleteDailyPuzzle("");
            int streak = Progression.DailyPuzzleStreak;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Parkopolis • Günün Otoparkı {System.DateTime.Now:dd.MM.yyyy}");
            int n = Mathf.Min(events.Count, 30);
            for (int i = 0; i < n; i++)
            {
                sb.Append(events[i] == 'G' ? "🟩" : events[i] == 'R' ? "🟥" : "🟧");
                if ((i + 1) % 10 == 0 && i + 1 < n) sb.AppendLine();
            }
            sb.AppendLine();
            int s = Mathf.RoundToInt(elapsed);
            sb.AppendLine($"⏱ {s / 60}:{s % 60:00} • {moves} hamle • {mistakes} hata");
            sb.AppendLine($"🔥 Seri: {streak} gün");
            sb.Append("#Parkopolis");

            Progression.Track(MissionType.Levels);
            Sfx.Play(Sfx.Sound.Win);
            Fx.Confetti(new Vector3(0, 0.5f, 0));
            ui.SetCoins(SaveData.Coins);
            ui.ShowDailyResult(elapsed, moves, mistakes, events, first, streak, sb.ToString());
        }

        // ---------------- Güçlendiriciler ----------------

        Car FindExitableCar()
        {
            foreach (var c in cars)
            {
                if (c == null || c.Gone || c.Busy) continue;
                if (Grid.CanExit(grid, c.Spec)) return c;
            }
            return null;
        }

        void ShowHint()
        {
            ClearHint();
            var car = FindExitableCar();
            if (car == null) return;
            int sign = car.Spec.ForwardSign;
            hintCar = car;
            car.SetPulse(true);
            hintArrow = HintArrow.Create(world, car.transform.position, car.Axis * sign);
        }

        void ClearHint()
        {
            if (hintCar != null) hintCar.SetPulse(false);
            hintCar = null;
            if (hintArrow != null)
            {
                if (Application.isPlaying) Destroy(hintArrow.gameObject);
                else DestroyImmediate(hintArrow.gameObject);
            }
            hintArrow = null;
        }

        void UseHint()
        {
            if (won) return;
            if (hintArrow != null) return;
            if (LevelNumber > 1)
            {
                if (SaveData.Coins < HintCost) { ui.ShowToast("Yeterli coin yok!", 1.5f); return; }
                SaveData.Coins -= HintCost;
                ui.SetCoins(SaveData.Coins);
            }
            ShowHint();
        }

        void ToggleCrane()
        {
            if (dailyMode) { ui.ShowToast("Günün Otoparkında vinç kullanılamaz!", 1.6f); return; }
            if (won) return;
            if (craneMode)
            {
                craneMode = false;
                ui.SetCraneMode(false);
                ui.HideToast();
                return;
            }
            if (SaveData.Coins < CraneCost) { ui.ShowToast("Yeterli coin yok!", 1.5f); return; }
            craneMode = true;
            ui.SetCraneMode(true);
            ui.ShowToast("Kaldırmak istediğin arabaya dokun", 0f);
        }

        void Crane(Car car)
        {
            craneMode = false;
            ui.SetCraneMode(false);
            ui.HideToast();
            if (car.Busy) return;
            ClearHint();
            SaveData.Coins -= CraneCost;
            ui.SetCoins(SaveData.Coins);
            Grid.Mark(grid, car.Spec, Grid.Empty);
            parked--;
            car.LiftAway();
            AfterCarLeft();
        }
    }
}
