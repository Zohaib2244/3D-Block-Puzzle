using UnityEngine;
using System.IO;

[CreateAssetMenu(fileName = "New Grid Data", menuName = "3D Block Puzzle/Grid Data")]
public class GridData : ScriptableObject
{
    // Grid dimensions
    public int gridWidth = 10;
    public int gridLength = 10;
    public float cellSize = 1.0f;
    public Vector3 gridStartPosition = Vector3.zero;
    public GateData gateData;
    [SerializeField] private bool dirtyFlag = false; // Used to track modifications

    #if UNITY_EDITOR
    // For debugging - track when data changes happen
    [TextArea(3, 5)]
    [SerializeField] private string lastModification = "No modifications recorded";
    #endif

    // Serializable arrays to store grid state
    [System.Serializable]
    public class SerializableGridData
    {
        public bool[] occupiedCells;
        public bool[] wallCells;
    }
    
    public SerializableGridData gridData;
    
    // Initialize arrays
    public void Initialize(int width, int length)
    {
        gridWidth = width;
        gridLength = length;
        gridData = new SerializableGridData
        {
            occupiedCells = new bool[width * length],
            wallCells = new bool[width * length]
        };
        
        MarkDirty("Initialize called");
    }
    
    // Helper methods to convert between 2D and 1D indices
    public int GetIndex(int x, int z)
    {
        return z * gridWidth + x;
    }
    
    public Vector2Int GetCoordinates(int index)
    {
        return new Vector2Int(index % gridWidth, index / gridWidth);
    }
    
    public void InitializeGateData(int gateCount, int totalPositions)
    {
        gateData = new GateData();
        gateData.gateColorTypes = new int[gateCount];
        gateData.gateDirections = new int[gateCount];
        gateData.gatePositionCounts = new int[gateCount];
        gateData.gatePositionsX = new int[totalPositions];
        gateData.gatePositionsZ = new int[totalPositions];
        
        MarkDirty("InitializeGateData called");
    }

    // Mark data as dirty and ensure it gets saved
    public void MarkDirty(string modificationReason)
    {
        dirtyFlag = true;
        
        #if UNITY_EDITOR
        // Record when and why this change happened
        lastModification = $"Modified: {System.DateTime.Now}\nReason: {modificationReason}\n" +
                          $"In play mode: {UnityEditor.EditorApplication.isPlaying}";
        
        // Request immediate save in editor
        UnityEditor.EditorUtility.SetDirty(this);
        
        // Optional: Request more aggressive saving if in play mode
        if (UnityEditor.EditorApplication.isPlaying)
        {
            SaveDataToBackup();
            UnityEditor.AssetDatabase.SaveAssets();
        }
        #endif
    }
    
    // Backup system to prevent data loss during play mode
    #if UNITY_EDITOR
    private void SaveDataToBackup()
    {
        try
        {
            // Only backup if we have meaningful data
            if (gridData == null || (gridData.occupiedCells == null && gridData.wallCells == null))
                return;
                
            string backupDir = "Assets/_3D Block Puzzle/Gameplay/Data/Backups";
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
            
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string backupPath = $"{backupDir}/{assetName}_backup.json";
            
            // Create a serializable version of our data
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(backupPath, json);
            
            Debug.Log($"GridData backup saved to {backupPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to backup GridData: {e.Message}");
        }
    }
    
    // Method to restore from backup if needed
    [ContextMenu("Restore From Backup")]
    public void RestoreFromBackup()
    {
        try
        {
            string backupDir = "Assets/_3D Block Puzzle/Gameplay/Data/Backups";
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string backupPath = $"{backupDir}/{assetName}_backup.json";
            
            if (!File.Exists(backupPath))
            {
                Debug.LogError($"No backup file found at {backupPath}");
                return;
            }
            
            string json = File.ReadAllText(backupPath);
            JsonUtility.FromJsonOverwrite(json, this);
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"GridData restored from {backupPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to restore GridData from backup: {e.Message}");
        }
    }
    #endif
    
    // Validate data when loading to catch potential issues
    private void OnEnable()
    {
        ValidateData();
    }
    
    public void ValidateData()
    {
        // Check if data structures match grid dimensions
        if (gridData != null)
        {
            if (gridData.occupiedCells != null && gridData.occupiedCells.Length != gridWidth * gridLength)
            {
                Debug.LogWarning($"GridData: Occupied cells array size ({gridData.occupiedCells.Length}) " +
                                $"doesn't match grid dimensions ({gridWidth}x{gridLength})");
            }
            
            if (gridData.wallCells != null && gridData.wallCells.Length != gridWidth * gridLength)
            {
                Debug.LogWarning($"GridData: Wall cells array size ({gridData.wallCells.Length}) " +
                                $"doesn't match grid dimensions ({gridWidth}x{gridLength})");
            }
        }
        
        // Any other validation logic you need
    }
}