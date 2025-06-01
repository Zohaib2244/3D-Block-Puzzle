using DG.Tweening;
using UnityEngine;

public class GateObject : MonoBehaviour
{
    public Vector2Int[] gatePosition;
    float originalY;
    public GameObject exitVFX;
    public void MoveDown()
    {
        originalY = transform.position.y;
        transform.DOMoveY(-0.1f, 0.2f);
    }
    public void MoveUp()
    {
        transform.DOMoveY(originalY, 0.2f);
    }
}