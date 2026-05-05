namespace AAPlus.Services;

public class AudioHapticService
{
    private readonly SaveManager _save;

    public AudioHapticService(SaveManager save) => _save = save;

    private bool HapticOn => _save.Data.HapticEnabled;
    private bool SoundOn => _save.Data.SoundEnabled;

    public void PlayTap()
    {
        if (!HapticOn) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    public void PlayDotPlaced()
    {
        if (!HapticOn) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    public void PlayGameOver()
    {
        if (!HapticOn) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
    }

    public void PlayLevelComplete()
    {
        if (!HapticOn) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }
}
