using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Block Shape", menuName = "3D Block Puzzle/Block Shape")]
public class BlockShape : ScriptableObject
{
    [System.Serializable]
    public class ShapeLayer
    {
        [Tooltip("Define the shape with cells. Each true value represents an active cell.")]
        public bool[,] cells = new bool[5, 5];

        // For inspector visualization (since Unity can't show 2D arrays in inspector)
        public bool[] cellsSerializable = new bool[25];

        // Transfer from serializable array to 2D array
        public void UpdateCellsFromSerializable()
        {
            cells = new bool[5, 5];
            for (int z = 0; z < 5; z++)
            {
                for (int x = 0; x < 5; x++)
                {
                    cells[x, z] = cellsSerializable[z * 5 + x];
                }
            }
        }

        // Transfer from 2D array to serializable array
        public void UpdateSerializableFromCells()
        {
            cellsSerializable = new bool[25];
            for (int z = 0; z < 5; z++)
            {
                for (int x = 0; x < 5; x++)
                {
                    cellsSerializable[z * 5 + x] = cells[x, z];
                }
            }
        }
    }

    public string shapeName;
    public ShapeLayer shapeDefinition = new ShapeLayer();
    
    [Tooltip("Reference to the custom mesh for this shape")]
    public Mesh customMesh;
    
    [Tooltip("Optional offset to adjust mesh position relative to pivot point")]
    public Vector3 meshOffset = Vector3.zero;
    
    [Tooltip("Defines which cell in the grid is the pivot point (default is 0,0)")]
    public Vector2Int pivotCell = Vector2Int.zero;
    
    // This method is called when the ScriptableObject is loaded
    private void OnEnable()
    {
        // Make sure to initialize the 2D array from serialized data whenever loaded
        shapeDefinition.UpdateCellsFromSerializable();
    }

    //* Get array of Vector2Int representing occupied cells relative to pivot
    public List<Vector2Int> GetOccupiedPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int center = new Vector2Int(2, 2); // Center of 5×5 grid
        
        for (int z = 0; z < 5; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (shapeDefinition.cells[x, z])
                {
                    // Convert from array indices to centered coordinates
                    Vector2Int centeredPos = new Vector2Int(x - center.x, z - center.y);
                    positions.Add(centeredPos);
                }
            }
        }
        return positions;
    }
public List<Vector2Int> GetRotatedPositions90()
{
    List<Vector2Int> rotated = new List<Vector2Int>();
    foreach (Vector2Int pos in GetOccupiedPositions())
    {
        // Rotate 90 degrees clockwise: (x,y) -> (y,-x)
        rotated.Add(new Vector2Int(pos.y, -pos.x));
    }
    return rotated;
}

public List<Vector2Int> GetRotatedPositions180()
{
    List<Vector2Int> rotated = new List<Vector2Int>();
    foreach (Vector2Int pos in GetOccupiedPositions())
    {
        // Rotate 180 degrees: (x,y) -> (-x,-y)
        rotated.Add(new Vector2Int(-pos.x, -pos.y));
    }
    return rotated;
}

public List<Vector2Int> GetRotatedPositions270()
{
    List<Vector2Int> rotated = new List<Vector2Int>();
    foreach (Vector2Int pos in GetOccupiedPositions())
    {
        // Rotate 250 degrees clockwise: (x,y) -> (-y,x)
        rotated.Add(new Vector2Int(-pos.y, pos.x));
    }
    return rotated;
}
}