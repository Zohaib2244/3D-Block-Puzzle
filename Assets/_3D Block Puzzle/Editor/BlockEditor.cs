using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(Block))]
public class BlockEditor : Editor
{
    private Block block;
    private BlockColorTypes selectedColorType;
    
    private void OnEnable()
    {
        block = (Block)target;
        // Initialize selected color type to the block's current color
        if (block != null)
        {
            selectedColorType = block.BlockColor;
        }
    }
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector excluding the color field
        serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        
        // Draw the default inspector properties
        DrawPropertiesExcluding(serializedObject, "blockColor");
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Block Color Selection", EditorStyles.boldLabel);
        
        // Replace color enum dropdown with color buttons
        EditorGUILayout.LabelField("Block Color:", EditorStyles.boldLabel);
        
        // Start horizontal layout for the color buttons
        EditorGUILayout.BeginHorizontal();
        
        // Create a button for each color type
        BlockColorTypes[] colorTypes = (BlockColorTypes[])System.Enum.GetValues(typeof(BlockColorTypes));
        
        // Calculate how many buttons per row
        int buttonsPerRow = 4;
        int buttonCount = 0;
        
        foreach (BlockColorTypes colorType in colorTypes)
        {
            // Start a new row if needed
            if (buttonCount > 0 && buttonCount % buttonsPerRow == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            
            // Get the material color from GameConstants
            Color buttonColor = GameConstants.GetBlockColorMaterial(colorType).color;
            string colorName = colorType.ToString();
            
            // Style the button based on selection
            GUI.backgroundColor = (selectedColorType == colorType) ? 
                Color.white : new Color(buttonColor.r * 0.7f, buttonColor.g * 0.7f, buttonColor.b * 0.7f);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fixedHeight = 30;
            
            if (selectedColorType == colorType)
            {
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.normal.textColor = Color.black;
            }
            
            if (GUILayout.Button(colorName, buttonStyle))
            {
                // Update the selected color
                selectedColorType = colorType;
                
                // Apply the color change to the block using the SetColor method
                Undo.RecordObject(block, "Change Block Color");
                block.SetColor(colorType);
                
                // Mark the object as dirty to ensure the scene is saved properly
                EditorUtility.SetDirty(block);
            }
            
            buttonCount++;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Add preview of selected color
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected Color:", EditorStyles.boldLabel, GUILayout.Width(100));
        
        Rect colorRect = EditorGUILayout.GetControlRect(GUILayout.Height(20), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(colorRect, GameConstants.GetBlockColorMaterial(selectedColorType).color);
        EditorGUILayout.EndHorizontal();
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
    }
}