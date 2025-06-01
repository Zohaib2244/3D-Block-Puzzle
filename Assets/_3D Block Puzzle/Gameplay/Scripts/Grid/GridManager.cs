using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;
using UnityEngine.Events;
using System.Linq;
using Voodoo.Utils;


public class GridManager : MonoBehaviour
{
    #region Variables

    [Header("Grid Properties")]
    [SerializeField] private GridData savedGridData;
    public GridData SavedGridData
    {
        get { return savedGridData; }
    }
    [SerializeField] private float gridSpacing = 2.25f; // Spacing between grid cells (for custom meshes)
    [SerializeField] private Transform wallParent; // Parent object for walls
    public Transform WallParent
    {
        get { return wallParent; }
        set { wallParent = value; }
    }
    private int gridWidth = 10; // Number of columns in the grid
    private int gridLength = 10; // Number of rows in the griZS
    private Vector3 gridStartPosition; // Starting position of the grid
    public Vector3 GridStartPosition
    {
        get { return gridStartPosition; }
        set { gridStartPosition = value; }
    }
    public int GetGridWidth() => gridWidth;
    public int GetGridLength() => gridLength;


    [Header("Blocks & Gates")]
    [SerializeField] private List<Block> placedBlocks = new List<Block>();
    [SerializeField] private List<Gate> gates = new List<Gate>();
    public List<Gate> GetGates() => gates;


    //* Grid Info Data Sets

    private Dictionary<Vector3, Vector2Int> _worldToGridMap;

    private Dictionary<Vector2Int, Gate> gateRegistry = new Dictionary<Vector2Int, Gate>();


    private Dictionary<Vector2Int, GameObject> wallRegistry = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, CellObject> cellRegistry = new Dictionary<Vector2Int, CellObject>();
    private bool[,] cellOccupied;

    private bool[,] cellIsWall;


    //* Events
    public UnityEvent onBlockRemoved = new UnityEvent();
    public UnityEvent onGridInitialized = new UnityEvent();
    public UnityEvent onBlockClicked = new UnityEvent();
    #endregion
    #region Essentials
    void Start()
    {
        if (savedGridData != null)
        {
            LoadGridData();
        }
        else
        {
            InitializeGridFromChildren();
        }

        //* Mark Cells Occupied For All Blocks
        foreach (var block in placedBlocks)
        {
            SetCellsOccupied(block.GridPosition, true); // Mark cells as occupied
        }


        DOVirtual.DelayedCall(0.1f, () =>
        {
            onGridInitialized.Invoke();
        });
    }
    void OnDestroy()
    {
        DOTween.Kill(this);
    }
    #endregion

    #region Grid Setup
    #region Grid Data Serialization

    // This will be called from your editor script
    public void SaveGridDataToAsset(string assetName)
    {
        string basePath = "Assets/_3D Block Puzzle/Gameplay/Scriptable Objects/GridData/";
        string assetPath = basePath + assetName + ".asset";
        bool assetExists = false;

        // Check if asset already exists
#if UNITY_EDITOR
        assetExists = UnityEditor.AssetDatabase.LoadAssetAtPath<GridData>(assetPath) != null;

        // If asset exists, ask for user decision
        if (assetExists)
        {
            bool shouldReplace = UnityEditor.EditorUtility.DisplayDialog(
                "Asset Already Exists",
                $"An asset with the name '{assetName}' already exists.\nDo you want to replace it or use a new name?",
                "Replace", "New Name");

            if (!shouldReplace) // User clicked "New Name"
            {
                string inputName = assetName + "_new";
                if (!UnityEditor.EditorUtility.DisplayDialog(
                    "New Asset Name",
                    "Do you want to save as '" + inputName + "'?",
                    "Yes", "Cancel"))
                {
                    Debug.Log("Save operation canceled.");
                    return; // User canceled the operation
                }

                // Recursively call this method with the new name
                SaveGridDataToAsset(inputName);
                return;
            }
            // If shouldReplace is true, we continue with replacing the existing asset
        }
#endif

        // Create a new grid data instance
        GridData gridData = ScriptableObject.CreateInstance<GridData>();
        gridData.Initialize(gridWidth, gridLength);
        gridData.cellSize = gridSpacing;
        gridData.gridStartPosition = gridStartPosition;

        // Save occupancy and wall data
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // Convert world Z to data Z
                int dataZ = WorldToDataZ(z);
                int index = gridData.GetIndex(x, z);
                gridData.gridData.occupiedCells[index] = cellOccupied[x, z];
                gridData.gridData.wallCells[index] = cellIsWall[x, z];
            }
        }

        // Save the asset
#if UNITY_EDITOR
        if (assetExists)
        {
            // Update existing asset
            GridData existingData = UnityEditor.AssetDatabase.LoadAssetAtPath<GridData>(assetPath);
            UnityEditor.EditorUtility.CopySerialized(gridData, existingData);
            UnityEditor.EditorUtility.SetDirty(existingData);
            Debug.Log($"Updated existing grid data at {assetPath}");
            savedGridData = existingData;
        }
        else
        {
            // Create new asset
            UnityEditor.AssetDatabase.CreateAsset(gridData, assetPath);
            Debug.Log($"Created new grid data at {assetPath}");
            savedGridData = gridData;
        }

        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    // In your LoadGridData method, add:
    public void LoadGridData()
    {
        if (savedGridData == null)
        {
            Debug.LogWarning("No grid data asset assigned. Falling back to runtime initialization.");
            InitializeGridFromChildren();
            return;
        }

        // Apply grid dimensions from saved data
        gridWidth = savedGridData.gridWidth;
        gridLength = savedGridData.gridLength;
        gridSpacing = savedGridData.cellSize;
        gridStartPosition = savedGridData.gridStartPosition;

        // Initialize arrays
        EnsureArraysInitialized();
        BuildWorldToGridLookup();
        BuildWallRegistry();
        BuildGateRegistry();

        // Load occupancy and wall data
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                int index = savedGridData.GetIndex(x, z);
                cellOccupied[x, z] = savedGridData.gridData.occupiedCells[index];
                cellIsWall[x, z] = savedGridData.gridData.wallCells[index];
            }
        }
    }

    #endregion

    public void ConfigureGridInEditor()
    {
        LoadGridData();
        InitializeBlocks();
    }
    private void EnsureArraysInitialized()
    {
        //* Ensuring Cell Occupied Array is Initialized
        if (cellOccupied == null || cellOccupied.GetLength(0) != gridWidth || cellOccupied.GetLength(1) != gridLength)
        {
            // Store old values if they exist
            bool[,] oldValues = cellOccupied;

            // Create new array with current dimensions
            cellOccupied = new bool[gridWidth, gridLength];

            // Copy old values to new array if old array exists
            if (oldValues != null)
            {
                int oldWidth = Mathf.Min(oldValues.GetLength(0), gridWidth);
                int oldLength = Mathf.Min(oldValues.GetLength(1), gridLength);

                for (int x = 0; x < oldWidth; x++)
                {
                    for (int z = 0; z < oldLength; z++)
                    {
                        cellOccupied[x, z] = oldValues[x, z];
                    }
                }
            }
        }

        //* Ensuring Cell Is Wall Array is Initialized
        if (cellIsWall == null || cellIsWall.GetLength(0) != gridWidth || cellIsWall.GetLength(1) != gridLength)
        {
            bool[,] oldWalls = cellIsWall;
            cellIsWall = new bool[gridWidth, gridLength];

            if (oldWalls != null)
            {
                int oldWidth = Mathf.Min(oldWalls.GetLength(0), gridWidth);
                int oldLength = Mathf.Min(oldWalls.GetLength(1), gridLength);

                for (int x = 0; x < oldWidth; x++)
                {
                    for (int z = 0; z < oldLength; z++)
                    {
                        cellIsWall[x, z] = oldWalls[x, z];
                    }
                }
            }
        }
    }
    public void MarkExteriorCellsAsOccupied(bool[,] interiorCells)
    {
        // Make sure arrays are initialized
        EnsureArraysInitialized();

        // Mark all non-interior cells as occupied
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // If the cell is not interior (outside the walls), mark it as occupied
                if (!interiorCells[x, z])
                {
                    // Convert the world Z position to data Z position
                    int dataZ = WorldToDataZ(z);

                    // Use the converted Z index when accessing the array
                    cellOccupied[x, z] = true;
                    cellIsWall[x, z] = true; // Also mark as a wall for consistency
                }
            }
        }

        Debug.Log("Exterior cells marked as occupied");
    }

    public void ClearBlocks()
    {
        // Clear the list of placed blocks
        placedBlocks.Clear();

        // Reset occupancy array while preserving wall information
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // Only clear occupancy for non-wall cells
                if (!cellIsWall[x, z])
                {
                    cellOccupied[x, z] = false;
                }
            }
        }
    }
    public List<Vector3> GetEdgePositions()
    {
        List<Vector3> edgePositions = new List<Vector3>();

        // Calculate expanded grid dimensions (add 1 unit of spacing to each side)
        float expandedOffset = gridSpacing;

        // Add expanded corner positions (1 grid unit outside the actual grid)
        // Bottom-left corner (expanded)
        edgePositions.Add(new Vector3(
            gridStartPosition.x - expandedOffset,
            gridStartPosition.y,
            gridStartPosition.z - expandedOffset
        ));

        // Bottom-right corner (expanded)
        edgePositions.Add(new Vector3(
            gridStartPosition.x + (gridWidth - 1) * gridSpacing + expandedOffset,
            gridStartPosition.y,
            gridStartPosition.z - expandedOffset
        ));

        // Top-left corner (expanded)
        edgePositions.Add(new Vector3(
            gridStartPosition.x - expandedOffset,
            gridStartPosition.y,
            gridStartPosition.z + (gridLength - 1) * gridSpacing + expandedOffset
        ));

        // Top-right corner (expanded)
        edgePositions.Add(new Vector3(
            gridStartPosition.x + (gridWidth - 1) * gridSpacing + expandedOffset,
            gridStartPosition.y,
            gridStartPosition.z + (gridLength - 1) * gridSpacing + expandedOffset
        ));

        // Add a point slightly above the center to help with camera angle
        Vector3 centerTop = new Vector3(
            gridStartPosition.x + (gridWidth / 2f) * gridSpacing,
            gridStartPosition.y + (gridSpacing * 2f), // Raised position above the grid
            gridStartPosition.z + (gridLength / 2f) * gridSpacing
        );
        edgePositions.Add(centerTop);

        return edgePositions;
    }
    public void UpdateGridProperties(int width, int length, float size)
    {
        gridWidth = width;
        gridLength = length;
        gridSpacing = size;

        // Reinitialize arrays with the new dimensions
        EnsureArraysInitialized();
    }

    /// <summary>
    /// Call this once after your grid has been created (and gridStartPosition + gridSpacing set).
    /// </summary>
    public void BuildWorldToGridLookup()
    {
        _worldToGridMap = new Dictionary<Vector3, Vector2Int>(gridWidth * gridLength);

        for (int x = 0; x < gridWidth; x++)
            for (int z = 0; z < gridLength; z++)
            {
                // compute the exact world‐space center of cell [x,z]
                Vector3 center = new Vector3(
                    gridStartPosition.x + x * gridSpacing,
                    gridStartPosition.y,
                    gridStartPosition.z + z * gridSpacing
                );

                _worldToGridMap[center] = new Vector2Int(x, z);
            }
    }
    public void BuildWallRegistry()
    {
        wallRegistry.Clear();
        HashSet<Vector2Int> processedPositions = new HashSet<Vector2Int>();

        GameObject[] allWalls = wallParent
            .Cast<Transform>()
            .Select(t => t.gameObject)
            .Where(g => g.transform.parent == wallParent)
            .ToArray();
        foreach (var wall in allWalls)
        {
            Vector3 worldPos = wall.transform.position;
            Vector2Int gridPos = wall.GetComponent<WallData>().wallGridPosition;

            // Only log if we haven't seen this position before
            if (!processedPositions.Contains(gridPos))
            {
                processedPositions.Add(gridPos);
            }

            // Last wall at this position wins
            wallRegistry[gridPos] = wall;
        }
    }

    // This should be called after all grid cells are created in the editor
    public void InitializeGridFromChildren()
    {
        // Initialize grid arrays with correct dimensions
        EnsureArraysInitialized();

        // Find all child objects with "Cell" in their name
        Transform[] children = GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            // Skip the parent (this) transform
            if (child == transform) continue;

            // Parse cell position from its name (assuming format "Cell_X_Z")
            if (child.name.StartsWith("Cell_"))
            {
                string[] parts = child.name.Split('_');
                if (parts.Length == 3 && parts[0] == "Cell")
                {
                    if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
                    {

                        int dataZ = WorldToDataZ(z); // Convert world Z to data Z
                        if (IsWithinGrid(x, z))
                        {
                            cellOccupied[x, z] = false; // Not occupied initially
                            cellIsWall[x, z] = false; // Not a wall
                        }
                    }
                }
            }
            // Check for wall cells (assuming format "Wall_Interior_X_Z")
            else if (child.name.StartsWith("Wall_Interior_"))
            {
                string[] parts = child.name.Split('_');
                if (parts.Length == 4 && parts[0] == "Wall" && parts[1] == "Interior")
                {
                    if (int.TryParse(parts[2], out int x) && int.TryParse(parts[3], out int z))
                    {
                        int dataZ = WorldToDataZ(z); // Convert world Z to data Z
                        if (IsWithinGrid(x, z))
                        {
                            cellIsWall[x, z] = true; // Mark as wall
                            cellOccupied[x, z] = true; // Mark as occupied
                        }
                    }
                }
            }
        }

        Debug.Log($"Grid initialized with dimensions {gridWidth}x{gridLength}");
    }
    //* Add a block to the grid
    public void AddBlock(Block block, Vector2Int[] gridPosition)
    {
        // Check if the block can be placed at the specified grid position
        if (!IsValidPlacement(block, gridPosition))
        {
            Debug.LogWarning("Invalid placement for block: " + block.name);
            return; // Invalid placement, exit the method
        }

        // Place the block and mark cells as occupied
        placedBlocks.Add(block);
        block.gridManager = this; // Set the grid manager reference for the bloc    k
        SetCellsOccupied(gridPosition, true); // Mark cells as occupied
        MoveBlockToPosition(block, gridPosition);

#if UNITY_EDITOR
        // First mark the GridManager component as modified
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        // Get the parent prefab instance (LVL_x)
        GameObject prefabRoot = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);

        if (prefabRoot != null)
        {
            // Mark the parent as dirty too
            UnityEditor.EditorUtility.SetDirty(prefabRoot);

            // Record modifications to the parent prefab instance
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(prefabRoot);
        }

        // Mark the scene as dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

        // Save prefab changes
        GameObject prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
        if (prefabAsset != null)
        {
            // Try to apply overrides
            UnityEditor.PrefabUtility.ApplyPrefabInstance(prefabRoot, UnityEditor.InteractionMode.AutomatedAction);
        }
#endif
        //?  block.OnBlockPlaced.AddListener(() => CheckGatePulls()); // Subscribe to block placement event
    }
    void InitializeBlocks()
    {
        var all = GetComponentsInChildren<Block>();
        Debug.Log($"Initializing {all.Length} blocks...");
        placedBlocks.Clear(); // Clear the list before adding new blocks

        foreach (var block in all)
        {
            // 1) Get the world position of the block's logical pivot
            Vector3 pivotWorld = block.transform.position - block.Shape.meshOffset;

            // 2) Convert to grid coordinates
            Vector2Int baseCell = WorldToGridPosition(pivotWorld);

            // 3) Get the rotation around Y axis (in degrees)
            float yRotation = block.transform.rotation.eulerAngles.y;

            // Round to nearest 90 degrees for sanity
            int rotationIndex = Mathf.RoundToInt(yRotation / 90f) % 4;


            // Get rotated positions based on rotation index
            List<Vector2Int> occ;
            switch (rotationIndex)
            {
                case 1: // 90 degrees
                    occ = block.Shape.GetRotatedPositions90();
                    break;
                case 2: // 180 degrees
                    occ = block.Shape.GetRotatedPositions180();
                    break;
                case 3: // 270 degrees
                    occ = block.Shape.GetRotatedPositions270();
                    break;
                default: // 0 degrees
                    occ = block.Shape.GetOccupiedPositions();
                    break;
            }

            // 4) Create grid positions array
            Vector2Int[] gridPositions = new Vector2Int[occ.Count];

            // 5) Each position is relative to the center position
            for (int i = 0; i < occ.Count; i++)
            {
                gridPositions[i] = baseCell + occ[i];
            }

            // 6) Visualize the cells to verify correctness
            foreach (var pos in gridPositions)
            {
                Vector3 worldPos = new Vector3(
                    gridStartPosition.x + pos.x * gridSpacing,
                    gridStartPosition.y,
                    gridStartPosition.z + pos.y * gridSpacing
                );
                Debug.DrawLine(worldPos, worldPos + Vector3.up * gridSpacing, Color.red, 5f);
            }

            // 7) Add the block to the grid
            AddBlock(block, gridPositions);
        }
    }

    #endregion

    #region Gate System  
    public void AssignVFXToAllGates()
    {

        // Track level prefab instances we've modified
        HashSet<GameObject> modifiedLevelPrefabs = new HashSet<GameObject>();

        foreach (var gate in gates)
        {
            // Load the VFX prefab based on the gate's color type
            string colorName = gate.colorType.ToString();

#if UNITY_EDITOR
            // Use AssetDatabase in editor
            string assetPath = $"Assets/_3D Block Puzzle/Gameplay/VFX/BlockCrush/Prefabs/BlocksGrinding_{colorName}.prefab";
            GameObject gateVFX = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (gateVFX != null)
            {
                // Record the change only for this specific gate object instance
                UnityEditor.Undo.RecordObject(gate.gateObject, "Assign VFX to Gate");

                // Assign the VFX
                gate.gateObject.exitVFX = gateVFX;

                // Mark the gate object as dirty
                UnityEditor.EditorUtility.SetDirty(gate.gateObject);

                Debug.Log($"Assigned VFX from AssetDatabase: '{assetPath}' to gate with color {colorName}");

                // Find the level prefab this gate belongs to (not the gate prefab)
                GameObject levelPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);

                if (levelPrefab != null && !modifiedLevelPrefabs.Contains(levelPrefab))
                {
                    // Add to our set of modified level prefabs
                    modifiedLevelPrefabs.Add(levelPrefab);

                    // Mark the level prefab dirty
                    UnityEditor.EditorUtility.SetDirty(levelPrefab);

                    // Record the change to the level prefab instance
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"Could not load VFX prefab at path: {assetPath}");
            }
#endif
        }

#if UNITY_EDITOR
        // Only apply level prefab changes once at the end for all unique level prefabs
        foreach (GameObject levelPrefab in modifiedLevelPrefabs)
        {
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(levelPrefab))
            {
                // Apply changes to the level prefab instance only
                UnityEditor.PrefabUtility.ApplyPrefabInstance(levelPrefab, UnityEditor.InteractionMode.UserAction);
                Debug.Log($"Applied changes to level prefab: {levelPrefab.name}");
            }
        }

        // Save the scene if any changes were made
        if (modifiedLevelPrefabs.Count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
    // Get the gate at a position (if any)
    public Gate GetGate(Vector2Int position)
    {
        if (gateRegistry.TryGetValue(position, out Gate gate))
        {
            return gate;
        }
        return null;
    }

    // Add a new gate to the grid
    public Gate AddGate(BlockColorTypes colorType, List<Vector2Int> positions, Gate.GateDirection pullDirection, GateObject gateObj)
    {
        //* Get The Gate Grid VFX
        GameObject gateVFX = Resources.Load<GameObject>($"_3D Block Puzzle/Gameplay/VFX/BlockCrush/BlocksGrinding_{colorType}");
        // Create a new gate
        Gate gate = new(
            colorType,
            positions,
            pullDirection,
            gateObj,
            gateVFX
        );
        gates.Add(gate);
        gateObj.gatePosition = positions.ToArray();
        // Add to registry for quick lookups
        foreach (Vector2Int pos in positions)
        {
            gateRegistry[pos] = gate;
        }

        return gate;
    }

    // Remove a gate from the grid
    public void RemoveGate(Gate gate)
    {
        if (gate == null) return;

        // Get default wall material
        Material defaultWallMaterial = GameConstants.GetDefaultWallMaterial();

        // Restore original wall materials
        foreach (Vector2Int pos in gate.positions)
        {
            if (wallRegistry.TryGetValue(pos, out GameObject wallObj))
            {
                Renderer renderer = wallObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Apply default wall material
                    renderer.material = defaultWallMaterial;
                }
            }
        }
        // Remove from registry
        foreach (Vector2Int pos in gate.positions)
        {
            gateRegistry.Remove(pos);
        }

        // Remove from list
        gates.Remove(gate);
    }
    void BuildGateRegistry()
    {
        gateRegistry.Clear();
        HashSet<Vector2Int> processedPositions = new HashSet<Vector2Int>();

        foreach (var gate in gates)
        {
            foreach (var pos in gate.positions)
            {
                // Only log if we haven't seen this position before
                if (!processedPositions.Contains(pos))
                {
                    processedPositions.Add(pos);
                }

                // Last gate at this position wins
                gateRegistry[pos] = gate;
            }
        }
    }
    #region Check Gate Pulls
    //* Check if any blocks need to be pulled by gates
    public void CheckGatePulls()
    {
        if (GameManager.Instance.currentLevelState != LevelState.InProgress) return;
        foreach (Block block in placedBlocks.ToArray()) // Use ToArray to avoid collection modification issues
        {
            // Check each position of the block
            foreach (Vector2Int blockPos in block.GridPosition)
            {
                // Check adjacent cells for gates
                CheckAdjacentCellsForGates(block, blockPos, block.BlockColor);
            }
        }
    }

    // Check cells adjacent to a block position for gates
    private void CheckAdjacentCellsForGates(Block block, Vector2Int blockPos, BlockColorTypes blockColorType)
    {
        DebugLogger.Log($"Checking adjacent cells for block {block.name}", DebugColor.Cyan);
        // Check in all four directions
        CheckDirectionForGate(block, blockPos, new Vector2Int(0, 1), blockColorType); // North
        CheckDirectionForGate(block, blockPos, new Vector2Int(0, -1), blockColorType); // South
        CheckDirectionForGate(block, blockPos, new Vector2Int(1, 0), blockColorType); // East
        CheckDirectionForGate(block, blockPos, new Vector2Int(-1, 0), blockColorType); // West
    }

    // Check a specific direction for a gate
    private void CheckDirectionForGate(Block block, Vector2Int blockPos, Vector2Int direction, BlockColorTypes blockColorType)
    {
        Vector2Int checkPos = blockPos + direction;

        if (!IsWithinGrid(checkPos.x, checkPos.y)) return;

        Gate gate = GetGate(checkPos);
        if (gate != null)
        {
            // Check if colors match
            if (gate.colorType == blockColorType)
            {
                // Check if gate direction is opposite to the check direction
                bool shouldPull = false;

                switch (gate.pullDirection)
                {
                    case Gate.GateDirection.North:
                        shouldPull = direction.y == 1;
                        break;
                    case Gate.GateDirection.South:
                        shouldPull = direction.y == -1;
                        break;
                    case Gate.GateDirection.East:
                        shouldPull = direction.x == 1;
                        break;
                    case Gate.GateDirection.West:
                        shouldPull = direction.x == -1;
                        break;
                }

                if (shouldPull)
                {
                    if (CanBlockFitThroughGate(block, gate))
                    {
                        // Check if the block can fit through the gate and if the path is clear
                        if (IsPathClearThroughGate(block, gate))
                        {
                            PullBlockToGate(block, gate);
                            return; // Exit after pulling the block
                        }
                        else
                        {
                            DebugLogger.Log($"Path blocked for block {block.name}", DebugColor.Red);
                        }
                    }
                    else
                    {
                        DebugLogger.Log($"Block {block.name} cannot fit through gate", DebugColor.Red);
                    }
                }
            }
        }
    }

    private bool CanBlockFitThroughGate(Block block, Gate gate)
    {
        // Use the current grid positions of the block
        Vector2Int[] blockPositions = block.GridPosition;
        List<Vector2Int> gatePositions = gate.positions;

        // Calculate block dimensions and position
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (Vector2Int pos in blockPositions)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // Find gate boundaries
        int minGateX = int.MaxValue, maxGateX = int.MinValue;
        int minGateY = int.MaxValue, maxGateY = int.MinValue;

        foreach (Vector2Int pos in gatePositions)
        {
            minGateX = Mathf.Min(minGateX, pos.x);
            maxGateX = Mathf.Max(maxGateX, pos.x);
            minGateY = Mathf.Min(minGateY, pos.y);
            maxGateY = Mathf.Max(maxGateY, pos.y);
        }

        // Create sets for faster lookup
        HashSet<int> gateXPositions = new HashSet<int>();
        HashSet<int> gateYPositions = new HashSet<int>();

        foreach (Vector2Int pos in gatePositions)
        {
            gateXPositions.Add(pos.x);
            gateYPositions.Add(pos.y);
        }

        // Create a 2D grid representing the block's shape
        bool[,] blockShape = new bool[maxX - minX + 1, maxY - minY + 1];

        // Fill in the block shape
        foreach (Vector2Int pos in blockPositions)
        {
            blockShape[pos.x - minX, pos.y - minY] = true;
        }

        switch (gate.pullDirection)
        {
            case Gate.GateDirection.North:
            case Gate.GateDirection.South:
                // For north/south gates, check if each column of the block has a matching gate cell
                for (int x = minX; x <= maxX; x++)
                {
                    bool columnHasBlock = false;

                    // Check if this column has any block cells
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (blockShape[x - minX, y - minY])
                        {
                            columnHasBlock = true;
                            break;
                        }
                    }

                    // If this column has a block cell but no matching gate cell, the block can't fit
                    if (columnHasBlock && !gateXPositions.Contains(x))
                    {
                        DebugLogger.Log($"Block column at x={x} has no matching gate position", DebugColor.Red);
                        return false;
                    }
                }
                return true;

            case Gate.GateDirection.East:
            case Gate.GateDirection.West:
                // For east/west gates, check if each row of the block has a matching gate cell
                for (int y = minY; y <= maxY; y++)
                {
                    bool rowHasBlock = false;

                    // Check if this row has any block cells
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (blockShape[x - minX, y - minY])
                        {
                            rowHasBlock = true;
                            break;
                        }
                    }

                    // If this row has a block cell but no matching gate cell, the block can't fit
                    if (rowHasBlock && !gateYPositions.Contains(y))
                    {
                        DebugLogger.Log($"Block row at y={y} has no matching gate position", DebugColor.Red);
                        return false;
                    }
                }
                return true;
        }

        return false;
    }
    private bool IsPathClearThroughGate(Block block, Gate gate)
    {
        // Get the direction vector based on gate pull direction
        Vector2Int direction = Vector2Int.zero;
        switch (gate.pullDirection)
        {
            case Gate.GateDirection.North:
                direction = new Vector2Int(0, 1);
                break;
            case Gate.GateDirection.South:
                direction = new Vector2Int(0, -1);
                break;
            case Gate.GateDirection.East:
                direction = new Vector2Int(1, 0);
                break;
            case Gate.GateDirection.West:
                direction = new Vector2Int(-1, 0);
                break;
        }

        // Create a HashSet of all gate positions for fast lookups
        HashSet<Vector2Int> gatePositions = new HashSet<Vector2Int>(gate.positions);

        // Create a HashSet of all current block positions for fast lookups
        HashSet<Vector2Int> blockPositions = new HashSet<Vector2Int>(block.GridPosition);

        // Calculate the positions the block will occupy when pulled through
        HashSet<Vector2Int> destinationPositions = new HashSet<Vector2Int>();

        // Calculate how far to move the block
        int movementDistance = 2; // Move 2 cells through the gate

        // For each position in the block, calculate its destination position
        foreach (Vector2Int blockPos in block.GridPosition)
        {
            Vector2Int destPos = blockPos + (direction * movementDistance);
            destinationPositions.Add(destPos);

            // Also check the intermediate position (1 cell into the gate)
            Vector2Int intermediatePos = blockPos + direction;
            if (!gatePositions.Contains(intermediatePos) && !blockPositions.Contains(intermediatePos))
            {
                // If this intermediate position isn't the gate itself or part of the block,
                // we need to make sure it's clear too
                if (!IsWithinGrid(intermediatePos.x, intermediatePos.y) || IsCellOccupied(intermediatePos.x, intermediatePos.y))
                {
                    Debug.Log($"Path blocked at intermediate position {intermediatePos}");
                    return false;
                }
            }
        }
        return true; // Path is clear
    }
    private void PullBlockToGate(Block block, Gate gate)
    {
        // Store block's current position before removal
        Vector3 blockStartPos = block.transform.position;

        // Remove block from the GRID TRACKING but keep in placedBlocks list until animation completes
        Vector2Int[] positions = block.GridPosition;
        SetCellsOccupied(positions, false);

        // Direction vectors
        Vector3 pullDirection = Vector3.zero;
        switch (gate.pullDirection)
        {
            case Gate.GateDirection.North:
                pullDirection = Vector3.forward; // +Z
                break;
            case Gate.GateDirection.South:
                pullDirection = Vector3.back;    // -Z
                break;
            case Gate.GateDirection.East:
                pullDirection = Vector3.right;   // +X
                break;
            case Gate.GateDirection.West:
                pullDirection = Vector3.left;    // -X
                break;
        }

        // Calculate exit point - directly in the pull direction
        Vector3 exitPoint = blockStartPos + (pullDirection * gridSpacing * 5f);

        // Find which gate cells the block will pass through
        List<Vector2Int> exitCells = new List<Vector2Int>();
        HashSet<Vector2Int> blockCellsSet = new HashSet<Vector2Int>(block.GridPosition);

        // Determine pull direction as Vector2Int for comparing block and gate cells
        Vector2Int pullDir2D = Vector2Int.zero;
        switch (gate.pullDirection)
        {
            case Gate.GateDirection.North: pullDir2D = new Vector2Int(0, 1); break;
            case Gate.GateDirection.South: pullDir2D = new Vector2Int(0, -1); break;
            case Gate.GateDirection.East: pullDir2D = new Vector2Int(1, 0); break;
            case Gate.GateDirection.West: pullDir2D = new Vector2Int(-1, 0); break;
        }

        // Find gate cells that align with block cells (in the direction perpendicular to pull)
        foreach (Vector2Int gatePos in gate.positions)
        {
            foreach (Vector2Int blockPos in block.GridPosition)
            {
                if (pullDir2D.x != 0) // East/West gate
                {
                    // Check if block and gate are aligned on the Y axis
                    if (blockPos.y == gatePos.y &&
                        ((pullDir2D.x > 0 && blockPos.x + 1 == gatePos.x) ||
                         (pullDir2D.x < 0 && blockPos.x - 1 == gatePos.x)))
                    {
                        exitCells.Add(gatePos);
                        break;
                    }
                }
                else // North/South gate
                {
                    // Check if block and gate are aligned on the X axis
                    if (blockPos.x == gatePos.x &&
                        ((pullDir2D.y > 0 && blockPos.y + 1 == gatePos.y) ||
                         (pullDir2D.y < 0 && blockPos.y - 1 == gatePos.y)))
                    {
                        exitCells.Add(gatePos);
                        break;
                    }
                }
            }
        }

        gate.gateObject.MoveDown(); // Move the gate down

        DOVirtual.DelayedCall(0.2f, () =>
        {
            //* Play exit VFX at each exit cell (will happen as the block passes through)
            foreach (Vector2Int exitCell in exitCells)
            {
                // Convert grid position to world position
                Vector3 exitPos = GridToWorldPosition(exitCell);

                // Determine the correct rotation based on gate direction
                Quaternion vfxRotation = Quaternion.identity;
                switch (gate.pullDirection)
                {
                    case Gate.GateDirection.North:
                        vfxRotation = Quaternion.Euler(0, 0, 0);  // Forward-facing
                        break;
                    case Gate.GateDirection.South:
                        vfxRotation = Quaternion.Euler(0, 180, 0); // Backward-facing
                        break;
                    case Gate.GateDirection.East:
                        vfxRotation = Quaternion.Euler(0, 90, 0);  // Right-facing
                        break;
                    case Gate.GateDirection.West:
                        vfxRotation = Quaternion.Euler(0, 270, 0); // Left-facing
                        break;
                }

                //* Rotate VFX to face Backwards From Gate
                vfxRotation = Quaternion.Euler(-90, vfxRotation.eulerAngles.y, vfxRotation.eulerAngles.z);

                //* Instantiate VFX at the gate position with the correct rotation
                if (gate.gateObject.exitVFX != null)
                {
                    GameObject vfxInstance = Instantiate(gate.gateObject.exitVFX, exitPos, vfxRotation);

                    // Optionally control lifetime of VFX
                    Destroy(vfxInstance, 2f);
                }
            }

            //* Play SFX
            AudioManager.Instance.PlayBlockRemove();

            //* Configure The Block Material So It Knows Where to Dissolve From
            block.ConfigureMaterialToDissappear(gate.pullDirection, gate.gateObject.gameObject.transform.position);

            //* Move the block to the exit point
            block.transform.DOMove(exitPoint, 0.6f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Only remove and trigger event if not already done
                    if (placedBlocks.Contains(block))
                    {
                        placedBlocks.Remove(block);
                        onBlockRemoved.Invoke(); // Notify that a block has been removed
                        Destroy(block.gameObject);
                        //* Play Feel
                        Vibrations.Haptic(HapticTypes.Success);
                        gate.gateObject.MoveUp(); // Move the gate back up
                    }
                });
        });
    }
    #endregion


    // Save gate data to GridData
    public void SaveGateDataToGridData()
    {
        // Skip if no gates
        if (gates == null || gates.Count == 0)
        {
            Debug.Log("No gates to save");
            return;
        }

        // Create new gate data
        GateData gateData = new GateData();

        // Initialize arrays with correct sizes
        gateData.gateColorTypes = new int[gates.Count];
        gateData.gateDirections = new int[gates.Count];
        gateData.gatePositionCounts = new int[gates.Count];

        // Calculate total positions
        int totalPositions = 0;
        foreach (Gate gate in gates)
        {
            totalPositions += gate.positions.Count;
        }

        gateData.gatePositionsX = new int[totalPositions];
        gateData.gatePositionsZ = new int[totalPositions];

        // Fill arrays with gate data
        int posIndex = 0;
        for (int i = 0; i < gates.Count; i++)
        {
            Gate gate = gates[i];

            // Store color as enum index
            gateData.gateColorTypes[i] = (int)gate.colorType;
            gateData.gateDirections[i] = (int)gate.pullDirection;
            gateData.gatePositionCounts[i] = gate.positions.Count;

            // Store positions
            for (int j = 0; j < gate.positions.Count; j++)
            {
                gateData.gatePositionsX[posIndex] = gate.positions[j].x;
                gateData.gatePositionsZ[posIndex] = gate.positions[j].y;
                posIndex++;
            }
        }

        // Assign to grid data
        savedGridData.gateData = gateData;
    }

    #endregion
    #region Cell Occupancy
    // Set cell occupancy state
    void SetCellsOccupied(Vector2Int[] gridPos, bool occupied)
    {

        foreach (Vector2Int pos in gridPos)
        {
            if (IsWithinGrid(pos.x, pos.y))
            {
                cellOccupied[pos.x, pos.y] = occupied;
            }
        }
    }

    public bool IsCellOccupied(int x, int z)
    {
        if (IsWithinGrid(x, z))
        {

            return cellOccupied[x, z];
        }
        return false;
    }
    private int WorldToDataZ(int worldZ)
    {
        return gridLength - 1 - worldZ;
    }

    #endregion
    #region Block
    /// <summary>
    /// Notifies the grid manager that a block has been moved to a new position.
    /// This method should be called by the Block class when a block is moved.
    /// </summary>
    /// <param name="block"></param>
    /// <param name="newGridPosition"></param>
    /// <param name="oldGridPosition"></param>
    public void PlaceBlock(Block block, Vector2Int[] newGridPosition, Vector2Int[] oldGridPosition)
    {
        // Check if the block can be placed at the specified grid position
        if (IsValidPlacement(block, newGridPosition))
        {
            SetCellsOccupied(oldGridPosition, false); // Mark old cells as unoccupied
            SetCellsOccupied(newGridPosition, true); // Mark new cells as occupied
        }
        else
        {
            Debug.LogWarning("Invalid placement for block: " + block.name);
        }
    }
    #endregion
    #region Utility Methods
    public bool IsValidPlacement(Block block, Vector2Int[] gridPosition)
    {
        // Create a copy of the cell occupancy array to work with
        bool[,] cellOccupiedCopy = new bool[gridWidth, gridLength];

        // Copy the current state
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                cellOccupiedCopy[x, z] = cellOccupied[x, z];
            }
        }

        // Temporarily unoccupy the block's current position in our COPY
        if (block != null && block.GridPosition != null)
        {
            foreach (Vector2Int pos in block.GridPosition)
            {
                if (IsWithinGrid(pos.x, pos.y))
                {
                    cellOccupiedCopy[pos.x, pos.y] = false;
                }
            }
        }

        // Check if the block can be placed at the specified grid positions
        foreach (Vector2Int pos in gridPosition)
        {
            bool withinGrid = IsWithinGrid(pos.x, pos.y);
            bool isOccupied = false;
            bool isWall = false;

            if (withinGrid)
            {
                isOccupied = cellOccupiedCopy[pos.x, pos.y]; // Use our copy
                isWall = cellIsWall[pos.x, pos.y];
            }

            if (!withinGrid || isOccupied || isWall)
            {
                return false; // Invalid placement
            }
        }

        return true; // Valid placement
    }

    // … inside GridManager …
    void MoveBlockToPosition(Block block, Vector2Int[] gridPositions)
    {
        if (gridPositions == null || gridPositions.Length == 0)
            return;

        // Find which entry in gridPositions corresponds to the pivot (relative offset == (0,0))
        var offsets = block.Shape.GetOccupiedPositions();
        int pivotIndex = 0;
        for (int i = 0; i < offsets.Count && i < gridPositions.Length; i++)
        {
            if (offsets[i] == Vector2Int.zero)
            {
                pivotIndex = i;
                break;
            }
        }

        Vector2Int pivotGrid = gridPositions[pivotIndex];
        Vector3 targetWorld = new Vector3(
            gridStartPosition.x + pivotGrid.x * gridSpacing,
            block.YPos,
            gridStartPosition.z + pivotGrid.y * gridSpacing
        );

        block.SetGridPosition(gridPositions);
        block.transform.position = targetWorld;

    }

    public bool IsCellWall(int x, int z)
    {
        // First check if the cell is within grid bounds
        if (!IsWithinGrid(x, z))
            return false; // Out of bounds cells are not walls

        // Primary check: Look in the wall registry
        if (wallRegistry.ContainsKey(new Vector2Int(x, z)))
            return true;

        return cellIsWall[x, z];
    }
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridStartPosition.x + gridPos.x * gridSpacing,
            gridStartPosition.y,
            gridStartPosition.z + gridPos.y * gridSpacing
        );
    }
    /// <summary>
    /// Converts a world‐space point into grid‐cell coordinates (0…gridWidth‑1, 0…gridLength‑1).
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        // 1) project your query onto the same horizontal plane
        Vector3 query = new Vector3(worldPos.x, gridStartPosition.y, worldPos.z);

        // 2) try exact hit:
        if (_worldToGridMap.TryGetValue(query, out Vector2Int hit))
            return hit;

        // 3) else brute‐force nearest (still cheap for a 10×10 grid):
        float bestDist = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;
        foreach (var kv in _worldToGridMap)
        {
            float d = (kv.Key - query).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestCell = kv.Value;
            }
        }
        return bestCell;
    }
    public int GetBlockCount()
    {
        return placedBlocks.Count;
    }

    /// <summary>
    /// Batch version.
    /// </summary>
    public Vector2Int[] WorldToGridPositions(Vector3[] worldPositions)
    {
        var result = new Vector2Int[worldPositions.Length];
        for (int i = 0; i < worldPositions.Length; i++)
            result[i] = WorldToGridPosition(worldPositions[i]);
        return result;
    }
    // Add this method to your GridManager class
    public float GetCellSize()
    {
        return gridSpacing;
    }
    // Check if the given grid coordinates are within bounds
    public bool IsWithinGrid(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridLength;
    }
    public List<Vector2Int> GetWallPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        // Check if wallRegistry is initialized
        if (wallRegistry != null && wallRegistry.Count > 0)
        {
            foreach (var wallPos in wallRegistry.Keys)
            {
                positions.Add(wallPos);
            }
        }
        else
        {
            // Fallback to checking cellIsWall array
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridLength; z++)
                {
                    if (cellIsWall != null && cellIsWall[x, z])
                    {
                        positions.Add(new Vector2Int(x, z));
                    }
                }
            }
        }

        return positions;
    }
  #endregion
}
