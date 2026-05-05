namespace AAPlus.Services;

public class GameDataService
{
    private const string HighScoreKey = "high_score";
    private const string BestLevelKey = "best_level";
    private const string LastLevelKey = "last_level";
    private const string SoundEnabledKey = "sound_enabled";
    private const string HapticEnabledKey = "haptic_enabled";
    private const string TotalGamesKey = "total_games";

    public int GetHighScore() => Preferences.Get(HighScoreKey, 0);
    public void SetHighScore(int score) => Preferences.Set(HighScoreKey, score);

    public int GetBestLevel() => Preferences.Get(BestLevelKey, 1);
    public void SetBestLevel(int level) { if (level > GetBestLevel()) Preferences.Set(BestLevelKey, level); }

    public int GetLastLevel() => Preferences.Get(LastLevelKey, 1);
    public void SetLastLevel(int level) => Preferences.Set(LastLevelKey, level);

    public bool IsSoundEnabled() => Preferences.Get(SoundEnabledKey, true);
    public void SetSoundEnabled(bool v) => Preferences.Set(SoundEnabledKey, v);

    public bool IsHapticEnabled() => Preferences.Get(HapticEnabledKey, true);
    public void SetHapticEnabled(bool v) => Preferences.Set(HapticEnabledKey, v);

    public int GetTotalGames() => Preferences.Get(TotalGamesKey, 0);
    public void IncrementTotalGames() => Preferences.Set(TotalGamesKey, GetTotalGames() + 1);
}
