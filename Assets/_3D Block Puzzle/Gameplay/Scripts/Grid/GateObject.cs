using DG.Tweening;
using UnityEngine;

public class GateObject : MonoBehaviour
{
    public Vector2Int[] gatePosition;
    float originalY = 0f;
    public GameObject exitVFX;
    private Tween moveTween;
    private bool isDown = false;

    public void MoveDown()
    {
        if (isDown) return;
        if (originalY == 0f) originalY = transform.position.y;
        isDown = true;
        moveTween?.Kill();
        moveTween = transform.DOMoveY(-0.1f, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => moveTween = null);
    }

    public void MoveUp()
    {
        if (!isDown) return;
        isDown = false;
        moveTween?.Kill();
        moveTween = transform.DOMoveY(originalY, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => moveTween = null);
    }
}