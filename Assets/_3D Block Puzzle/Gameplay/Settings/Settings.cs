using UnityEngine;
using UnityEngine.Events;

public static class Settings
{
    // Events
    public static readonly UnityEvent<bool> OnVibrationSettingChanged = new UnityEvent<bool>();
    public static readonly UnityEvent<bool> OnSoundSettingChanged = new UnityEvent<bool>();
    public static readonly UnityEvent<bool> OnMusicSettingChanged = new UnityEvent<bool>();

    private static bool _isVibrationEnabled;
    private static bool _isSoundEnabled;
    private static bool _isMusicEnabled;

    public static bool IsVibrationEnabled 
    {
        get => PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("VibrationEnabled", value ? 1 : 0);
            OnVibrationSettingChanged.Invoke(value);
        }
    }

    public static bool IsSoundEnabled 
    {
        get => PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("SoundEnabled", value ? 1 : 0);
            OnSoundSettingChanged.Invoke(value);
        }
    }

    public static bool IsMusicEnabled 
    {
        get => PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("MusicEnabled", value ? 1 : 0);
            OnMusicSettingChanged.Invoke(value);
        }
    }
}