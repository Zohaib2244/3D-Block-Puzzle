using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BlockShape))]
public class BlockShapeEditor : Editor
{
    private bool[,] editingGrid = new bool[5,5];
    private bool isEditing = false;

    // Add fixed colors for editor display
    private readonly Color activeColor = new Color(0.2f, 0.6f, 1f); // Blue color for active cells
    private readonly Color inactiveColor = Color.gray;
    private readonly Color previewInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.2f);
    
    public override void OnInspectorGUI()
    {
        BlockShape blockShape = (BlockShape)target;
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("meshOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pivotCell"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape Definition", EditorStyles.boldLabel);
        
        if (!isEditing)
        {
            if (GUILayout.Button("Edit Shape"))
            {
                isEditing = true;
                // Initialize editing grid
                for (int z = 0; z < 5; z++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        editingGrid[x,z] = blockShape.shapeDefinition.cells[x,z];
                    }
                }
            }
        }
        else
        {
            DrawShapeEditor();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                // Copy editing grid to shape definition
                for (int z = 0; z < 5; z++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        blockShape.shapeDefinition.cells[x,z] = editingGrid[x,z];
                    }
                }
                blockShape.shapeDefinition.UpdateSerializableFromCells();
                isEditing = false;
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Cancel"))
            {
                isEditing = false;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        // Preview the shape
        if (!isEditing)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            DrawShapePreview(blockShape);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawShapeEditor()
    {
        BlockShape blockShape = (BlockShape)target;
        GUILayout.Label("Click on cells to toggle them on/off", EditorStyles.helpBox);
        
        float cellSize = 25f;
        float gridSize = 5 * cellSize;
        
        Rect gridRect = GUILayoutUtility.GetRect(gridSize, gridSize);
        
        // Draw grid
        for (int z = 0; z < 5; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                float xPos = gridRect.x + x * cellSize;
                float yPos = gridRect.y + z * cellSize;
                Rect cellRect = new Rect(xPos, yPos, cellSize - 1, cellSize - 1);
                
                EditorGUI.DrawRect(cellRect, editingGrid[x,z] ? activeColor : inactiveColor);
                
                // Handle click
                if (Event.current.type == EventType.MouseDown && 
                    cellRect.Contains(Event.current.mousePosition))
                {
                    editingGrid[x,z] = !editingGrid[x,z];
                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }
    
    private void DrawShapePreview(BlockShape blockShape)
    {
        float cellSize = 20f;
        float gridSize = 5 * cellSize;
        
        Rect gridRect = GUILayoutUtility.GetRect(gridSize, gridSize);
        
        // Draw grid
        for (int z = 0; z < 5; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                float xPos = gridRect.x + x * cellSize;
                float yPos = gridRect.y + z * cellSize;
                Rect cellRect = new Rect(xPos, yPos, cellSize - 1, cellSize - 1);
                
                if (blockShape.shapeDefinition.cells[x,z])
                {
                    EditorGUI.DrawRect(cellRect, activeColor);
                }
                else
                {
                    EditorGUI.DrawRect(cellRect, previewInactiveColor);
                }
            }
        }
    }
    
    private void OnDisable()
    {
        if (target != null)
        {
            BlockShape blockShape = (BlockShape)target;
            blockShape.shapeDefinition.UpdateSerializableFromCells();
            EditorUtility.SetDirty(target);
        }
    }
}
