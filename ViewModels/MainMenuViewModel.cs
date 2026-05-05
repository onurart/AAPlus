using CommunityToolkit.Mvvm.ComponentModel;
using AAPlus.Services;

namespace AAPlus.ViewModels;

public partial class MainMenuViewModel : ObservableObject
{
    private readonly SaveManager _save;

    [ObservableProperty] private int _highScore;
    [ObservableProperty] private int _bestLevel;
    [ObservableProperty] private int _totalGames;
    [ObservableProperty] private bool _soundEnabled;
    [ObservableProperty] private bool _hasSavedGame;
    [ObservableProperty] private int _savedLevel;
    [ObservableProperty] private int _savedPinsRemaining;

    public MainMenuViewModel(SaveManager save)
    {
        _save = save;
        LoadData();
    }

    public void LoadData()
    {
        var d = _save.Data;
        HighScore = d.HighScore;
        BestLevel = d.BestLevel;
        TotalGames = d.TotalGames;
        SoundEnabled = d.SoundEnabled;
        HasSavedGame = _save.HasSavedGame;
        SavedLevel = d.CurrentLevel;
        SavedPinsRemaining = d.PinsRemaining;
    }

    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        _save.Data.SoundEnabled = SoundEnabled;
        _ = _save.SaveAsync();
    }

    public bool CanContinue => HasSavedGame;
    public int LastLevel => SavedLevel;
}
