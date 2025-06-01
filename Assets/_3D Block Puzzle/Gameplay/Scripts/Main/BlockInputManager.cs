using UnityEngine;

public class BlockInputManager : MonoBehaviour
{
    private BlockMeshHandler currentMeshHandler; // Track the currently active handler
    private bool isDragging = false; // Track drag state
    private int activeTouchId = -1; // Track which touch is active for our drag

    void Update()
    {
        // Handle touch input
        if (Input.touchCount > 0 && GameConstants.inputEnabled)
        {
            // Process all touches to find our active one
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                
                // If we have an active drag and this is our touch
                if (isDragging && activeTouchId == touch.fingerId)
                {
                    // Handle the active drag regardless of raycast hit
                    if (touch.phase == TouchPhase.Moved)
                    {
                        currentMeshHandler?.OnTouchMove(touch);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        // End the drag operation
                        currentMeshHandler?.OnTouchEnd();
                        isDragging = false;
                        activeTouchId = -1;
                        currentMeshHandler = null;
                    }
                    
                    // Skip further processing for this touch
                    continue;
                }
                
                // Normal processing for new touches or non-active touches
                if (!isDragging && touch.phase == TouchPhase.Began)
                {
                    Ray ray = Camera.main.ScreenPointToRay(touch.position);
                    RaycastHit hit;
                    
                    if (Physics.Raycast(ray, out hit))
                    {
                        // Try to get BlockMeshHandler
                        if (hit.collider.TryGetComponent(out BlockMeshHandler handler))
                        {
                            // Start a new drag operation
                            currentMeshHandler = handler;
                            isDragging = true;
                            activeTouchId = touch.fingerId;
                            currentMeshHandler.OnTouchBegin();
                        }
                    }
                }
            }
        }
        else if (isDragging)
        {
            // All touches ended without proper detection (rare edge case)
            currentMeshHandler?.OnTouchEnd();
            isDragging = false;
            activeTouchId = -1;
            currentMeshHandler = null;
        }
    }
}