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

    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private string? _startLevel;

    private float _levelCompleteDelay;
    private const float LevelTransitionTime = 0.6f;
    private bool _initialized;

    public PinGameViewModel(GameDataService data, AudioHapticService audio)
    {
        _data = data;
        _audio = audio;
        Engine.HighScore = _data.GetHighScore();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        int level = 1;
        if (!string.IsNullOrEmpty(StartLevel) && int.TryParse(StartLevel, out int parsed))
            level = parsed;

        Engine.HighScore = _data.GetHighScore();
        Engine.StartLevel(level);
        IsPlaying = true;
    }

    [RelayCommand]
    private async void Tap()
    {
        switch (Engine.State)
        {
            case GameState.Playing:
                Engine.ShootPin(280f);
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

    public event Action? OnLevelTransition;

    public void UpdateFrame(float deltaSeconds)
    {
        Engine.Update(deltaSeconds);

        if (Engine.State == GameState.GameOver && _levelCompleteDelay == 0)
        {
            _audio.PlayGameOver();
            SaveProgress();
        }

        if (Engine.State == GameState.LevelComplete)
        {
            if (_levelCompleteDelay == 0)
                _audio.PlayLevelComplete();

            _levelCompleteDelay += deltaSeconds;
            if (_levelCompleteDelay >= LevelTransitionTime)
            {
                _levelCompleteDelay = 0;
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

    public void ResetForNewGame()
    {
        _initialized = false;
        _levelCompleteDelay = 0;
    }
}
