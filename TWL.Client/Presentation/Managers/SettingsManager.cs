using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace TWL.Client.Presentation.Managers;

public class SettingsManager
{
    // Helps restore volume after unmuting
    private bool _isMutedByFocus;

    public SettingsManager()
    {
        // Set initial defaults
        ApplyAudioSettings();
    }

    // 0.0 to 1.0
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 1.0f;
    public float SfxVolume { get; set; } = 1.0f;

    // 0: Slow, 1: Normal, 2: Fast
    public int TextSpeed { get; set; } = 1;

    public bool MuteOnUnfocus { get; set; } = true;

    public void ApplyAudioSettings()
    {
        if (_isMutedByFocus)
        {
            return; // Don't apply if currently muted by focus loss
        }

        // MonoGame SoundEffect MasterVolume controls all sound effects
        // We combine Master * Sfx for effective SFX volume
        SoundEffect.MasterVolume = Math.Clamp(MasterVolume * SfxVolume, 0f, 1f);

        // MediaPlayer Volume controls music
        MediaPlayer.Volume = Math.Clamp(MasterVolume * MusicVolume, 0f, 1f);
    }

    public void SetMuteState(bool isMuted)
    {
        if (isMuted)
        {
            SoundEffect.MasterVolume = 0f;
            MediaPlayer.Volume = 0f;
            _isMutedByFocus = true;
        }
        else
        {
            _isMutedByFocus = false;
            ApplyAudioSettings(); // Restore user settings
        }
    }
}