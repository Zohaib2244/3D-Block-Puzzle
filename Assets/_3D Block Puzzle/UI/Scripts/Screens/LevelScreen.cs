using UnityEngine;
using TMPro;
using DG.Tweening;
public class LevelScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI levelTimeText;
    [SerializeField] private Transform clockHandTransform;

    private int levelTime = 0;
    private float animationTimeOffset = 0f; // Accumulates animation time offsets

    // Keeps track if timer is currently frozen
    private bool isTimerFrozen = false;
    private Sequence freezeTimerSequence;

    public void StartLevelTime(int levelTime)
    {
        StopTimer();
        DebugLogger.Log("Starting level : " + (GameConstants.CurrentLevelIndex + 1), DebugColor.Purple);
        levelText.text = "Level " + (GameConstants.CurrentLevelIndex + 1);
        this.levelTime = levelTime;
        animationTimeOffset = 0f;
        StartTimer();
    }
    
    void StartTimer()
    {
        // Reset timer variables
        UpdateTimerDisplay();
        
        // Reset clock hand position
        if (clockHandTransform != null)
        {
            // Kill any existing animations on the clock hand
            DOTween.Kill(clockHandTransform);
            clockHandTransform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    
        // Create a sequence that repeats until stopped
        DOTween.Sequence()
            .AppendCallback(() =>
            {
                // Only decrement if not frozen
                if (!isTimerFrozen)
                {
                    levelTime--;
                    UpdateTimerDisplay();
    
                    // Check if timer is low to trigger warning animations
                    if (levelTime <= 15)
                    {
                        AnimateTimerWarning();
                    }
    
                    // Check if timer reached zero
                    if (levelTime <= 0)
                    {
                        OnTimerFinished();
                    }
                }
            })
            .AppendInterval(1f) // Wait 1 second between each tick
            .SetLoops(-1) // Repeat indefinitely
            .SetId("LevelTimer"); // Give it an ID so we can kill it later
    }
    
    private void UpdateTimerDisplay()
    {
        // Format time as M:SS
        int minutes = levelTime / 60;
        int seconds = levelTime % 60;
        levelTimeText.text = $"{minutes}:{seconds:00}";
        
        // Update clock hand rotation if it exists
        if (clockHandTransform != null)
        {
            // Determine which quarter position to show (0, 90, 180, or 270 degrees)
            int quarterPosition = seconds % 4; // Will give 0, 1, 2, or 3
            float rotation = quarterPosition * 90f; // Convert to 0, 90, 180, or 270 degrees
            
            DebugLogger.Log($"Clock hand rotation: {rotation} degrees", DebugColor.Green);
            // Snap to the quarter position (12, 3, 6, or 9 o'clock)
            clockHandTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }
    }
    private void AnimateTimerWarning()
    {
        // Kill any existing animations on the text
        DOTween.Kill(levelTimeText.transform);

        // Calculate intensity based on time left (more intense as time decreases)
        float intensity = Mathf.Clamp01((15f - levelTime) / 15f);
        float duration = Mathf.Lerp(0.8f, 0.3f, intensity); // Faster flashing as time decreases
        float scaleFactor = Mathf.Lerp(1.15f, 1.3f, intensity); // Larger scale as time decreases

        // Create a sequence for the warning animation
        Sequence warningSequence = DOTween.Sequence();

        // Color flash animation (normal to red)
        warningSequence.Append(levelTimeText.DOColor(Color.red, duration * 0.5f));
        warningSequence.Append(levelTimeText.DOColor(Color.white, duration * 0.5f));

        // Scale pulse animation (simultaneously)
        warningSequence.Join(levelTimeText.transform.DOScale(scaleFactor, duration * 0.5f));
        warningSequence.Join(levelTimeText.transform.DOScale(1f, duration * 0.5f).SetDelay(duration * 0.5f));

        // Set the sequence to play once (it will be called again on next timer tick)
        warningSequence.SetId(levelTimeText.transform);
    }

    private void OnTimerFinished()
    {
        // Stop the timer
        DOTween.Kill("LevelTimer");

        GameManager.Instance.LevelFailed();
    }
    public void PauseTimer()
    {
        DOTween.Kill("LevelTimer");
        DOTween.Kill(levelTimeText.transform);
        isTimerFrozen = true; // Set flag to prevent further countdown
        levelTimeText.DOColor(Color.gray, 0.3f); // Change color to indicate pause
    }
    public void ResumeTimer()
    {
        if (isTimerFrozen)
        {
            isTimerFrozen = false; // Reset flag
            levelTimeText.DOColor(Color.white, 0.3f); // Change color back to normal
            StartTimer(); // Restart the timer
        }
    }
    public void StopTimer()
    {
        DOTween.Kill("LevelTimer");
        DOTween.Kill(levelTimeText.transform);
        DOTween.Kill("TimeAddAnimation");
        DOTween.Kill("TimeFreezeAnimation");

        if (freezeTimerSequence != null)
        {
            freezeTimerSequence.Kill();
            freezeTimerSequence = null;
        }

        ResetTimer();
    }
    
    void ResetTimer()
    {
        levelTime = 0;
        animationTimeOffset = 0f;
        levelTimeText.text = "0:00";
        levelTimeText.color = Color.white;
        levelTimeText.transform.localScale = Vector3.one;
        isTimerFrozen = false;
    }

    /// <summary>
    /// Adds the specified amount of seconds to the timer
    /// </summary>
    /// <param name="secondsToAdd">Number of seconds to add</param>
    public void AddTime(int secondsToAdd)
    {
        if (secondsToAdd <= 0) return;
        
        // Add time to the actual game timer
        levelTime += secondsToAdd;
        UpdateTimerDisplay();
        
        // Visual feedback for adding time
        DOTween.Kill("TimeAddAnimation");
        
        // Animation timing constants
        float animDuration = 0.8f; // Total animation duration
        
        // Create a sequence for the time add animation
        Sequence addTimeSequence = DOTween.Sequence();
        
        // Store original color
        Color originalColor = levelTimeText.color;
        
        // Flash green and scale up/down
        addTimeSequence.Append(levelTimeText.DOColor(Color.green, animDuration * 0.375f));
        addTimeSequence.Join(levelTimeText.transform.DOScale(1.3f, animDuration * 0.375f));
        addTimeSequence.AppendInterval(animDuration * 0.25f);
        addTimeSequence.Append(levelTimeText.DOColor(originalColor, animDuration * 0.375f));
        addTimeSequence.Join(levelTimeText.transform.DOScale(1f, animDuration * 0.375f));
        
        addTimeSequence.SetId("TimeAddAnimation");
    }
    
    /// <summary>
    /// Freezes the timer for the specified duration
    /// </summary>
    /// <param name="freezeDuration">Duration in seconds to freeze the timer</param>
    public void FreezeTimer(float freezeDuration)
    {
        if (freezeDuration <= 0 || isTimerFrozen) return;
        
        // Stop any existing freeze sequence
        if (freezeTimerSequence != null)
        {
            freezeTimerSequence.Kill();
        }
        
        // Set flag to prevent timer from counting down
        isTimerFrozen = true;
        
        // Animation transition times
        float fadeInTime = 0.3f;
        float fadeOutTime = 0.3f;
        
        // Visual feedback for freezing time
        DOTween.Kill("TimeFreezeAnimation");
        
        // Create a sequence for the freeze animation
        Sequence freezeAnimation = DOTween.Sequence();
        
        // Make timer text blue while frozen
        Color originalColor = levelTimeText.color;
        freezeAnimation.Append(levelTimeText.DOColor(Color.cyan, fadeInTime));
        
        // Slow pulse while frozen
        freezeAnimation.Join(
            DOTween.Sequence()
            .Append(levelTimeText.transform.DOScale(1.1f, 0.8f))
            .Append(levelTimeText.transform.DOScale(1f, 0.8f))
            .SetLoops(-1)
        );
        
        freezeAnimation.SetId("TimeFreezeAnimation");
        
        // Create a sequence to unfreeze after the duration
        freezeTimerSequence = DOTween.Sequence();
        freezeTimerSequence.AppendInterval(freezeDuration);
        freezeTimerSequence.AppendCallback(() => {
            // Unfreeze timer
            isTimerFrozen = false;
            
            // Stop freeze animation
            DOTween.Kill("TimeFreezeAnimation");
            
            // Return to normal color
            levelTimeText.DOColor(originalColor, fadeOutTime);
            levelTimeText.transform.DOScale(1f, fadeOutTime);
        });
    }
}