using UnityEngine;
using Voodoo.Utils;

[RequireComponent(typeof(Block))]
public class BlockMeshHandler : MonoBehaviour
{
    private Block parentBlock;
    void Awake()
    {
        parentBlock = GetComponent<Block>();
    }
        
    // Add a method to handle user input for dragging
    public void OnTouchBegin()
    {
        if (parentBlock != null)
        {
            DebugLogger.Log("Touch began on block: " + parentBlock.name , DebugColor.Yellow);
            parentBlock.StartDragging();
            //*Play Feel
            Vibrations.Haptic(HapticTypes.LightImpact);
        }
    }
    
    public void OnTouchMove(Touch touch)
    {
        if (parentBlock != null)
        {
            DebugLogger.Log("Touch moved on block: " + parentBlock.name , DebugColor.Yellow);
            // Get the current world position under the touch
            Vector3 currentTouchWorldPos = GetWorldPositionFromTouch(touch.position);
            
            // Get the previous world position under the touch
            Vector3 previousTouchWorldPos = GetWorldPositionFromTouch(touch.position - touch.deltaPosition);
            
            // Calculate the actual movement in world space
            Vector3 movement = currentTouchWorldPos - previousTouchWorldPos;
            
            // Only move the block horizontally (X and Z)
            movement.y = 0;
            
            parentBlock.UpdateDragPosition(movement);
        }
    }
    
    public void OnTouchEnd()
    {
        if (parentBlock != null)
        {
            DebugLogger.Log("Touch ended on block: " + parentBlock.name , DebugColor.Yellow);
            parentBlock.EndDragging();
        }
            Vibrations.Haptic(HapticTypes.LightImpact);
    }
    
    private Vector3 GetWorldPositionFromTouch(Vector2 touchPosition)
    {
        // Create a plane at the block's height
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        
        // Cast a ray from the touch point
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        
        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        
        // Fallback
        return transform.position;
    }
}