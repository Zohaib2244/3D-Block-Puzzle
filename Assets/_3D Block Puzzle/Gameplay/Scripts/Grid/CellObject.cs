using UnityEngine;

public class CellObject : MonoBehaviour
{
    [SerializeField] Vector2Int cellPosition;
    public Vector2Int CellPosition
    {
        get => cellPosition;
        set
        {
            cellPosition = value;
        }
    }
}