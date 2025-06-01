using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class LevelFailedScreen : MonoBehaviour
{
    [SerializeField] private Transform mainImg;
    [SerializeField] private Transform star1;
    [SerializeField] private Transform star2;
    [SerializeField] private Transform star3;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private Ease entranceEase = Ease.OutBack;
    [SerializeField] private Ease starEase = Ease.OutElastic;
    [SerializeField] private float autoRetryDelay = 1.0f;
    
    private Sequence animationSequence;
    
    private void Awake()
    {
        // Hide stars initially
        HideStars();
        
        // Set initial states for animated elements
        HideElements();
    }
    
    private void HideStars()
    {
        if (star1) star1.localScale = Vector3.zero;
        if (star2) star2.localScale = Vector3.zero;
        if (star3) star3.localScale = Vector3.zero;
    }
    
    private void HideElements()
    {
        if (mainImg) mainImg.localScale = Vector3.zero;
    }
    void OnEnable()
    {
        Show(3); // Show with 0 stars by default
    }
    public void Show(int starsEarned = 0)
    {
        // Make sure we're visible
        gameObject.SetActive(true);
        
        // Stop any running animations
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }
        
        // Create animation sequence
        animationSequence = DOTween.Sequence();
        
        AudioManager.Instance?.PlayLevelFail(); // Play level failed sound if available
        
        // Animate main image popping in
        if (mainImg)
        {
            mainImg.localScale = Vector3.zero;
            animationSequence.Append(mainImg.DOScale(1f, 0.4f).SetEase(entranceEase));
            
            // Do a little shake animation after appearing
            animationSequence.Append(mainImg.DOShakeRotation(0.5f, 15f, 10, 90, true));
        }
        
        // Animate stars with delay between each
        animationSequence.AppendInterval(0.2f);
        
        // Determine how many stars to show based on performance
        AnimateStars(starsEarned);
        
        // Add delay before auto-retry
        animationSequence.AppendInterval(autoRetryDelay);
        
        // Auto retry at the end of the animation
        animationSequence.OnComplete(() => {
            // Auto-retry the level
            GameManager.Instance.RetryLevel();
            
            // Hide this screen
            Hide();
        });
        
        // Play the sequence
        animationSequence.Play();
    }
    
    private void AnimateStars(int starsEarned)
    {
        // Always show grayed-out stars first
        Transform[] stars = { star1, star2, star3 };
        
        // Ensure starsEarned is within valid range
        starsEarned = Mathf.Clamp(starsEarned, 0, 3);
        
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            
            // Store reference for closure
            Transform star = stars[i];
            
            // Reset scale
            star.localScale = Vector3.zero;
            
            // Whether this star is earned
            bool earned = i < starsEarned;
            
            // Add animation to sequence with delay between stars
            animationSequence.Append(
                star.DOScale(earned ? 1.2f : 0.8f, 0.3f)
                    .SetEase(starEase)
            );
            
            // Slight bounce back if earned
            if (earned)
            {
                animationSequence.Append(star.DOScale(1f, 0.2f).SetEase(Ease.OutBounce));
                
                // Add a subtle rotation for earned stars
                animationSequence.Join(
                    star.DOLocalRotate(new Vector3(0, 0, Random.Range(-10f, 10f)), 0.2f)
                        .SetEase(Ease.OutQuad)
                );
            }
            
            // Short delay before next star
            animationSequence.AppendInterval(0.1f);
        }
    }
    
    private void Hide()
    {
        // Stop any running animations
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }
        
        // Create exit animation
        Sequence exitSequence = DOTween.Sequence();
        
        if (mainImg)
            exitSequence.Append(mainImg.DOScale(0, 0.3f).SetEase(Ease.InBack));
            
        // Hide stars
        if (star1) exitSequence.Join(star1.DOScale(0, 0.2f).SetEase(Ease.InBack));
        if (star2) exitSequence.Join(star2.DOScale(0, 0.2f).SetEase(Ease.InBack));
        if (star3) exitSequence.Join(star3.DOScale(0, 0.2f).SetEase(Ease.InBack));
        
        // Disable gameObject after animation completes
        exitSequence.OnComplete(() => gameObject.SetActive(false));
        
        exitSequence.Play();
    }
    
    private void OnDestroy()
    {
        // Clean up DOTween animations
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }
    }
}