using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
public class LevelSelectionButtonItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image levelImage;
    [SerializeField] private Button levelButton;

    Color levelButtonColor;
    public void Initialize(int levelNumber, Color color, bool lockState)
    {
        levelText.text = levelNumber.ToString();
        levelButtonColor = color;
        levelImage.color = color;
        if (lockState)
            levelButton.onClick.AddListener(OnLockButtonClicked);
        else
            levelButton.onClick.AddListener(OnUnlockButtonClicked);
        PlayInitializeAnimation();
    }
    public void SwitchLevel(int levelNumber, Color color, bool lockState)
    {
        levelButtonColor = color;
        PlayLevelSwitchAnimation(levelNumber);
        levelButton.onClick.RemoveAllListeners();
        if (!lockState)
            levelButton.onClick.AddListener(OnUnlockButtonClicked);
        else
            levelButton.onClick.AddListener(OnLockButtonClicked);
    }
    void OnLockButtonClicked()
    {
    
    }

    void OnUnlockButtonClicked()
    {
        AudioManager.Instance.PlayButtonClick();
        levelButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 1, 0.5f).OnComplete(() =>
        {
            DebugLogger.Log("Level " + levelText.text + " clicked!");
            GameConstants.CurrentLevelIndex = int.Parse(levelText.text) - 1;
            GameManager.Instance.LoadLevel(GameConstants.CurrentLevelIndex);
        });
    }
    public void PlayInitializeAnimation()
    {
        levelButton.transform.localScale = Vector3.one * 0.7f;
        levelButton.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
    }
    public void PlayLevelSwitchAnimation(int levelNumber)
    {
        levelButton.transform.DOScale(0.7f, 0.2f).SetEase(Ease.OutBack);
        levelImage.DOColor(levelButtonColor, 0.2f).OnComplete(() =>
        {
            levelText.text = levelNumber.ToString();
            levelImage.DOColor(levelButtonColor, 0.2f);
            levelButton.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        });
    }
    public void Despawn()
    {
        levelButton.transform.DOScale(0.7f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}