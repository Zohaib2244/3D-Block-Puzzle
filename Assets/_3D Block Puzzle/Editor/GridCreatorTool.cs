using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class GridCreatorTool : EditorWindow
{
    #region Variables
    private string gridName = "New Grid";
    private GameObject gridParent;
    // Custom Mesh Prefabs
    private GameObject gridCellMeshPrefab;
    private GameObject straightWallMeshPrefab;
    private GameObject straightWallEndMeshPrefab;
    private GameObject cornerWallMeshPrefab;
    private Material gridCellMaterial_1;
    private Material gridCellMaterial_2;


    private float wallHeight = 0.17f;
    private float straightWallThickness = 0.35f; // Width of straight walls
    float spacing = 0.57f;
    float wallOffset = 0.4f; // Distance from cell edge to wall

    private int gridWidth = 10;
    private int gridLength = 10;


    // Grid editing variables
    private bool[,] gridWalls; // True if cell should be a wall
    private bool showGridEditor = false;
    private Vector2 scrollPosition;
    private Color normalCellColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Green
    private Color wallCellColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
    private Color hoveredCellColor = new Color(0.3f, 0.7f, 1f, 1f);
    private Color borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private int? hoveredCellX = null;
    private int? hoveredCellZ = null;
    private EditorWindow currentWindow;

    // Default search paths
    private const string DEFAULT_PREFABS_PATH = "Assets/_3D Block Puzzle/Gameplay/Prefabs/GridElements";
    private const string CELL_PREFAB_NAME = "GridCell";
    private const string WALL_PREFAB_NAME = "StraightWall";
    private const string WALL_END_PREFAB_NAME = "WallEnd";
    private const string CORNER_PREFAB_NAME = "CornerWall";
    #endregion

    #region Essentials
    [MenuItem("Block Puzzle/Grid Creator")]
    public static void ShowWindow()
    {
        GetWindow<GridCreatorTool>("Grid Creator");
    }

    private void OnEnable()
    {
        currentWindow = this;
        InitializeGridWalls(gridWidth, gridLength);
        AutoDetectPrefabs();
    }


    private void OnGUI()
    {
        GUILayout.Label("Grid Creation Settings", EditorStyles.boldLabel);

        // Grid parameter inputs
        gridName = EditorGUILayout.TextField("Grid Name", gridName);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Mesh Settings", EditorStyles.boldLabel);

        gridCellMeshPrefab = EditorGUILayout.ObjectField("Cell Mesh Prefab", gridCellMeshPrefab,
            typeof(GameObject), false) as GameObject;
        straightWallMeshPrefab = EditorGUILayout.ObjectField("Straight Wall Prefab", straightWallMeshPrefab,
            typeof(GameObject), false) as GameObject;
        straightWallEndMeshPrefab = EditorGUILayout.ObjectField("Straight Wall End Prefab", straightWallEndMeshPrefab,
            typeof(GameObject), false) as GameObject;
        cornerWallMeshPrefab = EditorGUILayout.ObjectField("Corner Wall Prefab", cornerWallMeshPrefab,
            typeof(GameObject), false) as GameObject;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);
        gridCellMaterial_1 = EditorGUILayout.ObjectField("Grid Cell Material 1", gridCellMaterial_1,
            typeof(Material), false) as Material;
        gridCellMaterial_2 = EditorGUILayout.ObjectField("Grid Cell Material 2", gridCellMaterial_2,
            typeof(Material), false) as Material;
        EditorGUILayout.Space();

        // Size settings
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        straightWallThickness = EditorGUILayout.FloatField("Straight Wall Thickness", straightWallThickness);
        EditorGUILayout.Space();

        spacing = EditorGUILayout.FloatField("Spacing", spacing);
        wallOffset = EditorGUILayout.FloatField("Wall Offset", wallOffset);
        EditorGUILayout.Space();
        // Grid size fields with update handling
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Grid Width", gridWidth);
        int newLength = EditorGUILayout.IntField("Grid Length", gridLength);
        if (EditorGUI.EndChangeCheck())
        {
            // Only update if values actually changed
            if (newWidth != gridWidth || newLength != gridLength)
            {
                gridWidth = Mathf.Max(1, newWidth);
                gridLength = Mathf.Max(1, newLength);
                InitializeGridWalls(gridWidth, gridLength);
            }
        }

        EditorGUILayout.Space();

        // Grid Editor Button
        if (GUILayout.Button("Edit Grid Layout", GUILayout.Height(30)))
        {
            showGridEditor = true;
        }

        // Grid Editor
        if (showGridEditor)
        {
            DrawGridEditor();
        }

        EditorGUILayout.Space();

        // Create Grid Button
        if (GUILayout.Button("Create Custom Grid", GUILayout.Height(40)))
        {
            CreateCustomGrid();
        }
    }
    #endregion
    #region Prefab Detection
    // Auto-detect mesh prefabs from the project
    private void AutoDetectPrefabs()
    {
        if (gridCellMeshPrefab == null)
            gridCellMeshPrefab = FindPrefabByName(CELL_PREFAB_NAME);

        if (straightWallMeshPrefab == null)
            straightWallMeshPrefab = FindPrefabByName(WALL_PREFAB_NAME);

        if (straightWallEndMeshPrefab == null)
            straightWallEndMeshPrefab = FindPrefabByName(WALL_END_PREFAB_NAME);

        if (cornerWallMeshPrefab == null)
            cornerWallMeshPrefab = FindPrefabByName(CORNER_PREFAB_NAME);

        if (gridCellMaterial_1 == null)
            gridCellMaterial_1 = FindMaterialByName("GridCellMaterial_1");

        if (gridCellMaterial_2 == null)
            gridCellMaterial_2 = FindMaterialByName("GridCellMaterial_2");
    }

    // Find prefab by name in the project
    private GameObject FindPrefabByName(string prefabName)
    {
        // First try the default location
        string defaultPath = Path.Combine(DEFAULT_PREFABS_PATH, prefabName + ".prefab");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(defaultPath);

        if (prefab != null)
            return prefab;

        return null;
    }
    private Material FindMaterialByName(string materialName)
    {
        // First try the default location
        string defaultPath = Path.Combine(DEFAULT_PREFABS_PATH, materialName + ".mat");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(defaultPath);

        if (material != null)
            return material;

        return null;
    }

    #endregion

    #region Inspector Grid


    private void DrawGridEditor()
    {
        GUILayout.Label("Grid Layout Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click cells to toggle between playable cells and walls. || This Grid is Flipped in Y Axis Keep That In Mind While Drawing Your Grid", MessageType.Info);

        // Editor controls
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("All Playable", GUILayout.Width(100)))
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 0; z < gridLength; z++)
                    gridWalls[x, z] = false;
            Repaint();
        }

        if (GUILayout.Button("All Walls", GUILayout.Width(100)))
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 0; z < gridLength; z++)
                    gridWalls[x, z] = true;
            Repaint();
        }

        if (GUILayout.Button("Outer Walls Only", GUILayout.Width(120)))
        {
            // First make all cells playable
            for (int x = 0; x < gridWidth; x++)
                for (int z = 0; z < gridLength; z++)
                    gridWalls[x, z] = false;

            // Then add outer walls
            for (int x = 0; x < gridWidth; x++)
            {
                gridWalls[x, 0] = true; // Bottom wall
                gridWalls[x, gridLength - 1] = true; // Top wall
            }

            for (int z = 0; z < gridLength; z++)
            {
                gridWalls[0, z] = true; // Left wall
                gridWalls[gridWidth - 1, z] = true; // Right wall
            }
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        // Calculate cell size based on available width
        float availableWidth = EditorGUIUtility.currentViewWidth - 40;
        float cellWidth = Mathf.Min(30, availableWidth / gridWidth);

        // Begin a scroll view for larger grids
        float gridVisualHeight = cellWidth * gridLength + 20;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                            GUILayout.Height(Mathf.Min(gridVisualHeight, 400)));

        // Calculate total grid visualization size
        float totalWidth = cellWidth * gridWidth;
        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, gridVisualHeight);

        // Detect mouse position relative to the grid
        Event e = Event.current;
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDown)
        {
            Vector2 mousePos = e.mousePosition;
            if (mousePos.x >= gridRect.x && mousePos.x < gridRect.x + totalWidth &&
                mousePos.y >= gridRect.y && mousePos.y < gridRect.y + cellWidth * gridLength)
            {
                // Calculate grid coordinates
                int newHoverX = Mathf.FloorToInt((mousePos.x - gridRect.x) / cellWidth);
                int newHoverZ = Mathf.FloorToInt((mousePos.y - gridRect.y) / cellWidth);

                // Ensure we're within the grid bounds
                if (newHoverX >= 0 && newHoverX < gridWidth && newHoverZ >= 0 && newHoverZ < gridLength)
                {
                    hoveredCellX = newHoverX;
                    hoveredCellZ = newHoverZ;
                    Repaint();
                }
                else
                {
                    hoveredCellX = null;
                    hoveredCellZ = null;
                }
            }
            else
            {
                hoveredCellX = null;
                hoveredCellZ = null;
            }
        }

        // Process mouse clicks for toggling cells
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (hoveredCellX.HasValue && hoveredCellZ.HasValue)
            {
                // Toggle cell state
                int x = hoveredCellX.Value;
                int z = hoveredCellZ.Value;
                gridWalls[x, z] = !gridWalls[x, z];

                // Use the Event
                e.Use();
                Repaint();
            }
        }

        // Draw the grid cells
        Handles.BeginGUI();

        // Draw column labels (X axis)
        for (int x = 0; x < gridWidth; x++)
        {
            Rect labelRect = new Rect(gridRect.x + x * cellWidth, gridRect.y - 15, cellWidth, 15);
            GUI.Label(labelRect, x.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }

        // Draw row labels (Z axis)
        for (int z = 0; z < gridLength; z++)
        {
            Rect labelRect = new Rect(gridRect.x - 20, gridRect.y + z * cellWidth, 20, cellWidth);
            GUI.Label(labelRect, z.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight });
        }

        // Draw each cell
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + x * cellWidth,
                    gridRect.y + z * cellWidth,
                    cellWidth,
                    cellWidth
                );

                // Determine the cell color
                Color cellColor;
                bool isHovered = hoveredCellX == x && hoveredCellZ == z;

                if (isHovered)
                {
                    cellColor = hoveredCellColor;
                }
                else if (gridWalls[x, z])
                {
                    cellColor = wallCellColor;
                }
                else
                {
                    cellColor = normalCellColor;
                }

                // Draw the cell
                EditorGUI.DrawRect(cellRect, cellColor);

                // Draw wall indicator
                if (gridWalls[x, z])
                {
                    GUI.Label(cellRect, "W", new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white },
                        fontStyle = FontStyle.Bold
                    });
                }

                // Draw cell border
                Handles.color = borderColor;
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y));
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x, cellRect.y + cellRect.height));
                Handles.DrawLine(new Vector3(cellRect.x + cellRect.width, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y + cellRect.height), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));
            }
        }

        Handles.EndGUI();

        EditorGUILayout.EndScrollView();

        // Button to close grid editor
        if (GUILayout.Button("Done Editing", GUILayout.Height(30)))
        {
            showGridEditor = false;
        }
    }

    private void FindInteriorCells(bool[,] interiorCells)
    {
        // First, mark all non-wall cells as potentially interior
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                interiorCells[x, z] = !gridWalls[x, z];
            }
        }

        // Use flood fill from the edges to identify exterior cells
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Add edge cells to queue
        for (int x = 0; x < gridWidth; x++)
        {
            // Top and bottom edges
            if (!gridWalls[x, 0])
            {
                interiorCells[x, 0] = false; // Mark as exterior
                queue.Enqueue(new Vector2Int(x, 0));
            }
            if (!gridWalls[x, gridLength - 1])
            {
                interiorCells[x, gridLength - 1] = false; // Mark as exterior
                queue.Enqueue(new Vector2Int(x, gridLength - 1));
            }
        }

        for (int z = 0; z < gridLength; z++)
        {
            // Left and right edges
            if (!gridWalls[0, z])
            {
                interiorCells[0, z] = false; // Mark as exterior
                queue.Enqueue(new Vector2Int(0, z));
            }
            if (!gridWalls[gridWidth - 1, z])
            {
                interiorCells[gridWidth - 1, z] = false; // Mark as exterior
                queue.Enqueue(new Vector2Int(gridWidth - 1, z));
            }
        }

        // Spread from edges inward to mark all accessible cells as exterior
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Check adjacent cells (4-directional)
            CheckAndMarkExterior(current.x + 1, current.y, queue, interiorCells);
            CheckAndMarkExterior(current.x - 1, current.y, queue, interiorCells);
            CheckAndMarkExterior(current.x, current.y + 1, queue, interiorCells);
            CheckAndMarkExterior(current.x, current.y - 1, queue, interiorCells);
        }

        // After this, only cells that are BOTH:
        // 1. Not walls AND
        // 2. Not reachable from the outside
        // will be marked as interior (true)
    }

    private void CheckAndMarkExterior(int x, int z, Queue<Vector2Int> queue, bool[,] interiorCells)
    {
        // Check bounds
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridLength)
            return;

        // Skip if already marked as exterior (false) or if it's a wall
        if (!interiorCells[x, z] || gridWalls[x, z])
            return;

        // Mark as exterior and add to queue
        interiorCells[x, z] = false;
        queue.Enqueue(new Vector2Int(x, z));
    }
    #endregion
    #region Grid Creation
    private void CreateCustomGrid()
    {
        if (gridParent == null)
        {
            gridParent = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridParent, "Create Grid Parent");
        }

        // Ensure parent has GridManager component
        GridManager gridManager = gridParent.GetComponent<GridManager>();
        if (gridManager == null)
        {
            gridManager = Undo.AddComponent<GridManager>(gridParent);
        }

        // Update GridManager properties with cell size
        gridManager.UpdateGridProperties(gridWidth, gridLength, spacing);

        // Delete existing children
        int childCount = gridParent.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(gridParent.transform.GetChild(i).gameObject);
        }

        // Create containers
        GameObject gridCellsContainer = new GameObject("GridCells");
        Undo.RegisterCreatedObjectUndo(gridCellsContainer, "Create Grid Cells Container");
        gridCellsContainer.transform.SetParent(gridParent.transform);
        gridCellsContainer.transform.localPosition = Vector3.zero;
        gridCellsContainer.transform.localRotation = Quaternion.identity;

        GameObject wallsContainer = new GameObject("Walls");
        Undo.RegisterCreatedObjectUndo(wallsContainer, "Create Walls Container");
        wallsContainer.transform.SetParent(gridParent.transform);
        wallsContainer.transform.localPosition = Vector3.zero;
        wallsContainer.transform.localRotation = Quaternion.identity;

        GameObject blocksContainer = new GameObject("Blocks");
        Undo.RegisterCreatedObjectUndo(blocksContainer, "Create Blocks Container");
        blocksContainer.transform.SetParent(gridParent.transform);
        blocksContainer.transform.localPosition = new Vector3(0f, wallHeight / 2, 0f);
        blocksContainer.transform.localRotation = Quaternion.identity;

        // Determine which cells are inside the walls
        bool[,] interiorCells = new bool[gridWidth, gridLength];
        FindInteriorCells(interiorCells);

        // Create grid cells for interior playable areas
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // Only create cells for interior non-wall areas
                if (!gridWalls[x, z] && interiorCells[x, z])
                {
                    CreateGridCell(gridCellsContainer, x, z);
                }
            }
        }


        // Create walls using specialized method
        CreateWalls(wallsContainer, interiorCells);

        // Initialize grid manager with created cells
        gridManager.WallParent = wallsContainer.transform;
        gridManager.GridStartPosition = gridParent.transform.position;
        gridManager.InitializeGridFromChildren();
        gridManager.MarkExteriorCellsAsOccupied(interiorCells);
        gridManager.SaveGridDataToAsset(gridName);
        EditorUtility.SetDirty(gridParent);
        Debug.Log("Custom grid created successfully!");
    }
    #endregion
    #region Wall Creation
    private void CreateWalls(GameObject parent, bool[,] interiorCells)
    {
        // Map the walls on a 2D array
        // We use a 2D array where each cell stores if there's a wall to the North(0), East(1), South(2), or West(3)
        bool[,,] wallMap = new bool[gridWidth, gridLength, 4]; // [x, z, direction]

        //* First pass: Map all walls onto the array
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                bool isPlayable = !gridWalls[x, z] && interiorCells[x, z];

                if (isPlayable)
                {
                    // North wall (0)
                    if (z == gridLength - 1 || (z < gridLength - 1 && (gridWalls[x, z + 1] || !interiorCells[x, z + 1])))
                    {
                        wallMap[x, z, 0] = true;
                    }

                    // East wall (1)
                    if (x == gridWidth - 1 || (x < gridWidth - 1 && (gridWalls[x + 1, z] || !interiorCells[x + 1, z])))
                    {
                        wallMap[x, z, 1] = true;
                    }

                    // South wall (2)
                    if (z == 0 || (z > 0 && (gridWalls[x, z - 1] || !interiorCells[x, z - 1])))
                    {
                        wallMap[x, z, 2] = true;
                    }

                    // West wall (3)
                    if (x == 0 || (x > 0 && (gridWalls[x - 1, z] || !interiorCells[x - 1, z])))
                    {
                        wallMap[x, z, 3] = true;
                    }
                }
            }
        }

        //* Second pass: Create straight walls from the map
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // North wall
                if (wallMap[x, z, 0])
                {
                    CreateStraightWall(wallMap, parent, x, z, 90, false, false, true, false);
                }

                // East wall
                if (wallMap[x, z, 1])
                {
                    CreateStraightWall(wallMap, parent, x, z, 0, false, true, false, false);
                }

                // South wall
                if (wallMap[x, z, 2])
                {
                    CreateStraightWall(wallMap, parent, x, z, 90, false, false, false, true);
                }

                // West wall
                if (wallMap[x, z, 3])
                {
                    CreateStraightWall(wallMap, parent, x, z, 0, true, false, false, false);
                }
            }
        }

        //* Third pass: Create corner walls where two adjacent walls meet
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // Only check for corners in playable cells
                if (!gridWalls[x, z] && interiorCells[x, z])
                {
                    // Northeast corner (where north and east walls meet)
                    if (wallMap[x, z, 0] && wallMap[x, z, 1])
                    {
                        CreateCornerWall(parent, x, z, 0); // 0 = Northeast corner type
                    }

                    // Southeast corner (where east and south walls meet)
                    if (wallMap[x, z, 1] && wallMap[x, z, 2])
                    {
                        CreateCornerWall(parent, x, z, 1); // 1 = Southeast corner type
                    }

                    // Southwest corner (where south and west walls meet)
                    if (wallMap[x, z, 2] && wallMap[x, z, 3])
                    {
                        CreateCornerWall(parent, x, z, 2); // 2 = Southwest corner type
                    }

                    // Northwest corner (where west and north walls meet)
                    if (wallMap[x, z, 3] && wallMap[x, z, 0])
                    {
                        CreateCornerWall(parent, x, z, 3); // 3 = Northwest corner type
                    }
                }
            }
        }
        //* Fourth pass: Create interior corner walls (for inside corners of L shapes)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                // Only check for interior corners in wall cells
                if (gridWalls[x, z] || !interiorCells[x, z])
                {
                    // Check for Northeast interior corner
                    // This happens when there's a wall cell at (x,z) and playable cells at (x+1,z) and (x,z+1)
                    if (x < gridWidth - 1 && z < gridLength - 1 &&
                        !gridWalls[x + 1, z] && interiorCells[x + 1, z] &&
                        !gridWalls[x, z + 1] && interiorCells[x, z + 1])
                    {
                        // Northeast interior corner - face Southwest (diagonally opposite)
                        CreateInteriorCornerWall(parent, x, z, 2); // 2 = Southwest facing
                    }

                    // Check for Northwest interior corner
                    // This happens when there's a wall cell at (x,z) and playable cells at (x-1,z) and (x,z+1)
                    if (x > 0 && z < gridLength - 1 &&
                        !gridWalls[x - 1, z] && interiorCells[x - 1, z] &&
                        !gridWalls[x, z + 1] && interiorCells[x, z + 1])
                    {
                        // Northwest interior corner - face Southeast
                        CreateInteriorCornerWall(parent, x, z, 1); // 1 = Southeast facing
                    }

                    // Check for Southwest interior corner
                    // This happens when there's a wall cell at (x,z) and playable cells at (x-1,z) and (x,z-1)
                    if (x > 0 && z > 0 &&
                        !gridWalls[x - 1, z] && interiorCells[x - 1, z] &&
                        !gridWalls[x, z - 1] && interiorCells[x, z - 1])
                    {
                        // Southwest interior corner - face Northeast
                        CreateInteriorCornerWall(parent, x, z, 0); // 0 = Northeast facing
                    }

                    // Check for Southeast interior corner
                    // This happens when there's a wall cell at (x,z) and playable cells at (x+1,z) and (x,z-1)
                    if (x < gridWidth - 1 && z > 0 &&
                        !gridWalls[x + 1, z] && interiorCells[x + 1, z] &&
                        !gridWalls[x, z - 1] && interiorCells[x, z - 1])
                    {
                        // Southeast interior corner - face Northwest
                        CreateInteriorCornerWall(parent, x, z, 3); // 3 = Northwest facing
                    }
                }
            }
        }
    }

    private void CreateCornerWall(GameObject parent, int x, int z, int cornerType)
    {
        GameObject wall = null;
        if (cornerWallMeshPrefab != null)
        {
            wall = PrefabUtility.InstantiatePrefab(cornerWallMeshPrefab) as GameObject;
        }
        else
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.localScale = new Vector3(straightWallThickness, wallHeight, straightWallThickness);
        }

        Undo.RegisterCreatedObjectUndo(wall, "Create Corner Wall");
        wall.name = $"Wall_Corner_{x}_{z}_{cornerType}";
        wall.transform.SetParent(parent.transform);
        wall.gameObject.tag = "Wall";

        // Position the corner at the correct location based on type
        Vector2Int wallPos = new Vector2Int(x, z); // Default to the cell position
        float cornerWallYOffset = 0.225f;
        float cornerWallOffset = 0.17f; // Offset for corner walls
        switch (cornerType)
        {
            case 0: // Northeast
                wall.transform.localPosition = new Vector3(x * spacing + wallOffset, cornerWallYOffset, z * spacing + wallOffset);
                // Check if there's an actual wall cell to the northeast
                if (x + 1 < gridWidth && z + 1 < gridLength && gridWalls[x + 1, z + 1])
                    wallPos = new Vector2Int(x + 1, z + 1);
                break;
            case 1: // Southeast
                wall.transform.localPosition = new Vector3(x * spacing + wallOffset, cornerWallYOffset, z * spacing - wallOffset);
                // Check if there's an actual wall cell to the southeast
                if (x + 1 < gridWidth && z - 1 >= 0 && gridWalls[x + 1, z - 1])
                    wallPos = new Vector2Int(x + 1, z - 1);
                break;
            case 2: // Southwest
                wall.transform.localPosition = new Vector3(x * spacing - wallOffset, cornerWallYOffset, z * spacing - wallOffset);
                // Check if there's an actual wall cell to the southwest
                if (x - 1 >= 0 && z - 1 >= 0 && gridWalls[x - 1, z - 1])
                    wallPos = new Vector2Int(x - 1, z - 1);
                break;
            case 3: // Northwest
                wall.transform.localPosition = new Vector3(x * spacing - wallOffset, cornerWallYOffset, z * spacing + wallOffset);
                // Check if there's an actual wall cell to the northwest
                if (x - 1 >= 0 && z + 1 < gridLength && gridWalls[x - 1, z + 1])
                    wallPos = new Vector2Int(x - 1, z + 1);
                break;
        }

        // Apply rotation if using custom mesh
        if (cornerWallMeshPrefab != null)
        {
            wall.transform.localRotation = Quaternion.Euler(0, 270 * cornerType, 0);

            //!TEMP CODE BAD PRACTICE
            if (cornerType == 2)
            {
                wall.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            else if (cornerType == 0)
            {
                wall.transform.localRotation = Quaternion.Euler(0, -180, 0);
            }

        }
        wall.AddComponent<WallData>();
        wall.GetComponent<WallData>().wallGridPosition = wallPos;
    }


    private void CreateStraightWall(bool[,,] wallMap, GameObject parent, int x, int z, float rotationY,
     bool isWestEdge = false, bool isEastEdge = false, bool isNorthEdge = false, bool isSouthEdge = false)
    {
        GameObject wall = null;
        bool hasCornerConnection = false;
        bool cornerNorth = false;
        bool cornerSouth = false;
        bool cornerEast = false;
        bool cornerWest = false;

        // Determine which specific corner this wall connects to
        if (isNorthEdge)
        {
            // Check for northeast corner
            if (x < gridWidth - 1 && wallMap[x, z, 1])
            {
                hasCornerConnection = true;
                cornerEast = true;
            }
            // Check for northwest corner
            if (x > 0 && wallMap[x, z, 3])
            {
                hasCornerConnection = true;
                cornerWest = true;
            }
        }
        else if (isEastEdge)
        {
            // Check for northeast corner
            if (z < gridLength - 1 && wallMap[x, z, 0])
            {
                hasCornerConnection = true;
                cornerNorth = true;
            }
            // Check for southeast corner
            if (z > 0 && wallMap[x, z, 2])
            {
                hasCornerConnection = true;
                cornerSouth = true;
            }
        }
        else if (isSouthEdge)
        {
            // Check for southeast corner
            if (x < gridWidth - 1 && wallMap[x, z, 1])
            {
                hasCornerConnection = true;
                cornerEast = true;
            }
            // Check for southwest corner
            if (x > 0 && wallMap[x, z, 3])
            {
                hasCornerConnection = true;
                cornerWest = true;
            }
        }
        else if (isWestEdge)
        {
            // Check for northwest corner
            if (z < gridLength - 1 && wallMap[x, z, 0])
            {
                hasCornerConnection = true;
                cornerNorth = true;
            }
            // Check for southwest corner
            if (z > 0 && wallMap[x, z, 2])
            {
                hasCornerConnection = true;
                cornerSouth = true;
            }
        }

        // Use end wall prefab if there's a corner connection
        if (hasCornerConnection && straightWallEndMeshPrefab != null)
        {
            wall = PrefabUtility.InstantiatePrefab(straightWallEndMeshPrefab) as GameObject;
        }
        else if (straightWallMeshPrefab != null)
        {
            wall = PrefabUtility.InstantiatePrefab(straightWallMeshPrefab) as GameObject;
        }
        else
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        Undo.RegisterCreatedObjectUndo(wall, "Create Straight Wall");
        wall.name = hasCornerConnection ? $"Wall_StraightEnd_{x}_{z}_{rotationY}" : $"Wall_Straight_{x}_{z}_{rotationY}";
        wall.transform.SetParent(parent.transform);
        wall.gameObject.tag = "Wall";

        // Position and rotate the wall
        float overlapExtension = straightWallThickness * 0.5f;
        Vector2Int wallPos = new Vector2Int(x, z); // Default to the cell position

        // Position based on wall type
        switch ((int)rotationY)
        {
            case 0: // East/West walls (using 0 degrees)
                if (isWestEdge)
                {
                    wall.transform.localPosition = new Vector3(x * spacing - wallOffset, wallHeight, z * spacing);

                    if (hasCornerConnection)
                    {
                        if (cornerNorth)
                        {
                            // West wall with corner to the north - closed end should face north
                            wall.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        }
                        else if (cornerSouth)
                        {
                            // West wall with corner to the south - closed end should face south
                            wall.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        }
                        else
                        {
                            // Default
                            wall.transform.localRotation = Quaternion.Euler(0, 270, 0);
                        }
                    }

                    // Check if there's a wall cell to the west
                    if (x - 1 >= 0 && gridWalls[x - 1, z])
                        wallPos = new Vector2Int(x - 1, z);
                }
                else // East edge
                {
                    wall.transform.localPosition = new Vector3(x * spacing + wallOffset, wallHeight, z * spacing);

                    if (hasCornerConnection)
                    {
                        if (cornerNorth)
                        {
                            // East wall with corner to the north - closed end should face north
                            wall.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        }
                        else if (cornerSouth)
                        {
                            // East wall with corner to the south - closed end should face south
                            wall.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        }
                        else
                        {
                            // Default
                            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        }
                    }

                    // Check if there's a wall cell to the east
                    if (x + 1 < gridWidth && gridWalls[x + 1, z])
                        wallPos = new Vector2Int(x + 1, z);
                }

                if (straightWallMeshPrefab == null && straightWallEndMeshPrefab == null)
                    wall.transform.localScale = new Vector3(straightWallThickness, wallHeight, spacing + overlapExtension);
                break;

            case 90: // North/South walls (using 90 degrees)
                if (isSouthEdge)
                {
                    wall.transform.localPosition = new Vector3(x * spacing, wallHeight, z * spacing - wallOffset);

                    if (hasCornerConnection)
                    {
                        if (cornerEast)
                        {
                            // South wall with corner to the east - closed end should face east
                            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        }
                        else if (cornerWest)
                        {
                            // South wall with corner to the west - closed end should face west
                            wall.transform.localRotation = Quaternion.Euler(0, 270, 0);
                        }
                        else
                        {
                            // Default
                            wall.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        }
                    }

                    // Check if there's a wall cell to the south
                    if (z - 1 >= 0 && gridWalls[x, z - 1])
                        wallPos = new Vector2Int(x, z - 1);
                }
                else // North edge
                {
                    wall.transform.localPosition = new Vector3(x * spacing, wallHeight, z * spacing + wallOffset);

                    if (hasCornerConnection)
                    {
                        if (cornerEast)
                        {
                            // North wall with corner to the east - closed end should face east
                            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        }
                        else if (cornerWest)
                        {
                            // North wall with corner to the west - closed end should face west
                            wall.transform.localRotation = Quaternion.Euler(0, 270, 0);
                        }
                        else
                        {
                            // Default
                            wall.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        }
                    }

                    // Check if there's a wall cell to the north
                    if (z + 1 < gridLength && gridWalls[x, z + 1])
                        wallPos = new Vector2Int(x, z + 1);
                }
                break;
        }

        // Only apply standard rotation if not a corner connection
        if (!hasCornerConnection)
        {
            wall.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
        }

        // Scale adjustments for custom meshes
        if (straightWallMeshPrefab != null || straightWallEndMeshPrefab != null)
        {
            Vector3 currentScale = wall.transform.localScale;
            float wallMultiplier = 1.1f;

            if (rotationY == 0 || (hasCornerConnection && (isEastEdge || isWestEdge))) // East/West walls
            {
                wall.transform.localScale = new Vector3(currentScale.x, currentScale.y, straightWallThickness);
                wall.transform.localScale = new Vector3(
                    wall.transform.localScale.x,
                    wall.transform.localScale.y,
                    wall.transform.localScale.z * wallMultiplier);
            }
            else // North/South walls
            {
                wall.transform.localScale = new Vector3(currentScale.x, currentScale.y, straightWallThickness);
                wall.transform.localScale = new Vector3(
                    wall.transform.localScale.x,
                    wall.transform.localScale.y,
                    wall.transform.localScale.z * wallMultiplier);
            }
        }
        wall.AddComponent<WallData>();
        wall.GetComponent<WallData>().wallGridPosition = wallPos;
    }
    private void CreateInteriorCornerWall(GameObject parent, int x, int z, int cornerType)
    {
        GameObject wall = null;
        if (cornerWallMeshPrefab != null)
        {
            wall = PrefabUtility.InstantiatePrefab(cornerWallMeshPrefab) as GameObject;
        }
        else
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.localScale = new Vector3(straightWallThickness, wallHeight, straightWallThickness);
        }
    
        Undo.RegisterCreatedObjectUndo(wall, "Create Interior Corner Wall");
        wall.name = $"Wall_InteriorCorner_{x}_{z}_{cornerType}";
        wall.transform.SetParent(parent.transform);
        wall.gameObject.tag = "Wall";
    
        // Position the interior corner at the correct location based on type
        Vector2Int wallPos = new Vector2Int(x, z); // Default to the cell position
        float cornerWallYOffset = 0.225f;
        float interiorWallOffset = 0.13f; // Offset for interior corners
        
        switch (cornerType)
        {
            case 0: // Northeast facing - place in southwest corner of cell
                wall.transform.localPosition = new Vector3(x * spacing - interiorWallOffset, cornerWallYOffset, z * spacing - interiorWallOffset);
                break;
            case 1: // Southeast facing - place in northwest corner of cell
                wall.transform.localPosition = new Vector3(x * spacing - interiorWallOffset, cornerWallYOffset, z * spacing + interiorWallOffset);
                break;
            case 2: // Southwest facing - place in northeast corner of cell
                wall.transform.localPosition = new Vector3(x * spacing + interiorWallOffset, cornerWallYOffset, z * spacing + interiorWallOffset);
                break;
            case 3: // Northwest facing - place in southeast corner of cell
                wall.transform.localPosition = new Vector3(x * spacing + interiorWallOffset, cornerWallYOffset, z * spacing - interiorWallOffset);
                break;
        }
    
        // Apply rotation if using custom mesh
        if (cornerWallMeshPrefab != null)
        {
            wall.transform.localRotation = Quaternion.Euler(0, 270 * cornerType, 0);
    
            //!TEMP CODE BAD PRACTICE - maintain consistency with exterior corners
            if (cornerType == 1)
            {
                wall.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (cornerType == 3)
            {
                wall.transform.localRotation = Quaternion.Euler(0, -90, 0);
            }
        }
    
        wall.AddComponent<WallData>();
        wall.GetComponent<WallData>().wallGridPosition = wallPos;
        
        // Find and adjust any straight walls in this same cell
        AdjustStraightWallsInInteriorCornerCell(parent, x, z, cornerType);
    }
    
    // New method to find and adjust straight walls in cells with interior corners
      private void AdjustStraightWallsInInteriorCornerCell(GameObject parent, int x, int z, int cornerType)
    {
        // Find all wall objects that are positioned in this cell
        foreach (Transform child in parent.transform)
        {
            // Only modify straight walls (not corner walls)
            if (child.name.Contains("Wall_Straight") || child.name.Contains("Wall_StraightEnd"))
            {
                // Check if this wall has a WallData component with matching position
                WallData wallData = child.GetComponent<WallData>();
                if (wallData != null && wallData.wallGridPosition.x == x && wallData.wallGridPosition.y == z)
                {
                    // Adjust scale - make z dimension thinner
                    Vector3 scale = child.localScale;
                    scale.z = 0.25f;
                    child.localScale = scale;
                    
                    // Determine if this is an East/West wall or North/South wall
                    float yRotation = child.localRotation.eulerAngles.y;
                    bool isEastWestWall = (Mathf.Approximately(yRotation, 0) || Mathf.Approximately(yRotation, 180) ||
                                           yRotation == 0 || yRotation == 180);
                    bool isNorthSouthWall = (Mathf.Approximately(yRotation, 90) || Mathf.Approximately(yRotation, 270) ||
                                            yRotation == 90 || yRotation == 270);
                    
                    // The wall name also contains rotation information we can use as a fallback
                    if (!isEastWestWall && !isNorthSouthWall)
                    {
                        isEastWestWall = child.name.Contains("_0_");
                        isNorthSouthWall = child.name.Contains("_90_");
                    }
                    
                    // Apply position offset based on wall orientation and corner type
                    Vector3 position = child.localPosition;
                    float offsetAmount = 0.13f;
                    
                    switch (cornerType)
                    {
                        case 0: // Northeast facing (in southwest corner)
                            if (isEastWestWall) {
                                // Only move horizontal wall in Z direction
                                position.z += offsetAmount;
                            }
                            if (isNorthSouthWall) {
                                // Only move vertical wall in X direction
                                position.x += offsetAmount;
                            }
                            break;
                            
                        case 1: // Southeast facing (in northwest corner)
                            if (isEastWestWall) {
                                // Only move horizontal wall in Z direction
                                position.z -= offsetAmount;
                            }
                            if (isNorthSouthWall) {
                                // Only move vertical wall in X direction
                                position.x += offsetAmount;
                            }
                            break;
                            
                        case 2: // Southwest facing (in northeast corner)
                            if (isEastWestWall) {
                                // Only move horizontal wall in Z direction
                                position.z -= offsetAmount;
                            }
                            if (isNorthSouthWall) {
                                // Only move vertical wall in X direction
                                position.x -= offsetAmount;
                            }
                            break;
                            
                        case 3: // Northwest facing (in southeast corner)
                            if (isEastWestWall) {
                                // Only move horizontal wall in Z direction
                                position.z += offsetAmount;
                            }
                            if (isNorthSouthWall) {
                                // Only move vertical wall in X direction
                                position.x -= offsetAmount;
                            }
                            break;
                    }
                    
                    child.localPosition = position;
                }
            }
        }
    }
    private void InitializeGridWalls(int width, int length)
    {
        // Initialize the grid walls array
        gridWalls = new bool[width, length];

        // By default, all cells are normal (not walls)
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                gridWalls[x, z] = false;
            }
        }
    }

    #endregion
    #region Cell Creation
    private void CreateGridCell(GameObject parent, int x, int z)
    {
        // Create cell
        GameObject cell = null;
        if (gridCellMeshPrefab != null)
        {
            cell = PrefabUtility.InstantiatePrefab(gridCellMeshPrefab) as GameObject;
        }
        else
        {
            cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Default scaling for primitive cube - slightly smaller than cell size
            cell.transform.localScale = new Vector3(1 * 0.9f, 0.1f, 1 * 0.9f);
        }

        Undo.RegisterCreatedObjectUndo(cell, "Create Grid Cell");

        // Setup cell
        cell.name = $"Cell_{x}_{z}";
        cell.transform.SetParent(parent.transform);

        cell.transform.localPosition = new Vector3(x * spacing, 0, z * spacing);

        // Apply alternating materials in a checkerboard pattern
        MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Determine which material to use based on position
            bool isEvenCell = (x + z) % 2 == 0;

            if (isEvenCell && gridCellMaterial_1 != null)
            {
                renderer.material = gridCellMaterial_1;
            }
            else if (!isEvenCell && gridCellMaterial_2 != null)
            {
                renderer.material = gridCellMaterial_2;
            }
        }
        cell.GetComponent<CellObject>().CellPosition = new Vector2Int(x, z);
    }
    #endregion
}