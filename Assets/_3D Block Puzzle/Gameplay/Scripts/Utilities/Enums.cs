using System;

public enum BlockColorTypes
{
    Red,
    Orange,
    Yellow,
    Blue,
    Cyan,
    Green,
    Purple,
    Pink,
    DarkGreen
}

public static class EnumExtensions
{
    // Returns the number of values in any enum
    public static int Count<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Length;
    }
}

public enum ScreenType
{
    MainMenu,
    LevelSelection,
    GamePlay,
    Settings,
    GameOver,
    LevelCompleted,
    Loading,
    Pause,
    NoInternet,
}
public enum LevelState
{
    None,
    InProgress,
    Completed,
    Failed,
}