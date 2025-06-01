using UnityEngine;

public static class BlockProperties
{
    public const float maxTiltAngle = 10f; // Maximum tilt angle for blocks
    public const float tiltSmoothness = 5f;
    public const float collisionThreshold = 0.9f; // Threshold for collision detection

    public static float GetBlockRandomStartYValue()
    {
        // Generate a random start Y value for blocks
        return Random.Range(0.5f, 1.5f);
    }
    public static float GetBlockStartRandomAnimationDuration()
    {
        // Generate a random duration for block start animation
        return Random.Range(0.5f, 1f);
    }
}