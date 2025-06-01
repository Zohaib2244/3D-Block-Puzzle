using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private int loadingTime = 3;

    Tween loadingTween;
    void OnEnable()
    {
        StartLoading();
    }
    void OnDisable()
    {
        loadingTween?.Kill();
        loadingTween = null;
    }
    private void StartLoading()
    {
        loadingTween?.Kill();
        // Simulate a loading process
        loadingTween = DOVirtual.DelayedCall(loadingTime, () =>
        {
            OnLoadingComplete();
        });
    }
    void OnLoadingComplete()
    {
        // Loading complete
        GameUIManager.Instance.ShowScreen(ScreenType.MainMenu, 1);
    }
}