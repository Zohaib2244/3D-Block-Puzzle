using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gate VFX Set", menuName = "3D Block Puzzle/Gate VFX Set")]
public class GateVFXSet : ScriptableObject
{
    public List<GateVFX> gateVFXList = new List<GateVFX>();

    public GameObject GetVFXPrefab(BlockColorTypes colorType)
    {
        foreach (var gateVFX in gateVFXList)
        {
            if (gateVFX.colorType == colorType)
            {
                return gateVFX.vfxPrefab;
            }
        }
        return null;
    }
}