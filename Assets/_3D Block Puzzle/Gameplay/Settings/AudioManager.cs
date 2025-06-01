using System;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Settings")]
    [SerializeField] private float defaultMusicVolume = 0.5f;
    [SerializeField] private float defaultSfxVolume = 0.7f;

    [Header("Audio Clips")]
    [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip levelComplete;
    [SerializeField] private AudioClip levelFail;
    [SerializeField] private AudioClip blockPlace;
    [SerializeField] private AudioClip blockMove;
    [SerializeField] private AudioClip blockRemove;
    [SerializeField] private AudioClip blockCollide;
    [SerializeField] private AudioClip collectiblePickup;
    [SerializeField] private AudioClip timerWarning;
    [SerializeField] private AudioClip timeFreeze;
    [SerializeField] private AudioClip timeAdd;

    // Audio settings keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SfxVolume";
    private const string MUTE_KEY = "Muted";

    private bool isMuted = false;
    private int currentMusicTrack = 0;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // Initialize audio sources if not set in inspector
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Load saved audio settings
        LoadAudioSettings();

        // Start playing background music
        PlayBackgroundMusic(0);
    }

    private void LoadAudioSettings()
    {
        musicSource.volume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
        sfxSource.volume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSfxVolume);
        isMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;

        // Apply mute settings
        if (isMuted)
        {
            musicSource.volume = 0;
            sfxSource.volume = 0;
        }
    }

    public void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicSource.volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxSource.volume);
        PlayerPrefs.SetInt(MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    #region Volume Controls
    public void SetMusicVolume(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, normalizedVolume);
        
        if (!isMuted)
        {
            musicSource.volume = normalizedVolume;
        }
        
        SaveAudioSettings();
    }

    public void SetSfxVolume(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, normalizedVolume);
        
        if (!isMuted)
        {
            sfxSource.volume = normalizedVolume;
        }
        
        SaveAudioSettings();
    }
    #endregion

    #region Music Functions
    public void PlayBackgroundMusic(int trackIndex)
    {
        if (musicTracks.Count == 0)
            return;

        // Make sure the index is valid
        trackIndex = Mathf.Clamp(trackIndex, 0, musicTracks.Count - 1);
        
        // Only change if needed
        if (musicSource.clip != musicTracks[trackIndex])
        {
            musicSource.Stop();
            musicSource.clip = musicTracks[trackIndex];
            currentMusicTrack = trackIndex;
            musicSource.Play();
            musicSource.loop = true;
        }
        else if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void PlayNextTrack()
    {
        currentMusicTrack = (currentMusicTrack + 1) % musicTracks.Count;
        PlayBackgroundMusic(currentMusicTrack);
    }

    public void PauseMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (!musicSource.isPlaying)
            musicSource.UnPause();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
    #endregion

    #region SFX Functions
    public void PlayButtonClick()
    {
        PlaySFX(buttonClick);
    }

    public void PlayLevelComplete()
    {
        PlaySFX(levelComplete);
    }

    public void PlayLevelFail()
    {
        PlaySFX(levelFail);
    }

    public void PlayBlockPlace()
    {
        PlaySFX(blockPlace);
    }

    public void PlayBlockMove()
    {
        PlaySFX(blockMove);
    }

    public void PlayBlockCollide()
    {
        PlaySFX(blockCollide);
    }

    public void PlayCollectiblePickup()
    {
        PlaySFX(collectiblePickup);
    }

    public void PlayTimerWarning()
    {
        PlaySFX(timerWarning);
    }

    public void PlayTimeFreeze()
    {
        PlaySFX(timeFreeze);
    }

    public void PlayTimeAdd()
    {
        PlaySFX(timeAdd);
    }
    public void PlayBlockRemove()
    {
        PlaySFX(blockRemove);
    }
    // Generic play function for one-off sound effects
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && !isMuted)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // For playing sound effects with specific volume
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip != null && !isMuted)
        {
            sfxSource.PlayOneShot(clip, volumeScale * sfxSource.volume);
        }
    }
    #endregion
}