using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AAPlus.Services;

namespace AAPlus.ViewModels;

public partial class PinGameViewModel : ObservableObject
{
    private readonly SaveManager _save;
    private readonly AudioHapticService _audio;

    public PinGameEngine Engine { get; } = new();

    private float _levelDelay;
    private const float LevelTransition = 0.6f;
    private bool _init;

    public PinGameViewModel(SaveManager save, AudioHapticService audio)
    {
        _save = save;
        _audio = audio;

        Engine.OnPinPlaced += () => _audio.PlayDotPlaced();
        Engine.OnCollision += () =>
        {
            _audio.PlayGameOver();
            // Game over — seviyeyi kaydet, DEVAM ET ile tekrar deneyebilsin
            _save.Data.HasActiveGame = true;
            _save.Data.CurrentLevel = Engine.CurrentLevel;
            _save.Data.PinsRemaining = 0;
            _save.Data.PlacedPinAngles.Clear();
            _save.Data.PrePlacedPinAngles.Clear();
            _save.Data.Score = Engine.Score;
            _save.Data.HighScore = Engine.HighScore;
            _save.Data.BestLevel = Math.Max(_save.Data.BestLevel, Engine.CurrentLevel);
            _ = _save.SaveAsync();
        };
        Engine.OnLevelCleared += () => _audio.PlayLevelComplete();
    }

    public void Initialize()
    {
        if (_init) return;
        _init = true;

        Engine.HighScore = _save.Data.HighScore;

        // SaveManager'daki flag'i oku (navigasyondan ÖNCE set edildi)
        bool continueMode = _save.ContinueRequested;
        _save.ContinueRequested = false; // flag'i temizle

        if (continueMode && _save.HasSavedGame)
        {
            var d = _save.Data;
            if (d.PlacedPinAngles.Count > 0 && d.PinsRemaining > 0)
            {
                // Uygulama kapatılmıştı — tam devam (iğne pozisyonları dahil)
                _save.RestoreGameState(Engine);
                System.Diagnostics.Debug.WriteLine(
                    $"[Game] DEVAM: Level {d.CurrentLevel}, {d.PinsRemaining} iğne kaldı, {d.PlacedPinAngles.Count} saplanmış");
            }
            else
            {
                // Game Over sonrası — aynı seviyeyi baştan başlat
                Engine.StartLevel(d.CurrentLevel);
                System.Diagnostics.Debug.WriteLine(
                    $"[Game] DEVAM: Level {d.CurrentLevel} baştan");
            }
        }
        else
        {
            // Yeni oyun
            Engine.StartLevel(1);
            Engine.Score = 0;
            System.Diagnostics.Debug.WriteLine("[Game] YENİ OYUN: Level 1");
        }
    }

    [RelayCommand]
    private async Task Tap()
    {
        switch (Engine.State)
        {
            case GameState.Playing:
                if (Engine.IsPaused)
                    Engine.TogglePause();
                else
                    Engine.Shoot();
                _audio.PlayTap();
                break;

            case GameState.GameOver:
                _save.Data.TotalGames++;
                await _save.SaveAsync();
                await Shell.Current.GoToAsync("//MainMenuPage");
                break;

            case GameState.Victory:
                _save.Data.TotalGames++;
                await _save.SaveAsync();
                await Shell.Current.GoToAsync("//MainMenuPage");
                break;
        }
    }

    public void PauseTapped() => Engine.TogglePause();

    public event Action? OnLevelTransition;

    public void UpdateFrame(float dt)
    {
        Engine.Update(dt);

        if (Engine.State == GameState.LevelComplete)
        {
            _levelDelay += dt;
            if (_levelDelay >= LevelTransition)
            {
                _levelDelay = 0;
                Engine.NextLevel();
                OnLevelTransition?.Invoke();
                AutoSave();
            }
        }
    }

    public void AutoSave()
    {
        if (Engine.State == GameState.Playing || Engine.State == GameState.LevelComplete)
        {
            _save.CaptureGameState(Engine);
            _ = _save.SaveAsync();
        }
    }

    public void Reset()
    {
        _init = false;
        _levelDelay = 0;
    }
}
