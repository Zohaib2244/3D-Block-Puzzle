using UnityEngine;
using DG.Tweening;
using System;

public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private RectTransform titleTextTransform;
    [SerializeField] private RectTransform playButtonTransform;
    [SerializeField] private CanvasGroup settingsButtonTransform;


    private float animationDuration = 0.8f;
    private float titleDelayTime = 0.1f;
    private float buttonDelayTime = 0.3f;
    private float settingsDelayTime = 0.5f;

    private Vector2 initialTitlePos;
    private Vector2 initialPlayButtonPos;
    private Sequence introSequence;

    void Awake()
    {
        // Store original positions
        initialTitlePos = titleTextTransform.anchoredPosition;
        initialPlayButtonPos = playButtonTransform.anchoredPosition;
    }

    void OnEnable()
    {
        // Reset positions for animation
        SetupInitialOffscreenPositions();

        // Begin intro animation
        AnimateIntro();
    }

    private void SetupInitialOffscreenPositions()
    {
        // Position title above the screen (outside viewport)
        titleTextTransform.anchoredPosition = new Vector2(
            initialTitlePos.x,
            initialTitlePos.y + Screen.height);

        // Position play button below the screen (outside viewport)
        playButtonTransform.anchoredPosition = new Vector2(
            initialPlayButtonPos.x,
            initialPlayButtonPos.y - Screen.height);

        // Hide settings button
        settingsButtonTransform.alpha = 0f;
    }

    private void AnimateIntro()
    {
        // Kill any existing sequences
        if (introSequence != null)
        {
            introSequence.Kill();
        }

        // Create new sequence
        introSequence = DOTween.Sequence();

        // Animate title from below to original position
        introSequence.Append(
            titleTextTransform.DOAnchorPos(initialTitlePos, animationDuration)
                .SetEase(Ease.OutCirc)
                .SetDelay(titleDelayTime)
        );

        // Animate play button from above to original position
        introSequence.Join(
            playButtonTransform.DOAnchorPos(initialPlayButtonPos, animationDuration)
                .SetEase(Ease.OutCirc)
                .SetDelay(buttonDelayTime)
        );

        // Fade in settings button
        introSequence.Join(
            settingsButtonTransform.DOFade(1f, animationDuration * 0.7f)
                .SetDelay(settingsDelayTime)
        );
    }
    void AnimateExit(Action onComplete)
    {
        // Kill any existing sequences
        if (introSequence != null)
        {
            introSequence.Kill();
        }

        // Create new sequence for exit animation
        introSequence = DOTween.Sequence();

        // Animate title up and off screen
        introSequence.Append(
            titleTextTransform.DOAnchorPosY(initialTitlePos.y + Screen.height, 0.5f)
                .SetEase(Ease.InCirc)
        );

        // Animate play button down and off screen
        introSequence.Join(
            playButtonTransform.DOAnchorPosY(initialPlayButtonPos.y - Screen.height, 0.5f)
                .SetEase(Ease.InCirc)
        );

        // Fade out settings button
        introSequence.Join(
            settingsButtonTransform.DOFade(0f, 0.3f)
        );

        // Call the onComplete action after the animation completes
        introSequence.OnComplete(() => onComplete?.Invoke());
    }
    public void PlayGame()
    {
                AudioManager.Instance?.PlayButtonClick();

        // Call the exit animation and then load the next screen
        AnimateExit(() =>
        {
            GameUIManager.Instance.ShowScreen(ScreenType.LevelSelection, 2);
        });
    }
    public void OpenSettings()
    {        AudioManager.Instance?.PlayButtonClick();

        // Call the exit animation and then load the settings screen
        AnimateExit(() =>
        {
            GameUIManager.Instance.ShowScreen(ScreenType.Settings, 2);
        });
    }
}