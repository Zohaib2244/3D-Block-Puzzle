using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using System.Reflection;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    // Grid visualization properties
    private bool showOccupancyGrid = false;
    private Vector2 scrollPosition;
    private Color occupiedColor = new Color(1f, 0.3f, 0.3f, 1f);
    private Color emptyColor = new Color(0.3f, 0.8f, 0.3f, 1f);
    private Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get the target GridManager
        GridManager gridManager = (GridManager)target;

        EditorGUILayout.Space();

        // Add Grid Visualization section
        EditorGUILayout.LabelField("Grid Visualization", EditorStyles.boldLabel);

        // Button to show/hide the grid
        showOccupancyGrid = EditorGUILayout.Foldout(showOccupancyGrid, "Show Occupancy Grid", true);

        if (showOccupancyGrid)
        {
            DrawOccupancyGrid(gridManager);

            // Add a button to refresh the view
            if (GUILayout.Button("Refresh Grid View"))
            {
                Repaint();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Creation Tools", EditorStyles.boldLabel);
        if(GUILayout.Button("Assign Exit VFX To Gates", GUILayout.Height(30)))
        {
            gridManager.AssignVFXToAllGates();
        }
        // Add a button for initializing blocks from scene
        if (GUILayout.Button("Initialize Grid", GUILayout.Height(30)))
        {
            //?InitializeBlocks(gridManager);
            gridManager.ConfigureGridInEditor();
        }
        if (GUILayout.Button("Create Level", GUILayout.Height(30)))
        {
            //create a new gameobject make the grid a child of that gameobject
            GameObject levelObject = new GameObject("LVL_");
            gridManager.transform.SetParent(levelObject.transform);
            levelObject.transform.position = Vector3.zero;
            levelObject.AddComponent<LevelManager>();
            levelObject.GetComponent<LevelManager>().ConfigureLevel();
        }
    }
    private void DrawOccupancyGrid(GridManager gridManager)
    {
        // Get the cellOccupied array via reflection (since it's private)
        var fieldInfo = typeof(GridManager).GetField("cellOccupied",
                          BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        if (fieldInfo == null)
        {
            EditorGUILayout.HelpBox("Could not access cellOccupied array!", MessageType.Error);
            return;
        }

        bool[,] cellOccupied = fieldInfo.GetValue(gridManager) as bool[,];

        if (cellOccupied == null)
        {
            EditorGUILayout.HelpBox("cellOccupied array is null!", MessageType.Error);
            return;
        }

        // Get grid dimensions
        int width = cellOccupied.GetLength(0);
        int length = cellOccupied.GetLength(1);

        if (width == 0 || length == 0)
        {
            EditorGUILayout.HelpBox("Grid dimensions are 0!", MessageType.Warning);
            return;
        }

        // Calculate cell size based on inspector width
        float availableWidth = EditorGUIUtility.currentViewWidth - 40;
        float cellSize = Mathf.Min(20, availableWidth / width);

        // Begin scroll view if the grid is large
        float gridHeight = cellSize * length + 20;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                            GUILayout.Height(Mathf.Min(gridHeight, 300)));

        // Calculate total grid size
        float gridWidth = cellSize * width;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        // Draw the grid
        Handles.BeginGUI();

        // Draw column labels (X axis)
        for (int x = 0; x < width; x++)
        {
            Rect labelRect = new Rect(gridRect.x + x * cellSize, gridRect.y - 15, cellSize, 15);
            GUI.Label(labelRect, x.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }
        // Draw row labels (Z axis)
        for (int z = 0; z < length; z++)
        {
            // Display the actual Z coordinate that corresponds to this visual row
            int dataZ = length - 1 - z; // This is the Z coordinate in the game world
            Rect labelRect = new Rect(gridRect.x - 20, gridRect.y + z * cellSize, 20, cellSize);
            GUI.Label(labelRect, dataZ.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight });
        }
        // Draw each cell
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                // In Unity UI, y-coordinates increase downwards, so no need to invert for visual layout
                int displayZ = z; // CHANGED: Don't invert for UI position
                int dataZ = length - 1 - z; // CHANGED: Invert for data access

                Rect cellRect = new Rect(
                    gridRect.x + x * cellSize,
                    gridRect.y + displayZ * cellSize,
                    cellSize,
                    cellSize
                );

                // Fill cell based on occupancy - use the raw data coordinate
                if (x < cellOccupied.GetLength(0) && dataZ < cellOccupied.GetLength(1))
                {
                    EditorGUI.DrawRect(cellRect, cellOccupied[x, dataZ] ? occupiedColor : emptyColor);

                    // Add a visual indicator for occupied cells
                    if (cellOccupied[x, dataZ])
                    {
                        GUI.Label(cellRect, "X", new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white },
                            fontStyle = FontStyle.Bold
                        });
                    }
                }

                // Draw cell border
                Handles.color = gridLineColor;
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y));
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x, cellRect.y + cellRect.height));
                Handles.DrawLine(new Vector3(cellRect.x + cellRect.width, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y + cellRect.height), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));
            }
        }

        Handles.EndGUI();

        EditorGUILayout.EndScrollView();
    }

    private void InitializeBlocks(GridManager gridManager)
    {
        // Find all Block components under this GridManager
        Block[] childBlocks = gridManager.GetComponentsInChildren<Block>(true);

        if (childBlocks.Length == 0)
        {
            EditorUtility.DisplayDialog("No Blocks Found",
                "No Block components found in children. Make sure blocks are placed as children of the GridManager and have Block components attached.",
                "OK");
            return;
        }

        int successCount = 0;
        List<string> failedBlocks = new List<string>();
        gridManager.ClearBlocks(); // Clear existing blocks before adding new ones
        // Process each block
        foreach (Block block in childBlocks)
        {
            if (TryAddBlockToGrid(gridManager, block))
            {
                successCount++;
            }
            else
            {
                failedBlocks.Add(block.name);
            }
        }
        // Display results
        string message = $"Successfully added {successCount} blocks to the grid.";
        if (failedBlocks.Count > 0)
        {
            message += $"\n\nFailed to add {failedBlocks.Count} blocks:";
            foreach (string blockName in failedBlocks)
            {
                message += $"\n- {blockName}";
            }
        }

        EditorUtility.DisplayDialog("Initialize Blocks Result", message, "OK");

        // Mark the scene as dirty to ensure changes are saved
        if (successCount > 0)
        {
            EditorUtility.SetDirty(gridManager);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gridManager.gameObject.scene);
            }
        }

        // Refresh the grid view
        Repaint();
    }

    private bool TryAddBlockToGrid(GridManager gridManager, Block block)
    {
        // Get all block segment positions
        Transform[] blockParts = block.GetComponentsInChildren<Transform>();
        List<Vector3> segmentPositions = new List<Vector3>();

        // Skip the block's own transform, only collect child segment positions
        foreach (Transform part in blockParts)
        {
            if (part != block.transform)
            {
                segmentPositions.Add(part.position);
            }
        }

        // If no segments found, use the block's own position
        if (segmentPositions.Count == 0)
        {
            segmentPositions.Add(block.transform.position);
        }

        // Convert to array
        Vector3[] worldPositions = segmentPositions.ToArray();

        // First, determine if we need to snap the positions to the grid
        bool needsSnapping = false;
        Vector2Int[] gridPositions = gridManager.WorldToGridPositions(worldPositions);

        foreach (Vector2Int pos in gridPositions)
        {
            if (!gridManager.IsWithinGrid(pos.x, pos.y))
            {
                needsSnapping = true;
                break;
            }
        }

        // If positions are outside the grid, try to snap them to the closest valid grid position
        if (needsSnapping)
        {
            // First calculate block center
            Vector3 blockCenter = Vector3.zero;
            foreach (Vector3 pos in worldPositions)
            {
                blockCenter += pos;
            }
            blockCenter /= worldPositions.Length;

            // Find closest grid cell to this center
            float gridCellSize = gridManager.GetCellSize();
            Vector3 gridStart = gridManager.GridStartPosition;

            // Calculate grid-aligned center position
            float closestGridX = Mathf.Round((blockCenter.x - gridStart.x) / gridCellSize) * gridCellSize + gridStart.x;
            float closestGridZ = Mathf.Round((blockCenter.z - gridStart.z) / gridCellSize) * gridCellSize + gridStart.z;
            Vector3 snappedCenter = new Vector3(closestGridX, blockCenter.y, closestGridZ);

            // Calculate offset to move the block
            Vector3 offset = snappedCenter - blockCenter;

            // Apply offset to all world positions
            for (int i = 0; i < worldPositions.Length; i++)
            {
                worldPositions[i] += offset;
            }

            // Recalculate grid positions with snapped world positions
            gridPositions = gridManager.WorldToGridPositions(worldPositions);

            // Show a message that snapping occurred
            Debug.Log($"Block '{block.name}' was automatically snapped to the nearest grid position.");
        }

        // Validate grid positions again after potential snapping
        foreach (Vector2Int pos in gridPositions)
        {
            if (!gridManager.IsWithinGrid(pos.x, pos.y))
            {
                Debug.LogWarning($"Block '{block.name}' has segments outside the grid boundaries even after snapping. Please place it closer to the grid.");
                return false;
            }

            if (gridManager.IsCellOccupied(pos.x, pos.y))
            {
                Debug.LogWarning($"Block '{block.name}' overlaps with another block at grid position ({pos.x}, {pos.y}).");
                return false;
            }
        }

        // If snapping was applied, move the block in the scene for visual feedback
        if (needsSnapping)
        {
            // Calculate the offset to the block's pivot
            Vector3 pivotOffset = worldPositions[0] - block.transform.position;
            if (worldPositions.Length > 1)
            {
                // For multi-part blocks, use average position
                Vector3 avgPos = Vector3.zero;
                foreach (Vector3 pos in worldPositions)
                {
                    avgPos += pos;
                }
                avgPos /= worldPositions.Length;
                pivotOffset = avgPos - block.transform.position;
            }

            // Move the block in the scene
            Undo.RecordObject(block.transform, "Snap Block to Grid");
            block.transform.position += pivotOffset;
        }

        // Add the block to the grid
        gridManager.AddBlock(block, gridPositions);
        return true;
    }
}