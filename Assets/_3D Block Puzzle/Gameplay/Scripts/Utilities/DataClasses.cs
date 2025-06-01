using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockColor
{
    public BlockColorTypes colorType;
    public Material colorMaterial;
}
[Serializable]
public class gateColor
{
    public BlockColorTypes colorType;
    public Material colorMaterial;
}
[Serializable]
public class Gate
{
    public BlockColorTypes colorType; // Changed from Color to BlockColorTypes
    public List<Vector2Int> positions = new List<Vector2Int>();
    public GateDirection pullDirection;
    public enum GateDirection
    {
        North,
        South,
        East,
        West
    }
    public GateObject gateObject;
    public Gate(BlockColorTypes colorType, List<Vector2Int> positions, GateDirection pullDirection, GateObject gateObject, GameObject exitVFX)
    {
        this.colorType = colorType;
        this.positions = positions;
        this.pullDirection = pullDirection;
        this.gateObject = gateObject;
        if (gateObject)
        {
            gateObject.exitVFX = exitVFX;
        }
    }
}

[Serializable]
public class GateData // Move outside Gate class
{
    public List<GateObject> gateObjects = new List<GateObject>();
    public int[] gateColorTypes; // Store color type indices instead of colors
    public int[] gateDirections;
    public int[] gatePositionCounts;
    public int[] gatePositionsX;
    public int[] gatePositionsZ;
}
[Serializable]
public class UISCreens
{
    public ScreenType screenType;
    public Transform screenTransform;
    public bool showOverlay = false;

}