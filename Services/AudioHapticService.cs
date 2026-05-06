namespace AAPlus.Services;

// ═══════════════════════════════════════════════════════════════════
//  AudioHapticService — Ses + Titreşim birleşik servisi
// ═══════════════════════════════════════════════════════════════════

public class AudioHapticService
{
    private readonly SaveManager _save;
    private readonly SoundService _sound;

    public AudioHapticService(SaveManager save)
    {
        _save = save;
        _sound = new SoundService(save);
    }

    private bool HapticOn => _save.Data.HapticEnabled;

    public void PlayTap()
    {
        if (HapticOn)
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        _sound.PlayTap();
    }

    public void PlayDotPlaced()
    {
        if (HapticOn)
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        _sound.PlayPinPlaced();
    }

    public void PlayGameOver()
    {
        if (HapticOn)
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
        _sound.PlayGameOver();
    }

    public void PlayLevelComplete()
    {
        if (HapticOn)
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        _sound.PlayLevelComplete();
    }

    public void PlayShieldHit()
    {
        if (HapticOn)
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
        _sound.PlayShieldHit();
    }
}
