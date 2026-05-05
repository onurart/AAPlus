using System;
using System.Collections.Generic;

namespace AAPlus.Services;

/// <summary>
/// "aa" tarzı nokta ateşleme oyun motoru — 500 seviye, 5 zorluk kademesi.
/// </summary>
public class PinGameEngine
{
    public int CurrentLevel { get; private set; } = 1;
    public const int MaxLevel = 500;
    public Difficulty CurrentDifficulty => GetDifficulty(CurrentLevel);

    public int PinsToPlace { get; private set; }
    public int PinsRemaining { get; private set; }

    public float RotationAngle { get; private set; }
    public float BaseRotationSpeed { get; private set; }
    private float _currentRotationSpeed;
    private bool _oscillating;
    private float _oscillationTimer;
    private float _oscillationFrequency;
    private bool _reversingDirection;
    private float _reverseTimer;
    private float _reverseInterval;

    public List<float> PlacedPinAngles { get; } = new();
    public List<float> PrePlacedPinAngles { get; } = new();

    public bool IsPinFlying { get; private set; }
    public float FlyingPinY { get; private set; }
    public float FlyingPinSpeed { get; } = 1800f;

    public GameState State { get; private set; } = GameState.Ready;
    public int Score { get; private set; }
    public int HighScore { get; set; }

    public const float CircleRadius = 45f;
    public const float PinLength = 75f;
    public const float PinHeadRadius = 12f;
    public const float CollisionAngleThreshold = 0.22f;

    private readonly Random _rand = new();

    // ═══════════════════════════════════════════════════════════
    //  ZORLUK SİSTEMİ
    // ═══════════════════════════════════════════════════════════

    public static Difficulty GetDifficulty(int level) => level switch
    {
        <= 50 => Difficulty.VeryEasy,
        <= 150 => Difficulty.Easy,
        <= 300 => Difficulty.Medium,
        <= 400 => Difficulty.Hard,
        _ => Difficulty.VeryHard
    };

    public static string GetDifficultyName(Difficulty d) => d switch
    {
        Difficulty.VeryEasy => "Çok Kolay",
        Difficulty.Easy => "Kolay",
        Difficulty.Medium => "Orta",
        Difficulty.Hard => "Zor",
        Difficulty.VeryHard => "Çok Zor",
        _ => ""
    };

    public static string GetDifficultyColor(Difficulty d) => d switch
    {
        Difficulty.VeryEasy => "#4CAF50",
        Difficulty.Easy => "#8BC34A",
        Difficulty.Medium => "#FF9800",
        Difficulty.Hard => "#F44336",
        Difficulty.VeryHard => "#9C27B0",
        _ => "#999999"
    };

    // ═══════════════════════════════════════════════════════════
    //  SEVİYE BAŞLATMA
    // ═══════════════════════════════════════════════════════════

    public void StartLevel(int level)
    {
        CurrentLevel = Math.Min(level, MaxLevel);
        RotationAngle = 0;
        PlacedPinAngles.Clear();
        PrePlacedPinAngles.Clear();
        IsPinFlying = false;
        State = GameState.Playing;
        _oscillationTimer = 0;
        _reverseTimer = 0;

        var config = GetLevelConfig(CurrentLevel);
        PinsToPlace = config.PinsToPlace;
        PinsRemaining = PinsToPlace;
        BaseRotationSpeed = config.RotationSpeed;
        _currentRotationSpeed = BaseRotationSpeed;
        _oscillating = config.Oscillating;
        _oscillationFrequency = config.OscillationFrequency;
        _reversingDirection = config.ReversingDirection;
        _reverseInterval = config.ReverseInterval;

        if (_rand.NextDouble() > 0.5)
            BaseRotationSpeed = -BaseRotationSpeed;

        PlacePrePins(config.PrePlacedPins);
    }

    private LevelConfig GetLevelConfig(int level)
    {
        var difficulty = GetDifficulty(level);
        return difficulty switch
        {
            Difficulty.VeryEasy => new LevelConfig
            {
                PinsToPlace = 3 + (level - 1) / 3,
                RotationSpeed = 1.0f + level * 0.025f,
                PrePlacedPins = 0,
                Oscillating = false,
                ReversingDirection = false
            },
            Difficulty.Easy => new LevelConfig
            {
                PinsToPlace = 6 + (level - 51) / 4,
                RotationSpeed = 2.0f + (level - 51) * 0.018f,
                PrePlacedPins = (level - 51) / 20,
                Oscillating = false,
                ReversingDirection = level > 100
            },
            Difficulty.Medium => new LevelConfig
            {
                PinsToPlace = 8 + (level - 151) / 5,
                RotationSpeed = 3.2f + (level - 151) * 0.014f,
                PrePlacedPins = 2 + (level - 151) / 20,
                Oscillating = level > 200,
                OscillationFrequency = 0.5f + (level - 200) * 0.006f,
                ReversingDirection = true,
                ReverseInterval = Math.Max(2f, 5f - (level - 151) * 0.015f)
            },
            Difficulty.Hard => new LevelConfig
            {
                PinsToPlace = 10 + (level - 301) / 4,
                RotationSpeed = 4.8f + (level - 301) * 0.018f,
                PrePlacedPins = 4 + (level - 301) / 15,
                Oscillating = true,
                OscillationFrequency = 1.0f + (level - 301) * 0.01f,
                ReversingDirection = true,
                ReverseInterval = Math.Max(1.5f, 3f - (level - 301) * 0.012f)
            },
            Difficulty.VeryHard => new LevelConfig
            {
                PinsToPlace = 14 + (level - 401) / 3,
                RotationSpeed = 6.0f + (level - 401) * 0.022f,
                PrePlacedPins = 6 + (level - 401) / 12,
                Oscillating = true,
                OscillationFrequency = 1.5f + (level - 401) * 0.012f,
                ReversingDirection = true,
                ReverseInterval = Math.Max(0.8f, 2f - (level - 401) * 0.01f)
            },
            _ => new LevelConfig { PinsToPlace = 5, RotationSpeed = 1.5f }
        };
    }

    private void PlacePrePins(int count)
    {
        if (count <= 0) return;
        float spacing = MathF.PI * 2f / (count + PlacedPinAngles.Count + PinsToPlace);
        for (int i = 0; i < count; i++)
        {
            float baseAngle = i * MathF.PI * 2f / count;
            float jitter = (float)(_rand.NextDouble() - 0.5) * spacing * 0.3f;
            PrePlacedPinAngles.Add(NormalizeAngle(baseAngle + jitter));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  GÜNCELLEME
    // ═══════════════════════════════════════════════════════════

    public void Update(float deltaSeconds)
    {
        if (State != GameState.Playing) return;

        if (_oscillating)
        {
            _oscillationTimer += deltaSeconds;
            float oscillation = MathF.Sin(_oscillationTimer * _oscillationFrequency * MathF.PI * 2f);
            _currentRotationSpeed = BaseRotationSpeed * (1f + oscillation * 0.4f);
        }
        else
        {
            _currentRotationSpeed = BaseRotationSpeed;
        }

        if (_reversingDirection)
        {
            _reverseTimer += deltaSeconds;
            if (_reverseTimer >= _reverseInterval)
            {
                _reverseTimer = 0;
                BaseRotationSpeed = -BaseRotationSpeed;
            }
        }

        RotationAngle += _currentRotationSpeed * deltaSeconds;

        if (IsPinFlying)
        {
            FlyingPinY -= FlyingPinSpeed * deltaSeconds;
            if (FlyingPinY <= 0)
            {
                IsPinFlying = false;
                float pinAngle = NormalizeAngle(-RotationAngle);

                if (CheckCollision(pinAngle))
                {
                    State = GameState.GameOver;
                    return;
                }

                PlacedPinAngles.Add(pinAngle);
                PinsRemaining--;
                Score++;

                if (Score > HighScore) HighScore = Score;
                if (PinsRemaining <= 0) State = GameState.LevelComplete;
            }
        }
    }

    public void ShootPin(float startY)
    {
        if (State != GameState.Playing || IsPinFlying) return;
        IsPinFlying = true;
        FlyingPinY = startY;
    }

    private bool CheckCollision(float newAngle)
    {
        foreach (var a in PlacedPinAngles)
            if (AngleDistance(newAngle, a) < CollisionAngleThreshold) return true;
        foreach (var a in PrePlacedPinAngles)
            if (AngleDistance(newAngle, a) < CollisionAngleThreshold) return true;
        return false;
    }

    private static float AngleDistance(float a, float b)
    {
        float diff = MathF.Abs(NormalizeAngle(a) - NormalizeAngle(b));
        return MathF.Min(diff, MathF.PI * 2f - diff);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= MathF.PI * 2f;
        if (angle < 0) angle += MathF.PI * 2f;
        return angle;
    }

    public void Reset() { Score = 0; StartLevel(1); }

    public void NextLevel()
    {
        if (CurrentLevel < MaxLevel) StartLevel(CurrentLevel + 1);
        else State = GameState.Victory;
    }
}

public enum GameState { Ready, Playing, LevelComplete, GameOver, Victory }

public enum Difficulty { VeryEasy, Easy, Medium, Hard, VeryHard }

internal class LevelConfig
{
    public int PinsToPlace { get; init; } = 5;
    public float RotationSpeed { get; init; } = 1.5f;
    public int PrePlacedPins { get; init; }
    public bool Oscillating { get; init; }
    public float OscillationFrequency { get; init; } = 1f;
    public bool ReversingDirection { get; init; }
    public float ReverseInterval { get; init; } = 3f;
}
