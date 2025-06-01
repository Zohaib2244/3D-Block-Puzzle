using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
public class PauseScreen : MonoBehaviour
{

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private LevelScreen levelScreen;

    private float animationDuration = 0.3f;
    private Vector2 hiddenPosition = new Vector2(0, -Screen.height);
    private Vector2 visiblePosition = new Vector2(0, 0);

    private bool isInitialized = false;

    ScreenType previousScreenType = ScreenType.GamePlay;
    #region Essentials
    void OnEnable()
    {
        InitializePanel();
        Open();
    }

    #endregion

    #region Methods
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
    }
    public void RestartLevel()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance.RetryLevel();
        Close();
    }
    public void Home()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance.currentLevelState = LevelState.None;
        previousScreenType = ScreenType.MainMenu;
        GameManager.Instance.UnloadAllLevels();
        Close();
    }
    public void PauseGame()
    {
        AudioManager.Instance?.PlayButtonClick();
        previousScreenType = ScreenType.GamePlay;
        levelScreen?.PauseTimer();
        GameUIManager.Instance.ShowScreen(ScreenType.Pause);
        GameConstants.inputEnabled = false;
    }
    #endregion
    #region Panel Animation
    /// <summary>
    /// Opens the pause screen with an animation.
    /// </summary>
    public void Open()
    {
        // Make panel active
        gameObject.SetActive(true);

        // Animate the panel in
        canvasGroup.DOFade(1, animationDuration).SetUpdate(true);
        panelRect.DOAnchorPos(visiblePosition, animationDuration).SetEase(Ease.OutCirc).SetUpdate(true);

        // Enable interactions
        DOTween.Sequence()
            .AppendInterval(animationDuration * 0.5f)
            .AppendCallback(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }).SetUpdate(true);
    }

    /// <summary>
    /// Closes the pause screen and returns to the main menu.
    /// </summary>
    public void Close()
    {
        AudioManager.Instance?.PlayButtonClick();
        // Disable interactions immediately
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        GameConstants.inputEnabled = true;
        // Animate the panel out
        canvasGroup.DOFade(0, animationDuration);
        panelRect.DOAnchorPos(hiddenPosition, animationDuration).SetEase(Ease.InCirc)
            .OnComplete(() =>
            {
                panelRect.anchoredPosition = hiddenPosition;
                // Deactivate the panel after animation
                gameObject.SetActive(false);
                levelScreen?.ResumeTimer();

                if (previousScreenType == ScreenType.GamePlay)
                {
                    GameUIManager.Instance.ShowScreen(ScreenType.GamePlay);
                }
                else
                {
                    GameUIManager.Instance.ShowScreen(previousScreenType, 1);
                }
            });
    }
    #endregion
}