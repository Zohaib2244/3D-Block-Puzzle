using UnityEngine;

/// <summary>
/// Color options for debug messages
/// </summary>
public enum DebugColor
{
    White,      // Default
    Red,        // Errors
    Orange,     // Warnings 
    Yellow,     // Caution
    Green,      // Success
    Blue,       // Info
    Cyan,       // System
    Purple,     // Special
    Pink,       // Fun
    Gray        // Less important
}

/// <summary>
/// Static utility class for sending colored debug logs to the Unity console
/// </summary>
public static class DebugLogger
{
    /// <summary>
    /// Log a message with the specified color
    /// </summary>
    /// <param name="message">Message to log</param>
    /// <param name="color">Color of the message</param>
    public static void Log(object message, DebugColor color = DebugColor.White)
    {
        string coloredMessage = ApplyColor(message.ToString(), color);
        Debug.Log(coloredMessage);
    }
    
    /// <summary>
    /// Log a warning message with the specified color (defaults to orange)
    /// </summary>
    /// <param name="message">Warning message to log</param>
    /// <param name="color">Color of the message (default: orange)</param>
    public static void LogWarning(object message, DebugColor color = DebugColor.Orange)
    {
        string coloredMessage = ApplyColor(message.ToString(), color);
        Debug.LogWarning(coloredMessage);
    }
    
    /// <summary>
    /// Log an error message with the specified color (defaults to red)
    /// </summary>
    /// <param name="message">Error message to log</param>
    /// <param name="color">Color of the message (default: red)</param>
    public static void LogError(object message, DebugColor color = DebugColor.Red)
    {
        string coloredMessage = ApplyColor(message.ToString(), color);
        Debug.LogError(coloredMessage);
    }
    
    /// <summary>
    /// Apply color formatting to a message
    /// </summary>
    private static string ApplyColor(string message, DebugColor color)
    {
        string colorCode = GetColorCode(color);
        return $"<color={colorCode}>{message}</color>";
    }
    
    /// <summary>
    /// Get the hex color code for the specified color
    /// </summary>
    private static string GetColorCode(DebugColor color)
    {
        switch (color)
        {
            case DebugColor.White:
                return "#FFFFFF";
            case DebugColor.Red:
                return "#FF0000";
            case DebugColor.Orange:
                return "#FF7F00";
            case DebugColor.Yellow:
                return "#FFFF00";
            case DebugColor.Green:
                return "#00FF00";
            case DebugColor.Blue:
                return "#0000FF";
            case DebugColor.Cyan:
                return "#00FFFF";
            case DebugColor.Purple:
                return "#8B00FF";
            case DebugColor.Pink:
                return "#FF69B4";
            case DebugColor.Gray:
                return "#808080";
            default:
                return "#FFFFFF";
        }
    }
}