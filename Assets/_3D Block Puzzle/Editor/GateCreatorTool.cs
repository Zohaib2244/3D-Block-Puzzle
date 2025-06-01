#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GateCreatorTool : EditorWindow
{
    #region Variables
    private GridManager targetGrid;
    private BlockColorTypes selectedColorType = BlockColorTypes.Red;
    private Gate.GateDirection selectedDirection = Gate.GateDirection.North;
    private List<Vector2Int> selectedPositions = new List<Vector2Int>();
    private Vector2 scrollPosition;

    // Gate mesh settings
    private GameObject gateMeshPrefab;
    private float gateZScale = 0.85f;

    // Grid visualization settings
    private float cellSize = 20f;
    private float gridPadding = 10f;
    private bool showWallLabels = true;

    // Selection mode
    private bool isSelectingGate = false;
    private bool isRemovingGate = false;
    #endregion

    [MenuItem("Block Puzzle/Gate Creator")]
    public static void ShowWindow()
    {
        GateCreatorTool window = GetWindow<GateCreatorTool>("Gate Creator");
        window.minSize = new Vector2(400, 500);
    }
    #region OnGui
    void OnEnable()
    {
        // Auto-detect the gate mesh prefab
        AutoDetectGateMeshPrefab();
    }
    private void OnGUI()
    {
        // Header
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Gate Creator Tool", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        // Grid Selection
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

        // Target grid selection
        targetGrid = (GridManager)EditorGUILayout.ObjectField("Target Grid", targetGrid, typeof(GridManager), true);

        if (targetGrid == null)
        {
            EditorGUILayout.HelpBox("Please select a GridManager object", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        // Gate mesh prefab selection
        gateMeshPrefab = (GameObject)EditorGUILayout.ObjectField("Gate Mesh Prefab", gateMeshPrefab, typeof(GameObject), false);

        // Grid visualization settings
        cellSize = EditorGUILayout.Slider("Cell Size", cellSize, 10f, 40f);
        showWallLabels = EditorGUILayout.Toggle("Show Wall Labels", showWallLabels);

        EditorGUILayout.EndVertical();

        // Gate Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Gate Settings", EditorStyles.boldLabel);

        // Replace color enum dropdown with color buttons
        EditorGUILayout.LabelField("Arrow Color (Block Type):", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        // Create a button for each color type
        BlockColorTypes[] colorTypes = (BlockColorTypes[])System.Enum.GetValues(typeof(BlockColorTypes));
        foreach (BlockColorTypes colorType in colorTypes)
        {
            Color buttonColor = GameConstants.GetGateColorMaterial(colorType).color;
            string colorName = colorType.ToString();
            
            // Style the button based on selection
            GUI.backgroundColor = (selectedColorType == colorType) ? 
                Color.white : new Color(buttonColor.r * 0.7f, buttonColor.g * 0.7f, buttonColor.b * 0.7f);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            if (selectedColorType == colorType)
            {
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.normal.textColor = Color.black;
            }
            
            if (GUILayout.Button(colorName, buttonStyle, GUILayout.Height(30)))
            {
                selectedColorType = colorType;
            }
        }
        
        GUI.backgroundColor = Color.white; // Reset background color
        EditorGUILayout.EndHorizontal();
        
        // Replace direction enum dropdown with direction buttons
        EditorGUILayout.LabelField("Pull Direction:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        // Button for West
        GUI.backgroundColor = (selectedDirection == Gate.GateDirection.West) ? Color.green : Color.white;
        if (GUILayout.Button("← West", GUILayout.Height(30)))
        {
            selectedDirection = Gate.GateDirection.West;
        }
        // Button for North
        GUI.backgroundColor = (selectedDirection == Gate.GateDirection.North) ? Color.green : Color.white;
        if (GUILayout.Button("↑ North", GUILayout.Height(30)))
        {
            selectedDirection = Gate.GateDirection.North;
        }
        
        
        // Button for South
        GUI.backgroundColor = (selectedDirection == Gate.GateDirection.South) ? Color.green : Color.white;
        if (GUILayout.Button("↓ South", GUILayout.Height(30)))
        {
            selectedDirection = Gate.GateDirection.South;
        }
        // Button for East
        GUI.backgroundColor = (selectedDirection == Gate.GateDirection.East) ? Color.green : Color.white;
        if (GUILayout.Button("→ East", GUILayout.Height(30)))
        {
            selectedDirection = Gate.GateDirection.East;
        }
        
        
        GUI.backgroundColor = Color.white; // Reset background color
        EditorGUILayout.EndHorizontal();

        // Draw color preview
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Arrow Color Preview");
        var colorRect = EditorGUILayout.GetControlRect(GUILayout.Height(20f));
        EditorGUI.DrawRect(colorRect, GameConstants.GetGateColorMaterial(selectedColorType).color);
        EditorGUILayout.EndHorizontal();

        // Selection info
        EditorGUILayout.LabelField($"Selected Positions: {selectedPositions.Count}/4");

        // Buttons for gate mode selection
        EditorGUILayout.BeginHorizontal(); 

        GUI.backgroundColor = isSelectingGate ? Color.green : Color.white;
        if (GUILayout.Button("Select Gate Cells", GUILayout.Height(30)))  
        {
            isSelectingGate = true;
            isRemovingGate = false;
        }

        GUI.backgroundColor = isRemovingGate ? Color.red : Color.white;
        if (GUILayout.Button("Remove Gates", GUILayout.Height(30)))
        {
            isRemovingGate = true;
            isSelectingGate = false;
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Clear selection button
        if (selectedPositions.Count > 0)
        {
            if (GUILayout.Button("Clear Selection"))
            {
                selectedPositions.Clear();
                Repaint();
            }
        }

        // Add gate button
        GUI.enabled = selectedPositions.Count > 0 && selectedPositions.Count <= 4 && gateMeshPrefab != null;
        if (GUILayout.Button("Create Gate", GUILayout.Height(35)))
        {
            CreateGate();
            selectedPositions.Clear();
        }

        if (gateMeshPrefab == null)
        {
            EditorGUILayout.HelpBox("Please assign a Gate Mesh Prefab", MessageType.Warning);
        }

        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        // Grid visualization
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Grid Visualization", EditorStyles.boldLabel);

        // Calculate grid dimensions
        float gridWidth = targetGrid.GetGridWidth() * cellSize + gridPadding * 2;
        float gridHeight = targetGrid.GetGridLength() * cellSize + gridPadding * 2;

        // Begin scrollview if needed
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Height(Mathf.Min(gridHeight + 20, position.height - 300)));

        // Calculate the rect for our grid display
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        // Draw the grid background
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

        // Mark the active area
        Rect activeArea = new Rect(
            gridRect.x + gridPadding,
            gridRect.y + gridPadding,
            targetGrid.GetGridWidth() * cellSize,
            targetGrid.GetGridLength() * cellSize);

        EditorGUI.DrawRect(activeArea, new Color(0.3f, 0.3f, 0.3f));

        // Draw grid lines
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        for (int x = 0; x <= targetGrid.GetGridWidth(); x++)
        {
            Vector3 start = new Vector3(activeArea.x + x * cellSize, activeArea.y, 0);
            Vector3 end = new Vector3(activeArea.x + x * cellSize, activeArea.y + activeArea.height, 0);
            Handles.DrawLine(start, end);
        }
        for (int z = 0; z <= targetGrid.GetGridLength(); z++)
        {
            Vector3 start = new Vector3(activeArea.x, activeArea.y + z * cellSize, 0);
            Vector3 end = new Vector3(activeArea.x + activeArea.width, activeArea.y + z * cellSize, 0);
            Handles.DrawLine(start, end);
        }

        // Draw all walls from wallRegistry
        if (targetGrid != null)
        {
            // Check if method exists
            if (targetGrid.GetWallPositions() != null)
            {
                var wallPositions = targetGrid.GetWallPositions();
                foreach (var wallPos in wallPositions)
                {
                    DrawWallCell(activeArea, wallPos);
                }
            }
            else
            {
                // Fallback if GetWallPositions doesn't exist
                for (int x = 0; x < targetGrid.GetGridWidth(); x++)
                {
                    for (int z = 0; z < targetGrid.GetGridLength(); z++)
                    {
                        if (targetGrid.IsCellWall(x, z))
                        {
                            DrawWallCell(activeArea, new Vector2Int(x, z));
                        }
                    }
                }
            }
        }

        // Draw all gates
        foreach (var gate in targetGrid.GetGates())
        {
            foreach (var pos in gate.positions)
            {
                DrawGateCell(activeArea, pos, gate.colorType, gate.pullDirection);
            }
        }

        // Draw selected positions
        foreach (var pos in selectedPositions)
        {
            DrawSelectedCell(activeArea, pos);
        }
        // Handle cell selection
        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            activeArea.Contains(Event.current.mousePosition))
        {
            // Calculate which cell was clicked - adjusted for flipped Y axis
            int x = Mathf.FloorToInt((Event.current.mousePosition.x - activeArea.x) / cellSize);
            int z = targetGrid.GetGridLength() - 1 - Mathf.FloorToInt((Event.current.mousePosition.y - activeArea.y) / cellSize); // Invert Z calculation

            if (x >= 0 && x < targetGrid.GetGridWidth() && z >= 0 && z < targetGrid.GetGridLength())
            {
                Vector2Int clickPos = new Vector2Int(x, z);

                if (isSelectingGate)
                {
                    // Only allow selecting wall cells
                    if (targetGrid.IsCellWall(x, z))
                    {
                        // Toggle selection
                        if (selectedPositions.Contains(clickPos))
                        {
                            selectedPositions.Remove(clickPos);
                        }
                        else if (selectedPositions.Count < 4)  // Max 4 cells for a gate
                        {
                            selectedPositions.Add(clickPos);
                        }
                    }
                }
                else if (isRemovingGate)
                {
                    // Try to get gate at this position
                    Gate gateToRemove = targetGrid.GetGate(clickPos);
                    if (gateToRemove != null)
                    {
                        Undo.RecordObject(targetGrid, "Remove Gate");
                        targetGrid.RemoveGate(gateToRemove);
                        EditorUtility.SetDirty(targetGrid);
                    }
                }

                Event.current.Use(); // Consume the event
                Repaint();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Instructions
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Assign a Gate Mesh Prefab\n" +
            "2. Select 'Select Gate Cells' to choose wall cells for gate placement\n" +
            "3. Click on wall cells in the grid to select them\n" +
            "4. Choose an arrow color and direction for the gate\n" +
            "5. Click 'Create Gate' to add the gate mesh\n" +
            "6. Use 'Remove Gates' to delete existing gates",
            MessageType.Info);

        if (GUILayout.Button("Save All Gates", GUILayout.Height(30)))
        {
            targetGrid.SaveGateDataToGridData();
        }
        EditorGUILayout.EndVertical();
    }
    void AutoDetectGateMeshPrefab()
    {
        string DEFAULT_PREFABS_PATH = "Assets/_3D Block Puzzle/Gameplay/Prefabs/GridElements";

        string defaultPath = Path.Combine(DEFAULT_PREFABS_PATH, "Gate" + ".prefab");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(defaultPath);

        if (prefab != null)
            gateMeshPrefab = prefab;

    }
    #endregion

    private void CreateGate()
    {
        if (selectedPositions.Count == 0 || selectedPositions.Count > 4 || gateMeshPrefab == null)
            return;
    
        Undo.RecordObject(targetGrid, "Add Gate");
    
        // Create the gate in the GridManager
    
        // Create a single gate mesh for all positions
        GameObject gateMesh = PrefabUtility.InstantiatePrefab(gateMeshPrefab) as GameObject;
        if (gateMesh == null) return;
    
        // Set the gate parent - assuming there's a gates container in your grid hierarchy
        Transform gatesContainer = targetGrid.transform.Find("Gates");
        if (gatesContainer == null)
        {
            gatesContainer = new GameObject("Gates").transform;
            gatesContainer.SetParent(targetGrid.transform);
            gatesContainer.localPosition = Vector3.zero;
        }
    
        gateMesh.transform.SetParent(gatesContainer);
    
        // Configure the gate mesh to span all selected positions
        ConfigureGateMesh(gateMesh, selectedPositions, selectedDirection, selectedColorType);
    
        // Remove existing walls at all positions
        foreach (Vector2Int pos in selectedPositions)
        {
            RemoveWallAtPosition(pos);
        }
        GateObject gateObject = gateMesh.GetComponent<GateObject>();
        targetGrid.AddGate(selectedColorType, new List<Vector2Int>(selectedPositions), selectedDirection, gateObject);
    
        EditorUtility.SetDirty(targetGrid);
    }
    
    private void ConfigureGateMesh(GameObject gateMesh, List<Vector2Int> positions, Gate.GateDirection direction, BlockColorTypes colorType)
    {
        float spacing = 0.57f;
        float wallOffset = 0.17f;
        float wallHeight = 0.17f;
    
        // Set the base properties
        string positionString = string.Join("_", positions.Select(p => $"{p.x}_{p.y}"));
        gateMesh.name = $"Gate_{positionString}_{colorType}_{direction}";
        gateMesh.tag = "Gate";
    
        // Find min and max grid positions to determine span
        int minX = positions.Min(p => p.x);
        int maxX = positions.Max(p => p.x);
        int minY = positions.Min(p => p.y);
        int maxY = positions.Max(p => p.y);
        
        // Calculate center position of all gate cells
        Vector2 centerPos = new Vector2(
            (minX + maxX) / 2f,
            (minY + maxY) / 2f
        );
    
        // Calculate position and scale based on direction
        Vector3 gatePosition = Vector3.zero;
        Quaternion gateRotation = Quaternion.identity;
        Vector3 gateScale = gateMesh.transform.localScale;
    
        // Calculate span in cells
        int cellsSpanned = 1;
    
        switch (direction)
        {
            case Gate.GateDirection.North:
            case Gate.GateDirection.South:
                cellsSpanned = maxX - minX + 1;
                gatePosition = new Vector3(
                    centerPos.x * spacing, 
                    wallHeight, 
                    (direction == Gate.GateDirection.North) ? 
                        minY * spacing - wallOffset : 
                        maxY * spacing + wallOffset
                );
                gateRotation = Quaternion.Euler(0, (direction == Gate.GateDirection.North) ? 90 : 270, 0);
                gateScale = new Vector3(gateScale.x, gateScale.y, gateZScale * cellsSpanned);
                break;
    
            case Gate.GateDirection.East:
            case Gate.GateDirection.West:
                cellsSpanned = maxY - minY + 1;
                gatePosition = new Vector3(
                    (direction == Gate.GateDirection.East) ? 
                        maxX * spacing - wallOffset : 
                        minX * spacing + wallOffset,
                    wallHeight, 
                    centerPos.y * spacing
                );
                gateRotation = Quaternion.Euler(0, (direction == Gate.GateDirection.East) ? 180 : 0, 0);
                gateScale = new Vector3(gateScale.x, gateScale.y, gateZScale * cellsSpanned);
                break;
        }
    
        gateMesh.transform.localPosition = gatePosition;
        gateMesh.transform.localRotation = gateRotation;
        gateMesh.transform.localScale = gateScale;
        
               // Find and fix the arrow child's scale
        Transform arrowTransform = gateMesh.transform.GetChild(0);
        if (arrowTransform != null)
        {
            // Counteract the parent's scale to maintain proper proportions
            // This creates an inverse scale that cancels out the parent's scaling effect
            arrowTransform.localScale = new Vector3(
                1f,
                1f,
                1f / gateScale.z  // Compensate specifically for the Z-scale which varies with gate width
            );
        }
        // Set the gate material
        MeshRenderer meshRenderer = gateMesh.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material gateMaterial = GameConstants.GetGateColorMaterial(colorType);
            meshRenderer.material = gateMaterial;
        }
    }
    // Helper to remove existing wall mesh at gate position
private void RemoveWallAtPosition(Vector2Int position)
    {
        // Find wall container
        Transform wallsContainer = targetGrid.transform.Find("Walls");
        if (wallsContainer == null) return;

        // Check all wall objects to find one at this position
        foreach (Transform child in wallsContainer)
        {
            WallData wallData = child.GetComponent<WallData>();
            if (wallData != null && wallData.wallGridPosition == position)
            {
                // Found the wall at this position, destroy it
                Undo.DestroyObjectImmediate(child.gameObject);
                break;
            }
        }
    }
    private void DrawWallCell(Rect gridArea, Vector2Int pos)
    {
        // FIXED: Vertically flip the grid representation
        Rect cellRect = new Rect(
            gridArea.x + pos.x * cellSize,
            gridArea.y + (targetGrid.GetGridLength() - 1 - pos.y) * cellSize, // Invert Y axis
            cellSize,
            cellSize);

        // Draw wall cell
        EditorGUI.DrawRect(cellRect, new Color(0.4f, 0.4f, 0.4f));

        // Draw wall label if enabled
        if (showWallLabels)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            GUI.Label(cellRect, "W", labelStyle);
        }
    }


    private void DrawGateCell(Rect gridArea, Vector2Int pos, BlockColorTypes colorType, Gate.GateDirection direction)
    {
        Rect cellRect = new Rect(
            gridArea.x + pos.x * cellSize,
            gridArea.y + (targetGrid.GetGridLength() - 1 - pos.y) * cellSize, // Invert Y axis
            cellSize,
            cellSize);
        // Draw gate cell with gray background (representing the gate mesh)
        EditorGUI.DrawRect(cellRect, new Color(0.6f, 0.6f, 0.6f));

        // Draw direction indicator as a colored arrow
        Rect arrowRect = new Rect(cellRect);
        arrowRect.x += cellSize * 0.25f;
        arrowRect.y += cellSize * 0.25f;
        arrowRect.width = cellSize * 0.5f;
        arrowRect.height = cellSize * 0.5f;

        // Get arrow color based on the block type
        Material arrowMaterial = GameConstants.GetGateColorMaterial(colorType);
        EditorGUI.DrawRect(arrowRect, arrowMaterial.color);

        // Draw arrow direction
        GUIStyle arrowStyle = new GUIStyle();
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.normal.textColor = Color.black;
        arrowStyle.fontSize = Mathf.RoundToInt(cellSize * 0.4f);

        string directionChar = "?";
        switch (direction)
        {
            case Gate.GateDirection.North: directionChar = "↑"; break;
            case Gate.GateDirection.South: directionChar = "↓"; break;
            case Gate.GateDirection.East: directionChar = "→"; break;
            case Gate.GateDirection.West: directionChar = "←"; break;
        }

        GUI.Label(arrowRect, directionChar, arrowStyle);
    }

    private void DrawSelectedCell(Rect gridArea, Vector2Int pos)
    {
        // FIXED: Vertically flip the grid representation
        Rect cellRect = new Rect(
            gridArea.x + pos.x * cellSize,
            gridArea.y + (targetGrid.GetGridLength() - 1 - pos.y) * cellSize, // Invert Y axis
            cellSize,
            cellSize);


        // Draw selection outline
        Color selectionColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); // Gray for gate mesh

        // Inner area with semi-transparent color
        EditorGUI.DrawRect(cellRect, selectionColor);

        // Highlight border
        Handles.color = Color.white;
        Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y));
        Handles.DrawLine(new Vector3(cellRect.x + cellRect.width, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));
        Handles.DrawLine(new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height), new Vector3(cellRect.x, cellRect.y + cellRect.height));
        Handles.DrawLine(new Vector3(cellRect.x, cellRect.y + cellRect.height), new Vector3(cellRect.x, cellRect.y));

        // Draw colored arrow in the center to show what it will look like
        Rect arrowRect = new Rect(cellRect);
        arrowRect.x += cellSize * 0.25f;
        arrowRect.y += cellSize * 0.25f;
        arrowRect.width = cellSize * 0.5f;
        arrowRect.height = cellSize * 0.5f;

        Material arrowMaterial = GameConstants.GetGateColorMaterial(selectedColorType);
        EditorGUI.DrawRect(arrowRect, arrowMaterial.color);

        // Draw direction arrow
        GUIStyle arrowStyle = new GUIStyle();
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.normal.textColor = Color.black;
        arrowStyle.fontSize = Mathf.RoundToInt(cellSize * 0.4f);

        string directionChar = "?";
        switch (selectedDirection)
        {
            case Gate.GateDirection.North: directionChar = "↑"; break;
            case Gate.GateDirection.South: directionChar = "↓"; break;
            case Gate.GateDirection.East: directionChar = "→"; break;
            case Gate.GateDirection.West: directionChar = "←"; break;
        }

        GUI.Label(arrowRect, directionChar, arrowStyle);
    }
}
#endif