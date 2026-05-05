using System;
using System.Collections.Generic;

namespace AAPlus.Services;

/// <summary>
/// "aa" tarzı iğne saplama oyun motoru.
/// İğneler alttan merkeze fırlatılır, daireye saplanır ve birlikte döner.
/// Çarpışma = Game Over.
/// </summary>
public class PinGameEngine
{
    // ─── Seviye ──────────────────────────────────────────────
    public int CurrentLevel { get; private set; } = 1;
    public const int MaxLevel = 500;

    // ─── İğne Sayıları ──────────────────────────────────────
    public int PinsToPlace { get; private set; }
    public int PinsRemaining { get; private set; }

    // ─── Dönüş ──────────────────────────────────────────────
    public float RotationAngle { get; private set; }
    private float _baseSpeed;
    private float _currentSpeed;

    // Salınım (ileri seviyelerde)
    private bool _oscillating;
    private float _oscTimer;
    private float _oscFreq;

    // Yön değişimi (ileri seviyelerde)
    private bool _reversing;
    private float _reverseTimer;
    private float _reverseInterval;

    // ─── Saplanmış İğneler (açı listesi) ────────────────────
    public List<float> PlacedPins { get; } = new();
    public List<float> PrePlacedPins { get; } = new();

    // ─── Fırlatılan İğne ────────────────────────────────────
    public bool IsPinFlying { get; private set; }
    public float FlyingPinY { get; private set; }
    private const float FlySpeed = 1800f;

    // ─── Durum ──────────────────────────────────────────────
    public GameState State { get; private set; } = GameState.Ready;
    public bool IsPaused { get; private set; }
    public int Score { get; private set; }
    public int HighScore { get; set; }

    // ─── Sabitler ───────────────────────────────────────────
    public const float CircleRadius = 50f;
    public const float PinLength = 70f;
    public const float PinHeadRadius = 8f;
    private const float CollisionThreshold = 0.20f;

    private readonly Random _rng = new();

    // ─── Event'ler (ses/haptic için) ────────────────────────
    public event Action? OnPinPlaced;
    public event Action? OnCollision;
    public event Action? OnLevelCleared;

    // ═══════════════════════════════════════════════════════════
    //  ZORLUK
    // ═══════════════════════════════════════════════════════════

    public Difficulty CurrentDifficulty => GetDifficulty(CurrentLevel);

    public static Difficulty GetDifficulty(int lvl) => lvl switch
    {
        <= 50 => Difficulty.VeryEasy,
        <= 150 => Difficulty.Easy,
        <= 300 => Difficulty.Medium,
        <= 400 => Difficulty.Hard,
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

    // ═══════════════════════════════════════════════════════════
    //  SEVİYE BAŞLAT
    // ═══════════════════════════════════════════════════════════

    public void StartLevel(int level)
    {
        CurrentLevel = Math.Clamp(level, 1, MaxLevel);
        RotationAngle = 0;
        PlacedPins.Clear();
        PrePlacedPins.Clear();
        IsPinFlying = false;
        IsPaused = false;
        State = GameState.Playing;
        _oscTimer = 0;
        _reverseTimer = 0;

        var cfg = BuildConfig(CurrentLevel);
        PinsToPlace = cfg.Pins;
        PinsRemaining = PinsToPlace;
        _baseSpeed = cfg.Speed * (_rng.NextDouble() > 0.5 ? 1 : -1);
        _currentSpeed = _baseSpeed;
        _oscillating = cfg.Oscillate;
        _oscFreq = cfg.OscFreq;
        _reversing = cfg.Reverse;
        _reverseInterval = cfg.ReverseInterval;

        SpawnPrePins(cfg.PrePins);
    }

    private LevelCfg BuildConfig(int lvl)
    {
        var d = GetDifficulty(lvl);
        return d switch
        {
            Difficulty.VeryEasy => new(
                Pins: 3 + (lvl - 1) / 3,
                Speed: 1.0f + lvl * 0.025f,
                PrePins: 0, Oscillate: false, OscFreq: 0,
                Reverse: false, ReverseInterval: 0),

            Difficulty.Easy => new(
                Pins: 6 + (lvl - 51) / 4,
                Speed: 2.0f + (lvl - 51) * 0.018f,
                PrePins: (lvl - 51) / 20,
                Oscillate: false, OscFreq: 0,
                Reverse: lvl > 100, ReverseInterval: 4f),

            Difficulty.Medium => new(
                Pins: 8 + (lvl - 151) / 5,
                Speed: 3.2f + (lvl - 151) * 0.014f,
                PrePins: 2 + (lvl - 151) / 20,
                Oscillate: lvl > 200,
                OscFreq: 0.5f + (lvl - 200) * 0.006f,
                Reverse: true,
                ReverseInterval: Math.Max(2f, 5f - (lvl - 151) * 0.015f)),

            Difficulty.Hard => new(
                Pins: 10 + (lvl - 301) / 4,
                Speed: 4.8f + (lvl - 301) * 0.018f,
                PrePins: 4 + (lvl - 301) / 15,
                Oscillate: true,
                OscFreq: 1.0f + (lvl - 301) * 0.01f,
                Reverse: true,
                ReverseInterval: Math.Max(1.5f, 3f - (lvl - 301) * 0.012f)),

            _ => new(
                Pins: 14 + (lvl - 401) / 3,
                Speed: 6.0f + (lvl - 401) * 0.022f,
                PrePins: 6 + (lvl - 401) / 12,
                Oscillate: true,
                OscFreq: 1.5f + (lvl - 401) * 0.012f,
                Reverse: true,
                ReverseInterval: Math.Max(0.8f, 2f - (lvl - 401) * 0.01f))
        };
    }

    private void SpawnPrePins(int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathF.PI * 2f / count;
            float jitter = (float)(_rng.NextDouble() - 0.5) * 0.3f;
            PrePlacedPins.Add(Normalize(angle + jitter));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  GÜNCELLEME (her frame)
    // ═══════════════════════════════════════════════════════════

    public void Update(float dt)
    {
        if (State != GameState.Playing || IsPaused) return;

        // Salınım
        if (_oscillating)
        {
            _oscTimer += dt;
            float osc = MathF.Sin(_oscTimer * _oscFreq * MathF.PI * 2f);
            _currentSpeed = _baseSpeed * (1f + osc * 0.4f);
        }
        else
        {
            _currentSpeed = _baseSpeed;
        }

        // Yön değişimi
        if (_reversing)
        {
            _reverseTimer += dt;
            if (_reverseTimer >= _reverseInterval)
            {
                _reverseTimer = 0;
                _baseSpeed = -_baseSpeed;
            }
        }

        // Dönüş
        RotationAngle += _currentSpeed * dt;

        // Uçan iğne
        if (IsPinFlying)
        {
            FlyingPinY -= FlySpeed * dt;

            // İğne daireye ulaştı
            if (FlyingPinY <= 0)
            {
                IsPinFlying = false;

                // Alttan geldiği için açı = π (aşağı), dönen daireye göre düzelt
                float pinAngle = Normalize(MathF.PI - RotationAngle);

                // Çarpışma kontrolü
                if (HasCollision(pinAngle))
                {
                    State = GameState.GameOver;
                    OnCollision?.Invoke();
                    return;
                }

                // İğneyi sapla
                PlacedPins.Add(pinAngle);
                PinsRemaining--;
                Score++;
                if (Score > HighScore) HighScore = Score;
                OnPinPlaced?.Invoke();

                // Seviye bitti mi?
                if (PinsRemaining <= 0)
                {
                    State = GameState.LevelComplete;
                    OnLevelCleared?.Invoke();
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  İĞNE FIRLATMA
    // ═══════════════════════════════════════════════════════════

    public void Shoot(float startY = 280f)
    {
        if (State != GameState.Playing || IsPinFlying || IsPaused) return;
        IsPinFlying = true;
        FlyingPinY = startY;
    }

    // ═══════════════════════════════════════════════════════════
    //  DURAKLATMA / DEVAM
    // ═══════════════════════════════════════════════════════════

    public void TogglePause()
    {
        if (State == GameState.Playing)
            IsPaused = !IsPaused;
    }

    // ═══════════════════════════════════════════════════════════
    //  ÇARPIŞMA
    // ═══════════════════════════════════════════════════════════

    private bool HasCollision(float newAngle)
    {
        foreach (var a in PlacedPins)
            if (AngleDist(newAngle, a) < CollisionThreshold) return true;
        foreach (var a in PrePlacedPins)
            if (AngleDist(newAngle, a) < CollisionThreshold) return true;
        return false;
    }

    private static float AngleDist(float a, float b)
    {
        float d = MathF.Abs(Normalize(a) - Normalize(b));
        return MathF.Min(d, MathF.PI * 2f - d);
    }

    private static float Normalize(float a)
    {
        a %= MathF.PI * 2f;
        if (a < 0) a += MathF.PI * 2f;
        return a;
    }

    // ═══════════════════════════════════════════════════════════
    //  DURUM YÖNETİMİ
    // ═══════════════════════════════════════════════════════════

    public void Restart() { Score = 0; StartLevel(1); }

    public void NextLevel()
    {
        if (CurrentLevel < MaxLevel) StartLevel(CurrentLevel + 1);
        else State = GameState.Victory;
    }
}

// ─── Yardımcı Tipler ────────────────────────────────────────

public enum GameState { Ready, Playing, LevelComplete, GameOver, Victory }

public enum Difficulty { VeryEasy, Easy, Medium, Hard, VeryHard }

internal record LevelCfg(
    int Pins, float Speed, int PrePins,
    bool Oscillate, float OscFreq,
    bool Reverse, float ReverseInterval);
