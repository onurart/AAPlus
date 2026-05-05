using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AAPlus.Services;

namespace AAPlus.ViewModels;

[QueryProperty(nameof(StartLevel), "level")]
public partial class PinGameViewModel : ObservableObject
{
    private readonly GameDataService _data;
    private readonly AudioHapticService _audio;

    public PinGameEngine Engine { get; } = new();

    [ObservableProperty] private string? _startLevel;

    private float _levelDelay;
    private const float LevelTransition = 0.6f;
    private bool _init;

    public PinGameViewModel(GameDataService data, AudioHapticService audio)
    {
        _data = data;
        _audio = audio;

        // Engine event'lerini dinle
        Engine.OnPinPlaced += () => _audio.PlayDotPlaced();
        Engine.OnCollision += () => { _audio.PlayGameOver(); SaveProgress(); };
        Engine.OnLevelCleared += () => _audio.PlayLevelComplete();
    }

    public void Initialize()
    {
        if (_init) return;
        _init = true;

        int level = 1;
        if (!string.IsNullOrEmpty(StartLevel) && int.TryParse(StartLevel, out int p))
            level = p;

        Engine.HighScore = _data.GetHighScore();
        Engine.StartLevel(level);
    }

    [RelayCommand]
    private async void Tap()
    {
        switch (Engine.State)
        {
            case GameState.Playing:
                if (Engine.IsPaused)
                    Engine.TogglePause(); // Devam et
                else
                    Engine.Shoot();
                _audio.PlayTap();
                break;

            case GameState.GameOver:
                SaveProgress();
                _data.IncrementTotalGames();
                await Shell.Current.GoToAsync("//MainMenuPage");
                break;

            case GameState.Victory:
                SaveProgress();
                await Shell.Current.GoToAsync("//MainMenuPage");
                break;
        }
    }

    public void PauseTapped()
    {
        Engine.TogglePause();
    }

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
                if (Engine.CurrentLevel % 5 == 0)
                    SaveProgress();
            }
        }
    }

    private void SaveProgress()
    {
        _data.SetHighScore(Engine.HighScore);
        _data.SetBestLevel(Engine.CurrentLevel);
        _data.SetLastLevel(Engine.CurrentLevel);
    }

    public void Reset()
    {
        _init = false;
        _levelDelay = 0;
    }
}
