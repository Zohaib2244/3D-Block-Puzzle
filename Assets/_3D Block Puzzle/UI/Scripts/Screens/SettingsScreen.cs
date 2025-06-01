using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SettingsScreen : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float animationDuration = 0.3f;

    [Header("Sound Settings")]
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle vibrationToggle;
     
    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    bool isInitialized = false;

    private void OnEnable()
    {
        InitializePanel();
        // Subscribe to settings change events
        Settings.OnSoundSettingChanged.AddListener(OnSoundSettingChanged);
        Settings.OnMusicSettingChanged.AddListener(OnMusicSettingChanged);
        Settings.OnVibrationSettingChanged.AddListener(OnVibrationSettingChanged);
        Open();
    }

    private void OnDisable()
    {
        // Unsubscribe from settings change events
        Settings.OnSoundSettingChanged.RemoveListener(OnSoundSettingChanged);
        Settings.OnMusicSettingChanged.RemoveListener(OnMusicSettingChanged);
        Settings.OnVibrationSettingChanged.RemoveListener(OnVibrationSettingChanged);
    }
 private void InitializePanel()
    {
        if (isInitialized) return;
        isInitialized = true;
        // Store positions for animations
        visiblePosition = panelRect.anchoredPosition;
        hiddenPosition = new Vector2(0, -Screen.height);
        
        // Initialize panel as hidden
        panelRect.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Setup event listeners
        SetupEventListeners();
        
        // Initialize UI state from saved settings
        InitializeUIState();
    }
    private void SetupEventListeners()
    {
        // Toggle event listeners
        if (soundToggle != null)
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        
        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
        
        // Button event listeners
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void InitializeUIState()
    {
        // Set toggle states based on current settings
        if (soundToggle != null)
            soundToggle.isOn = Settings.IsSoundEnabled;
        
        if (musicToggle != null)
            musicToggle.isOn = Settings.IsMusicEnabled;
        
        if (vibrationToggle != null)
            vibrationToggle.isOn = Settings.IsVibrationEnabled;
    }

    #region UI Event Handlers
    private void OnSoundToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButtonClick();
        Settings.IsSoundEnabled = isOn;
    }
    
    private void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButtonClick();
        Settings.IsMusicEnabled = isOn;
    }
    
    private void OnVibrationToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButtonClick();
        Settings.IsVibrationEnabled = isOn;
    }
    #endregion

    #region Settings Event Handlers
    private void OnSoundSettingChanged(bool isEnabled)
    {
        if (soundToggle != null && soundToggle.isOn != isEnabled)
            soundToggle.isOn = isEnabled;
        
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(isEnabled ? 1 : 0);
        }
    }
    
    private void OnMusicSettingChanged(bool isEnabled)
    {
        if (musicToggle != null && musicToggle.isOn != isEnabled)
            musicToggle.isOn = isEnabled;
        
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            if (isEnabled)
                AudioManager.Instance.ResumeMusic();
            else
                AudioManager.Instance.PauseMusic();

            AudioManager.Instance.SetMusicVolume(isEnabled ? 0.2f : 0);
        }
    }
    
    private void OnVibrationSettingChanged(bool isEnabled)
    {
        if (vibrationToggle != null && vibrationToggle.isOn != isEnabled)
            vibrationToggle.isOn = isEnabled;
    }
    #endregion

    #region Panel Animation
    public void Open()
    {        
        // Make panel active
        gameObject.SetActive(true);
        
        // Animate the panel in
        canvasGroup.DOFade(1, animationDuration);
        panelRect.DOAnchorPos(visiblePosition, animationDuration).SetEase(Ease.OutCirc);
        
        // Enable interactions
        DOTween.Sequence()
            .AppendInterval(animationDuration * 0.5f)
            .AppendCallback(() => {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
        
        // Refresh UI state when opening
        InitializeUIState();
    }
    
    public void Close()
    {
        
        AudioManager.Instance?.PlayButtonClick();
        
        // Disable interactions immediately
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Animate the panel out
        canvasGroup.DOFade(0, animationDuration);
        panelRect.DOAnchorPos(hiddenPosition, animationDuration).SetEase(Ease.InCirc)
            .OnComplete(() => {
                panelRect.anchoredPosition = hiddenPosition;
                // Deactivate the panel after animation
                gameObject.SetActive(false);

                GameUIManager.Instance.ShowScreen(ScreenType.MainMenu, 1);});
    }
    #endregion
}