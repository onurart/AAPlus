using System;
using System.Collections.Generic;

namespace AAPlus.Services;

// ═══════════════════════════════════════════════════════════════════
//  MATEMATİKSEL MODEL
// ═══════════════════════════════════════════════════════════════════
//
//  KOORDİNAT SİSTEMİ:
//    x = cx + r × sin(θ),  y = cy - r × cos(θ)
//    θ = 0 → yukarı,  θ = π → aşağı
//
//  AÇISAL HAREKET:
//    rotationAngle = Normalize(rotationAngle + omega * deltaTime)
//    omega = baseOmega * (1 + 0.4f * MathF.Sin(2 * MathF.PI * frequency * elapsedTime))
//
//  YENİ MEKANİKLER:
//    1. KALKAN HALKASI — Dairenin etrafında dönen koruyucu halka, boşluktan geçmeli
//    2. HAREKETLİ İĞNELER — Pre-pin'ler salınım yapar
//    3. BOSS SEVİYELERİ — Her 50 seviyede özel zorluk
//
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// "aa" tarzı iğne saplama oyun motoru — 250 seviye, 5 kademe, 3 özel mekanik.
/// </summary>
public class PinGameEngine
{
    // ╔═══════════════════════════════════════════════════════════╗
    // ║  SABİTLER                                                ║
    // ╚═══════════════════════════════════════════════════════════╝

    private const float TWO_PI = MathF.PI * 2f;
    public const float CircleRadius = 50f;
    public const float PinLength = 70f;
    public const float PinHeadRadius = 8f;
    private const float PIN_FLY_SPEED = 1800f;
    public const int MaxLevel = 250;

    // Çarpışma eşiği
    private const float BASE_COLLISION_THRESHOLD = 0.22f;
    private const float MIN_COLLISION_THRESHOLD = 0.14f;
    private const float THRESHOLD_DECAY = 0.0003f;
    private float _collisionThreshold;

    // Kalkan halkası sabitleri
    public const float ShieldRadiusOffset = 35f; // Daireden uzaklık (px)

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  DURUM DEĞİŞKENLERİ                                     ║
    // ╚═══════════════════════════════════════════════════════════╝

    // ─── Seviye ──────────────────────────────────────────────
    public int CurrentLevel { get; private set; } = 1;
    public int PinsToPlace { get; internal set; }
    public int PinsRemaining { get; internal set; }

    // ─── Açısal Hareket ──────────────────────────────────────
    public float RotationAngle { get; private set; }
    private float _omegaBase;
    private float _omegaCurrent;

    // ─── Salınım ─────────────────────────────────────────────
    private bool _oscillationEnabled;
    private float _oscillationTime;
    private float _oscillationFreqHz;

    // ─── Yön Değişimi ────────────────────────────────────────
    private bool _directionReversalEnabled;
    private float _reversalTimer;
    private float _reversalIntervalSec;

    // ─── İğneler ─────────────────────────────────────────────
    public List<float> PlacedPins { get; } = new();
    public List<float> PrePlacedPins { get; } = new();

    // ─── Fırlatılan İğne ─────────────────────────────────────
    public bool IsPinFlying { get; private set; }
    public float FlyingPinY { get; private set; }

    // ─── Durum ───────────────────────────────────────────────
    public GameState State { get; private set; } = GameState.Ready;
    public bool IsPaused { get; private set; }
    public int Score { get; set; }
    public int HighScore { get; set; }

    // ─── Aktif Level Config ─────────────────────────────────
    public LevelConfig? ActiveConfig { get; private set; }
    public RotationBehavior CurrentBehavior { get; private set; }

    // ─── Behavior state ──────────────────────────────────────
    private float _accelFactor = 1f;   // Accelerating/Decelerating
    private float _fakeReverseTimer;   // FakeReverse
    private bool _fakeReversed;

    // ─── Kalkan Halkası ──────────────────────────────────────

    public bool HasShield { get; private set; }
    public float ShieldAngle { get; private set; }    // Halka dönüş açısı
    public float ShieldGapAngle { get; private set; } // Boşluk genişliği (rad)
    private float _shieldSpeed;                        // Halka dönüş hızı
    private bool _shieldPassed;                        // İğne halkayı geçti mi

    // ═══════════════════════════════════════════════════════════
    //  YENİ MEKANİK 2: HAREKETLİ İĞNELER
    // ═══════════════════════════════════════════════════════════
    //
    //  Pre-pin'ler sabit durmaz, açısal salınım yapar.
    //  θ_prepin(t) = θ_base + A × sin(2π × f_move × t)
    //
    //  Bu, çarpışma tahminini zorlaştırır.
    //

    public bool HasMovingPins { get; private set; }
    private float _prePinOscRange;     // Salınım genliği (rad)
    private float _prePinTimer;
    private List<float> _prePinBaseAngles = new(); // Orijinal açılar

    // ═══════════════════════════════════════════════════════════
    //  YENİ MEKANİK 3: BOSS SEVİYELERİ
    // ═══════════════════════════════════════════════════════════
    //
    //  Her 50 seviyede (50, 100, 150, 200, 250) boss seviye.
    //  Tüm mekanikler aktif + ekstra zorluk.
    //

    public bool IsBossLevel { get; private set; }
    public static bool IsBoss(int level) => level % 50 == 0 && level > 0;

    // ═══════════════════════════════════════════════════════════
    //  OYUN MODLARI — Her seviye farklı oynama şekli
    // ═══════════════════════════════════════════════════════════
    //
    //  Normal:     Standart tek iğne fırlatma
    //  SpeedBurst: Daire ani hızlanıp yavaşlar (3sn döngü)
    //  DoublePin:  2 iğne aynı anda simetrik fırlatılır
    //  Invisible:  Saplanmış iğneler 2sn sonra görünmez olur
    //  Shrinking:  Daire periyodik olarak küçülüp büyür
    //

    public GameMode CurrentMode { get; private set; } = GameMode.Normal;

    // SpeedBurst state
    private float _burstTimer;
    public bool IsBurstActive { get; private set; }
    private const float BURST_CYCLE = 3f;     // 3 sn döngü
    private const float BURST_DURATION = 0.8f; // 0.8 sn hızlı
    private const float BURST_MULTIPLIER = 2.5f;

    // DoublePin state
    public bool IsDoublePin => CurrentMode == GameMode.DoublePin;
    public bool HasSecondPin { get; private set; } // ikinci pin de saplanacak mı

    // Invisible state
    public List<float> PinPlaceTimes { get; } = new(); // her pinin yerleşme zamanı
    private float _gameTimer;
    public float GameTimer => _gameTimer;
    public const float INVISIBLE_FADE_TIME = 2f;

    // Shrinking state
    public float CircleScale { get; private set; } = 1f;
    private const float SHRINK_CYCLE = 4f;  // 4 sn periyot
    private const float SHRINK_MIN = 0.7f;  // minimum %70

    private readonly Random _rng = new();

    // ─── Event'ler ───────────────────────────────────────────
    public event Action? OnPinPlaced;
    public event Action? OnCollision;
    public event Action? OnLevelCleared;
    public event Action? OnShieldHit;

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  TRİGONOMETRİK YARDIMCI                                  ║
    // ╚═══════════════════════════════════════════════════════════╝

    public static float Normalize(float angle)
    {
        angle %= TWO_PI;
        if (angle < 0f) angle += TWO_PI;
        return angle;
    }

    public static float AngleDistance(float a, float b)
    {
        float delta = MathF.Abs(Normalize(a) - Normalize(b));
        return MathF.Min(delta, TWO_PI - delta);
    }

    public static (float x, float y) PolarToScreen(float cx, float cy, float radius, float angle)
    {
        return (cx + radius * MathF.Sin(angle), cy - radius * MathF.Cos(angle));
    }

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  ZORLUK SİSTEMİ                                          ║
    // ╚═══════════════════════════════════════════════════════════╝

    public Difficulty CurrentDifficulty => GetDifficulty(CurrentLevel);

    public static Difficulty GetDifficulty(int lvl) => lvl switch
    {
        <= 30 => Difficulty.VeryEasy,
        <= 70 => Difficulty.Easy,
        <= 120 => Difficulty.Medium,
        <= 200 => Difficulty.Hard,
        _ => Difficulty.VeryHard
    };

    public static string DifficultyName(Difficulty d) => d switch
    {
        Difficulty.VeryEasy => "ÇOK KOLAY",
        Difficulty.Easy => "KOLAY",
        Difficulty.Medium => "ORTA",
        Difficulty.Hard => "ZOR",
        Difficulty.VeryHard => "ÇOK ZOR",
        _ => ""
    };

    public static string DifficultyColor(Difficulty d) => d switch
    {
        Difficulty.VeryEasy => "#4CAF50",
        Difficulty.Easy => "#8BC34A",
        Difficulty.Medium => "#FF9800",
        Difficulty.Hard => "#F44336",
        Difficulty.VeryHard => "#9C27B0",
        _ => "#888888"
    };

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  SEVİYE BAŞLATMA                                         ║
    // ╚═══════════════════════════════════════════════════════════╝

    public void StartLevel(int level)
    {
        CurrentLevel = Math.Clamp(level, 1, MaxLevel);
        var cfg = LevelConfigProvider.GetConfig(CurrentLevel);
        ActiveConfig = cfg;

        // Temel durum sıfırla
        RotationAngle = 0f;
        PlacedPins.Clear();
        PrePlacedPins.Clear();
        _prePinBaseAngles.Clear();
        IsPinFlying = false;
        IsPaused = false;
        State = GameState.Playing;
        _oscillationTime = 0f;
        _reversalTimer = 0f;
        _prePinTimer = 0f;
        _shieldPassed = false;

        // Config → Engine state
        PinsToPlace = SafePinCount(cfg);
        PinsRemaining = PinsToPlace;
        IsBossLevel = cfg.IsBoss;
        CurrentMode = cfg.Mode;
        CurrentBehavior = cfg.RotationBehaviorType;

        // Açısal hız
        _omegaBase = cfg.BaseRotationSpeed * (_rng.NextDouble() > 0.5 ? 1f : -1f);
        _omegaCurrent = _omegaBase;

        // Salınım
        bool needsOsc = cfg.OscillationFrequency > 0 ||
            cfg.RotationBehaviorType is RotationBehavior.Oscillating or RotationBehavior.Chaos;
        _oscillationEnabled = needsOsc;
        _oscillationFreqHz = cfg.OscillationFrequency > 0 ? cfg.OscillationFrequency : 0.5f;

        // Yön değişimi
        _directionReversalEnabled = cfg.DirectionChangeInterval > 0;
        _reversalIntervalSec = cfg.DirectionChangeInterval;

        // Çarpışma eşiği
        _collisionThreshold = MathF.Max(MIN_COLLISION_THRESHOLD, cfg.Threshold);

        // Kalkan
        HasShield = cfg.HasShield;
        ShieldAngle = 0f;
        _shieldSpeed = cfg.ShieldSpeed * (_rng.NextDouble() > 0.5 ? 1f : -1f);
        ShieldGapAngle = MathF.Max(30f, cfg.ShieldGapDeg) * MathF.PI / 180f;

        // Hareketli İğneler
        HasMovingPins = cfg.MovingPrePins;
        _prePinOscRange = cfg.PrePinOscRange;

        // Mod + behavior state sıfırla
        _burstTimer = 0f;
        IsBurstActive = false;
        HasSecondPin = false;
        PinPlaceTimes.Clear();
        _gameTimer = 0f;
        CircleScale = 1f;
        _accelFactor = 1f;
        _fakeReverseTimer = 0f;
        _fakeReversed = false;

        SpawnPrePins(cfg.PrePins);
    }

    private int SafePinCount(LevelConfig cfg)
    {
        float th = MathF.Max(MIN_COLLISION_THRESHOLD, cfg.Threshold);
        int maxSafe = (int)(TWO_PI / th * 0.70f);
        return Math.Min(cfg.NeedleCount, Math.Max(2, maxSafe - cfg.PrePins));
    }

    private void SpawnPrePins(int count)
    {
        if (count <= 0) return;
        float spacing = TWO_PI / count;
        for (int i = 0; i < count; i++)
        {
            float baseAngle = i * spacing;
            float jitter = (float)(_rng.NextDouble() - 0.5) * spacing * 0.25f;
            float angle = Normalize(baseAngle + jitter);
            PrePlacedPins.Add(angle);
            _prePinBaseAngles.Add(angle);
        }
    }

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  OYUN DÖNGÜSÜ                                            ║
    // ╚═══════════════════════════════════════════════════════════╝

    public void Update(float dt)
    {
        if (State != GameState.Playing || IsPaused) return;
        _gameTimer += dt;

        // ── 1. Temel Açısal Hız ──────────────────────────────
        if (_oscillationEnabled)
        {
            _oscillationTime += dt;
            _omegaCurrent = _omegaBase * (1 + 0.4f * MathF.Sin(
                2 * MathF.PI * _oscillationFreqHz * _oscillationTime));
        }
        else
        {
            _omegaCurrent = _omegaBase;
        }

        // ── 2. Yön Değişimi ──────────────────────────────────
        if (_directionReversalEnabled)
        {
            _reversalTimer += dt;
            if (_reversalTimer >= _reversalIntervalSec)
            {
                _reversalTimer = 0f;
                _omegaBase = -_omegaBase;
            }
        }

        // ── 3. RotationBehavior ──────────────────────────────
        float behaviorMult = 1f;
        switch (CurrentBehavior)
        {
            case RotationBehavior.Accelerating:
                _accelFactor = MathF.Min(2.5f, 1f + _gameTimer * 0.05f);
                behaviorMult = _accelFactor;
                break;

            case RotationBehavior.Decelerating:
                _accelFactor = MathF.Max(0.3f, 1f - _gameTimer * 0.03f);
                behaviorMult = _accelFactor;
                break;

            case RotationBehavior.SpeedBurst:
                _burstTimer += dt;
                float cyclePos = _burstTimer % BURST_CYCLE;
                IsBurstActive = cyclePos < BURST_DURATION;
                if (IsBurstActive) behaviorMult = BURST_MULTIPLIER;
                break;

            case RotationBehavior.SlowMotion:
                float slowCycle = _gameTimer % 4f;
                if (slowCycle < 1f) behaviorMult = 0.3f; // 1sn yavaş
                break;

            case RotationBehavior.FakeReverse:
                _fakeReverseTimer += dt;
                if (!_fakeReversed && _fakeReverseTimer > 3f)
                {
                    _fakeReversed = true;
                    _omegaBase = -_omegaBase; // kısa ters
                }
                if (_fakeReversed && _fakeReverseTimer > 3.4f)
                {
                    _fakeReversed = false;
                    _fakeReverseTimer = 0f;
                    _omegaBase = -_omegaBase; // geri dön
                }
                break;

            case RotationBehavior.Chaos:
                // Accelerating + SpeedBurst + FakeReverse birleşimi
                _accelFactor = 1f + 0.5f * MathF.Sin(_gameTimer * 0.7f);
                _burstTimer += dt;
                IsBurstActive = (_burstTimer % 2.5f) < 0.5f;
                behaviorMult = _accelFactor * (IsBurstActive ? 1.8f : 1f);
                break;
        }

        // ── 4. GameMode SpeedBurst (ayrı mod) ────────────────
        if (CurrentMode == GameMode.SpeedBurst && CurrentBehavior != RotationBehavior.SpeedBurst)
        {
            _burstTimer += dt;
            float cp = _burstTimer % BURST_CYCLE;
            IsBurstActive = cp < BURST_DURATION;
            if (IsBurstActive) behaviorMult *= BURST_MULTIPLIER;
        }

        // ── 5. Shrinking Modu ────────────────────────────────
        if (CurrentMode == GameMode.Shrinking)
        {
            CircleScale = SHRINK_MIN + (1f - SHRINK_MIN) *
                (0.5f + 0.5f * MathF.Cos(TWO_PI * _gameTimer / SHRINK_CYCLE));
        }

        // ── 6. Final Dönüş ──────────────────────────────────
        RotationAngle = Normalize(RotationAngle + _omegaCurrent * behaviorMult * dt);

        // ── 6. Kalkan Halkası Dönüşü ─────────────────────────
        if (HasShield)
            ShieldAngle = Normalize(ShieldAngle + _shieldSpeed * dt);

        // ── 7. Hareketli Pre-Pin'ler ─────────────────────────
        if (HasMovingPins && _prePinBaseAngles.Count > 0)
        {
            _prePinTimer += dt;
            for (int i = 0; i < PrePlacedPins.Count && i < _prePinBaseAngles.Count; i++)
            {
                float offset = _prePinOscRange * MathF.Sin(TWO_PI * 0.4f * _prePinTimer + i * 1.5f);
                PrePlacedPins[i] = Normalize(_prePinBaseAngles[i] + offset);
            }
        }

        // ── 8. Uçan İğne ────────────────────────────────────
        if (IsPinFlying)
        {
            FlyingPinY -= PIN_FLY_SPEED * dt;

            // Kalkan kontrolü
            if (HasShield && !_shieldPassed && FlyingPinY <= ShieldRadiusOffset)
            {
                float gapHalf = ShieldGapAngle / 2f;
                if (AngleDistance(MathF.PI, ShieldAngle) > gapHalf)
                {
                    State = GameState.GameOver;
                    OnShieldHit?.Invoke();
                    OnCollision?.Invoke();
                    return;
                }
                _shieldPassed = true;
            }

            // İğne daireye ulaştı
            if (FlyingPinY <= 0f)
            {
                IsPinFlying = false;
                float pinAngle = Normalize(MathF.PI - RotationAngle);

                if (HasCollision(pinAngle))
                {
                    State = GameState.GameOver;
                    OnCollision?.Invoke();
                    return;
                }

                // Sapla
                PlacePin(pinAngle);

                // DoublePin: simetrik ikinci iğne (180° karşı)
                if (CurrentMode == GameMode.DoublePin && HasSecondPin)
                {
                    float secondAngle = Normalize(pinAngle + MathF.PI);
                    if (HasCollision(secondAngle))
                    {
                        State = GameState.GameOver;
                        OnCollision?.Invoke();
                        return;
                    }
                    PlacePin(secondAngle);
                    HasSecondPin = false;
                }

                if (PinsRemaining <= 0)
                {
                    State = GameState.LevelComplete;
                    OnLevelCleared?.Invoke();
                }
            }
        }
    }

    private void PlacePin(float angle)
    {
        PlacedPins.Add(angle);
        PinPlaceTimes.Add(_gameTimer);
        PinsRemaining--;
        Score += IsBossLevel ? 3 : 1;
        if (Score > HighScore) HighScore = Score;
        OnPinPlaced?.Invoke();
    }

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  FIRLAMA / DURAKLATMA / ÇARPIŞMA                        ║
    // ╚═══════════════════════════════════════════════════════════╝

    public void Shoot(float startY = 280f)
    {
        if (State != GameState.Playing || IsPinFlying || IsPaused) return;
        if (PinsRemaining <= 0) return;
        IsPinFlying = true;
        FlyingPinY = startY;
        _shieldPassed = false;

        // DoublePin modunda ikinci iğne flag'i
        if (CurrentMode == GameMode.DoublePin && PinsRemaining >= 2)
            HasSecondPin = true;
        else
            HasSecondPin = false;
    }

    public void TogglePause()
    {
        if (State == GameState.Playing)
            IsPaused = !IsPaused;
    }

    private bool HasCollision(float newAngle)
    {
        for (int i = 0; i < PlacedPins.Count; i++)
            if (AngleDistance(newAngle, PlacedPins[i]) < _collisionThreshold)
                return true;
        for (int i = 0; i < PrePlacedPins.Count; i++)
            if (AngleDistance(newAngle, PrePlacedPins[i]) < _collisionThreshold)
                return true;
        return false;
    }

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  DURUM YÖNETİMİ                                          ║
    // ╚═══════════════════════════════════════════════════════════╝

    public void Restart() { Score = 0; StartLevel(1); }

    public void NextLevel()
    {
        if (CurrentLevel < MaxLevel) StartLevel(CurrentLevel + 1);
        else State = GameState.Victory;
    }

    public void RestoreState(
        int level, int score, int highScore,
        List<float> placedPins, List<float> prePlacedPins,
        float rotationAngle, int pinsRemaining)
    {
        StartLevel(level);
        Score = score;
        HighScore = highScore;
        RotationAngle = rotationAngle;
        PlacedPins.Clear();
        PlacedPins.AddRange(placedPins);
        PrePlacedPins.Clear();
        PrePlacedPins.AddRange(prePlacedPins);
        PinsRemaining = pinsRemaining;
        State = GameState.Playing;
        IsPinFlying = false;
    }
}

// ╔═══════════════════════════════════════════════════════════════╗
// ║  YARDIMCI TİPLER                                             ║
// ╚═══════════════════════════════════════════════════════════════╝

public enum GameState { Ready, Playing, LevelComplete, GameOver, Victory }

public enum Difficulty { VeryEasy, Easy, Medium, Hard, VeryHard }

public enum GameMode
{
    Normal,      // Standart tek iğne fırlatma
    SpeedBurst,  // Daire ani hızlanıp yavaşlar
    DoublePin,   // 2 iğne aynı anda simetrik
    Invisible,   // İğneler 2sn sonra görünmez olur
    Shrinking    // Daire periyodik küçülüp büyür
}

