using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Collection", menuName = "3D Block Puzzle/Level Collection")]
public class LevelData : ScriptableObject
{
    [Tooltip("Name of this level collection")]
    public string collectionName;
    
    [Tooltip("Description of this level collection")]
    [TextArea(2, 4)]
    public string description;
    
    [Tooltip("List of level prefabs in this collection")]
    public List<GameObject> levelPrefabs = new List<GameObject>();
}