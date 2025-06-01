using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
[CustomEditor(typeof(LevelManager))]

public class LevelManagerEditor : Editor
{
    private string newLevelName = "LVL_";
    private bool showConfigHelp = false;
    private bool showRenameHelp = false;
    private bool showCenterHelp = false;
    
    public override void OnInspectorGUI()
    {
        LevelManager levelManager = (LevelManager)target;
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);
        
        // Help box toggle for configuration
        showConfigHelp = EditorGUILayout.Foldout(showConfigHelp, "What does Configure Data do?");
        if (showConfigHelp)
        {
            EditorGUILayout.HelpBox("This button will:\n• Get the GridManager reference from children\n• Store the current camera position\n\nUse this after setting up your level layout but before saving.", MessageType.Info);
        }
        
        // Colored configure button
        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f); // Light blue
        if (GUILayout.Button("Configure Data", GUILayout.Height(30)))
        {
            levelManager.ConfigureLevel();
        }
        GUI.backgroundColor = Color.white; // Reset color
        
        // CENTER POSITION SECTION
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Camera Positioning", EditorStyles.boldLabel);
        
        // Help box toggle for centering
        showCenterHelp = EditorGUILayout.Foldout(showCenterHelp, "What does Move Camera To Position do?");
        if (showCenterHelp)
        {
            EditorGUILayout.HelpBox("This will:\n• Move The Camera To The Levels Camera position.", MessageType.Info);
        }
        
        // Colored center button
        GUI.backgroundColor = new Color(1f, 0.7f, 0.3f); // Orange
        if (GUILayout.Button("Move Camera To Position", GUILayout.Height(30)))
        {
            levelManager.MoveCameraToPosition();
            
            // Ensure changes are saved
            EditorUtility.SetDirty(levelManager.gameObject);
            
        }
        GUI.backgroundColor = Color.white; // Reset color
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Renaming", EditorStyles.boldLabel);
        
        // Help box toggle for renaming
        showRenameHelp = EditorGUILayout.Foldout(showRenameHelp, "What does Rename Level do?");
        if (showRenameHelp)
        {
            EditorGUILayout.HelpBox("This will rename:\n• This GameObject\n• The GridData ScriptableObject\n• The prefab asset (if this is a prefab instance)\n\nAll references will be maintained.", MessageType.Info);
        }
        
        // Field for entering a new level name
        newLevelName = EditorGUILayout.TextField("New Level Name", newLevelName);
        
        // Button to rename the level
        GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f); // Light green
        if (GUILayout.Button("Rename Level", GUILayout.Height(30)) && !string.IsNullOrEmpty(newLevelName))
        {
            string oldName = levelManager.gameObject.name;
            levelManager.RenameLevel(newLevelName);
            
            // Make sure the object name change is reflected in the hierarchy
            EditorUtility.SetDirty(levelManager.gameObject);
            
            EditorUtility.DisplayDialog("Level Renamed", 
                $"Successfully renamed level from '{oldName}' to '{newLevelName}'!", 
                "OK");
                
            newLevelName = ""; // Clear the field after renaming
        }
        GUI.backgroundColor = Color.white; // Reset color
    }
}
#endif