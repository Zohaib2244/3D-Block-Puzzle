using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    #region Singleton
    public static GameUIManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    [SerializeField] private List<UISCreens> uiScreens = new List<UISCreens>();
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private CanvasGroup bg1CanvasGroup;
    [SerializeField] private CanvasGroup bg2CanvasGroup;
    [Header("References")]
    [SerializeField] private LevelScreen levelScreen;
    public LevelScreen LevelScreen => levelScreen;
    /// <summary>
    /// Shows the specified screen type with an optional background index and completion callback.
    /// </summary>
    /// <param name="screenType">The Screen Type To be Displayed</param>
    /// <param name="bgIndex">Select Whether To Show Blurred Bg or Normal, 0 for Normal, 1 for Blurred</param>
    /// <param name="onComplete">Function To Be Called When The Screen is Activated</param>
    public void ShowScreen(ScreenType screenType, int bgIndex = 0, Action onComplete = null)
    {
        // First handle the background transition
        SwitchBackground(bgIndex);

        foreach (var screen in uiScreens)
        {
            if (screen.screenType == screenType)
            {
                if (screen.screenTransform.gameObject.activeSelf)
                    return;

                screen.screenTransform.gameObject.SetActive(true);
                if (screen.showOverlay)
                {
                    overlayCanvasGroup.gameObject.SetActive(true);
                    overlayCanvasGroup.alpha = 0f;
                    overlayCanvasGroup.DOFade(1f, 0.2f);
                }
                else
                {
                    if (overlayCanvasGroup.gameObject.activeSelf)
                    {
                        overlayCanvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
                        {
                            overlayCanvasGroup.gameObject.SetActive(false);
                        });
                    }
                    else
                    {
                        overlayCanvasGroup.gameObject.SetActive(false);
                    }
                }
                onComplete?.Invoke();
            }
            else
            {
                screen.screenTransform.gameObject.SetActive(false);
            }
        }
    }
    
    private void SwitchBackground(int bgIndex)
    {
        // Duration for fade animations
        float fadeDuration = 0.3f;
        
        // Handle background 1
        if (bgIndex == 1)
        {
            // Make sure BG1 is active
            bg1CanvasGroup.gameObject.SetActive(true);
            bg1CanvasGroup.DOFade(1f, fadeDuration);
        }
        else
        {
            // Fade out BG1 if it's active
            if (bg1CanvasGroup.gameObject.activeSelf)
            {
                bg1CanvasGroup.DOFade(0f, fadeDuration).OnComplete(() => 
                {
                    bg1CanvasGroup.gameObject.SetActive(false);
                });
            }
        }
        
        // Handle background 2
        if (bgIndex == 2)
        {
            // Make sure BG2 is active
            bg2CanvasGroup.gameObject.SetActive(true);
            bg2CanvasGroup.DOFade(1f, fadeDuration);
        }
        else
        {
            // Fade out BG2 if it's active
            if (bg2CanvasGroup.gameObject.activeSelf)
            {
                bg2CanvasGroup.DOFade(0f, fadeDuration).OnComplete(() => 
                {
                    bg2CanvasGroup.gameObject.SetActive(false);
                });
            }
        }
    }
}