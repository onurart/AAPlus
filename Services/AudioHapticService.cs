namespace AAPlus.Services;

public class AudioHapticService
{
    private readonly GameDataService _data;
    public AudioHapticService(GameDataService data) => _data = data;

    public void PlayTap()
    {
        if (!_data.IsHapticEnabled()) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    public void PlayDotPlaced()
    {
        if (!_data.IsHapticEnabled()) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    public void PlayGameOver()
    {
        if (!_data.IsHapticEnabled()) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
    }

    public void PlayLevelComplete()
    {
        if (!_data.IsHapticEnabled()) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }
}
