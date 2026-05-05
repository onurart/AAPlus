using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AAPlus.Services;

namespace AAPlus.ViewModels;

public partial class MainMenuViewModel : ObservableObject
{
    private readonly GameDataService _data;

    [ObservableProperty] private int _highScore;
    [ObservableProperty] private int _bestLevel;
    [ObservableProperty] private int _lastLevel;
    [ObservableProperty] private int _totalGames;
    [ObservableProperty] private bool _soundEnabled;

    public MainMenuViewModel(GameDataService data)
    {
        _data = data;
        LoadData();
    }

    public void LoadData()
    {
        HighScore = _data.GetHighScore();
        BestLevel = _data.GetBestLevel();
        LastLevel = _data.GetLastLevel();
        TotalGames = _data.GetTotalGames();
        SoundEnabled = _data.IsSoundEnabled();
    }

    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        _data.SetSoundEnabled(SoundEnabled);
    }

    public bool CanContinue => LastLevel > 1;
}
