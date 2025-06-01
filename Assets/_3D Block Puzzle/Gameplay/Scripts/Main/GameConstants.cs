using System.Collections.Generic;
using UnityEngine;
using Voodoo.Utils;

public static class GameConstants
{
    private static List<BlockColor> blockColors = new List<BlockColor>();
    private static List<gateColor> gateColors = new List<gateColor>();
    private static Material defaultWallMaterial;
    private static int currentLevelIndex = 0;
    public static bool inputEnabled = true;
    public static int highestUnlockedLevelIndex
    {
        get { return PlayerPrefs.GetInt("HighestUnlockedLevelIndex", 0); }
        private set { PlayerPrefs.SetInt("HighestUnlockedLevelIndex", value); }
    }

    public static int CurrentLevelIndex
    {
        get { return currentLevelIndex; }
        set
        {
            currentLevelIndex = value;
            // Update the highest unlocked level if needed
            if (value > highestUnlockedLevelIndex && value < GameManager.Instance.TotalLevels)
            {
                highestUnlockedLevelIndex = value;
            }
        }
    }
    public static Material GetDefaultWallMaterial()
    {
        if (defaultWallMaterial == null)
        {
            defaultWallMaterial = Resources.Load<Material>("Materials/DefaultWall");
        }
        return defaultWallMaterial;
    }
    public static Material GetGateColorMaterial(BlockColorTypes colorType)
    {
        if (gateColors.Count == 0)
        {
            InitializeGateColors();
        }
        return gateColors[(int)colorType].colorMaterial;
    }
    static void InitializeGateColors()
    {
        int colorCount = EnumExtensions.Count<BlockColorTypes>();
        // Initialize
        gateColors = new List<gateColor>(colorCount);
        for (int i = 0; i < colorCount; i++)
        {
            gateColor color = new gateColor
            {
                colorType = (BlockColorTypes)i,
                colorMaterial = Resources.Load<Material>($"Materials/GateColors/{(BlockColorTypes)i}")
            };
            gateColors.Add(color);
        }
    }
    public static Material GetBlockColorMaterial(BlockColorTypes colorType)
    {
        if (blockColors.Count == 0)
        {
            InitializeBlockColors();
        }
        return blockColors[(int)colorType].colorMaterial;
    }

    static void InitializeBlockColors()
    {
        int colorCount = EnumExtensions.Count<BlockColorTypes>();
        // Initialize
        blockColors = new List<BlockColor>(colorCount);
        for (int i = 0; i < colorCount; i++)
        {
            BlockColor color = new BlockColor
            {
                colorType = (BlockColorTypes)i,
                colorMaterial = Resources.Load<Material>($"Materials/BlockColors/{(BlockColorTypes)i}")
            };
            blockColors.Add(color);
        }
    }
    public static void InitializeGame()
    {
        Application.targetFrameRate = 60;
        Settings.OnVibrationSettingChanged.AddListener((value) => Vibrations.canVibrate = value);
        GameUIManager.Instance.ShowScreen(ScreenType.Loading, 1);
    }
}