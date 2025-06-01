using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using DG.Tweening;
using static Gate;

public class Block : MonoBehaviour
{
    [SerializeField] private BlockShape shape;
    public BlockShape Shape => shape;

    [SerializeField] private BlockColorTypes blockColor;
    public BlockColorTypes BlockColor => blockColor;
    [SerializeField] private float yPos = 0f;
    public float YPos => yPos;
    [SerializeField] private Outline outline;
    public UnityEvent OnBlockPlaced;

    // Current position in the grid
    [SerializeField]
    // Hide in inspector to avoid clutter
    private Vector2Int[] gridPosition;
    public Vector2Int[] GridPosition => gridPosition;

    // Reference to the grid manager
    [SerializeField] public GridManager gridManager;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private Material originalMaterial;
    private float collisionThreshold => BlockProperties.collisionThreshold;
    private float maxTiltAngle => BlockProperties.maxTiltAngle;

    private float tiltSmoothness => BlockProperties.tiltSmoothness;


    // Track movement for tilt effect
    private Vector3 lastDragDelta = Vector3.zero;
    private Quaternion targetRotation;
    private Quaternion originalRotation;

    void Start()
    {
        originalMaterial = GameConstants.GetBlockColorMaterial(blockColor);
        OnBlockPlaced.AddListener(() => gridManager.CheckGatePulls());
        Initialize();
        outline.enabled = false;

        // transform.position = new Vector3(transform.position.x, BlockProperties.GetBlockRandomStartYValue(), transform.position.z);


        originalRotation = transform.rotation;
        targetRotation = originalRotation;

        StartAnimation();
    }
    void Update()
    {
        if (isDragging)
        {
            // Smoothly rotate toward target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * tiltSmoothness);
        }
    }
    #region Block Setup For Level
    public void Initialize()
    {
        // Apply the block's color/material to the mesh renderer
        if (originalMaterial)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer)
            {
                renderer.material = originalMaterial;
            }
        }
    }
    // Set the block's current grid position
    public void SetGridPosition(Vector2Int[] position)
    {
        gridPosition = position;
    }
    #endregion
    #region Block Movement Methods
    public void StartDragging()
    {
        Debug.Log($"Starting drag for block: {name}");
        originalPosition = transform.position;
        isDragging = true;
        // Lift the block slightly when dragging
        transform.position += new Vector3(0, 0.05f, 0);
        AudioManager.Instance.PlayBlockMove();
        outline.enabled = true;
        lastDragDelta = Vector3.zero; // Reset last drag delta
        gridManager.onBlockClicked?.Invoke(); 
    }

    public void UpdateDragPosition(Vector3 dragDelta)
    {
        if (!isDragging) return;
    
        float dragYOffset = 0.05f;
    
        ApplyTiltEffect(dragDelta);
    
        // Calculate the potential new position
        Vector3 currentPos = transform.position;
        Vector3 desiredPosition = new Vector3(
            currentPos.x + dragDelta.x,
            yPos + dragYOffset,
            currentPos.z + dragDelta.z
        );
    
        // Check for fast movement that could cause tunneling
        float moveDistance = Vector3.Distance(currentPos, desiredPosition);
        float cellSize = gridManager ? gridManager.GetCellSize() : 1f;
        bool isFastMovement = moveDistance > cellSize * 0.75f; // If moving more than 75% of a cell in one frame
    
        // Use constraint-based approach with continuous collision detection
        if (gridManager)
        {
            Vector3 finalPosition;
    
            if (isFastMovement)
            {
                // For very fast movements, use step-based approach to prevent tunneling
                Vector3 moveDir = (desiredPosition - currentPos).normalized;
                float stepDistance = cellSize * 0.5f; // Half a cell per step
                int steps = Mathf.CeilToInt(moveDistance / stepDistance);
                
                finalPosition = currentPos;
                
                // Take multiple steps along the path
                for (int i = 0; i < steps; i++)
                {
                    Vector3 stepTarget = currentPos + (moveDir * stepDistance * (i + 1));
                    stepTarget.y = currentPos.y; // Keep y constant for collision checks
                     
                    if (IsPositionValid(stepTarget))
                    {
                        finalPosition = stepTarget;
                    }
                    else
                    {
                        // Found a collision, find exact point
                        finalPosition = FindMaxValidPosition(finalPosition, stepTarget);
                        break;
                    }
                }
                
                // Set y position after collision checks
                finalPosition.y = yPos + dragYOffset;
            }
            else
            {
                // Original two-step approach for normal speed movements
                // Try horizontal movement first
                Vector3 horizontalMovement = new Vector3(
                    desiredPosition.x,
                    currentPos.y,
                    currentPos.z
                );
    
                // Check if horizontal move is valid
                bool horizontalValid = IsPositionValid(horizontalMovement);
    
                // If not valid, find the maximum valid position horizontally
                if (!horizontalValid)
                {
                    horizontalMovement = FindMaxValidPosition(currentPos, horizontalMovement);
                }
    
                // Now try vertical movement from the valid horizontal position
                Vector3 combinedMovement = new Vector3(
                    horizontalMovement.x,
                    desiredPosition.y,
                    desiredPosition.z
                );
    
                bool combinedValid = IsPositionValid(combinedMovement);
    
                // If not valid, find maximum valid position vertically
                if (!combinedValid)
                {
                    combinedMovement = FindMaxValidPosition(horizontalMovement, combinedMovement);
                }
    
                finalPosition = combinedMovement;
            }
    
            // Apply the final valid position
            transform.position = finalPosition;
        }
        else
        {
            // No grid manager, move freely
            transform.position = desiredPosition;
        }
    }

    // New helper method to check if a position is valid
    private bool IsPositionValid(Vector3 testPosition)
    {
        float cellSize = gridManager.GetCellSize();
        Vector3 gridOrigin = gridManager.GridStartPosition;

        // Calculate exact grid position (non-rounded)
        float exactGridX = (testPosition.x - gridOrigin.x) / cellSize;
        float exactGridZ = (testPosition.z - gridOrigin.z) / cellSize;

        // Apply threshold to grid position calculation
        // This effectively expands collision boundaries by the threshold amount
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(exactGridX),
            Mathf.RoundToInt(exactGridZ)
        );

        // Get current rotation and determine occupied positions
        float yRotation = transform.rotation.eulerAngles.y;
        int rotationIndex = Mathf.RoundToInt(yRotation / 90f) % 4;

        List<Vector2Int> rotatedPositions;
        switch (rotationIndex)
        {
            case 1: rotatedPositions = shape.GetRotatedPositions90(); break;
            case 2: rotatedPositions = shape.GetRotatedPositions180(); break;
            case 3: rotatedPositions = shape.GetRotatedPositions270(); break;
            default: rotatedPositions = shape.GetOccupiedPositions(); break;
        }

        // Calculate all positions this block would occupy
        Vector2Int[] potentialPositions = rotatedPositions
            .Select(pos => gridPos + pos)
            .ToArray();

        // Standard valid placement check
        bool baseValid = gridManager.IsValidPlacement(this, potentialPositions);

        // Apply threshold-based collision detection
        if (baseValid && collisionThreshold > 0.5f) // Only perform extra check when threshold is higher than default
        {
            // Check if we're close to cell boundaries
            float fractionalX = Mathf.Abs(exactGridX - Mathf.Round(exactGridX));
            float fractionalZ = Mathf.Abs(exactGridZ - Mathf.Round(exactGridZ));

            // If we're closer to a cell boundary than our threshold allows, check adjacent cells
            if (fractionalX > (1.0f - collisionThreshold) || fractionalZ > (1.0f - collisionThreshold))
            {
                // Determine which adjacent cells to check based on our position
                List<Vector2Int> adjacentDirections = new List<Vector2Int>();

                if (fractionalX > (1.0f - collisionThreshold))
                {
                    // Add horizontal direction based on which cell boundary we're close to
                    adjacentDirections.Add(new Vector2Int(exactGridX > Mathf.Round(exactGridX) ? 1 : -1, 0));
                }

                if (fractionalZ > (1.0f - collisionThreshold))
                {
                    // Add vertical direction based on which cell boundary we're close to
                    adjacentDirections.Add(new Vector2Int(0, exactGridZ > Mathf.Round(exactGridZ) ? 1 : -1));
                }

                // If close to both boundaries, also check diagonal
                if (fractionalX > (1.0f - collisionThreshold) && fractionalZ > (1.0f - collisionThreshold))
                {
                    adjacentDirections.Add(new Vector2Int(
                        exactGridX > Mathf.Round(exactGridX) ? 1 : -1,
                        exactGridZ > Mathf.Round(exactGridZ) ? 1 : -1
                    ));
                }

                // Check all selected adjacent cells
                foreach (var dir in adjacentDirections)
                {
                    Vector2Int[] adjacentPositions = rotatedPositions
                        .Select(pos => gridPos + pos + dir)
                        .ToArray();

                    // If any adjacent cell is invalid, consider current position invalid too
                    if (!gridManager.IsValidPlacement(this, adjacentPositions))
                    {
                        return false;
                    }
                }
            }
        }

        return baseValid;
    }

    private Vector3 FindMaxValidPosition(Vector3 startPos, Vector3 endPos)
    {
        // Binary search to find maximum valid position
        Vector3 validPos = startPos;
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);

        // Adjust initial step based on collision threshold
        // Higher threshold = smaller initial step (more cautious approach)
        float step = Mathf.Lerp(0.75f, 0.25f, collisionThreshold);

        // Use 8 iterations of binary search for good precision vs performance
        for (int i = 0; i < 8; i++)
        {
            Vector3 testPos = validPos + (direction * step * distance);

            if (IsPositionValid(testPos))
            {
                validPos = testPos;
            }

            // Reduce step for next iteration
            step *= 0.5f;
        }

        // After binary search, apply collision threshold as final offset from boundary
        // Higher threshold = further from boundary
        if (collisionThreshold > 0.5f)
        {
            // Scale the offset: no offset at 0.5, maximum at 1.0
            float boundaryOffset = Mathf.Lerp(0, 0.2f, (collisionThreshold - 0.5f) * 2f);
            validPos -= direction * (boundaryOffset * distance);
        }

        return validPos;
    }

    public void EndDragging()
    {
        if (!isDragging) return;
        isDragging = false;

        AudioManager.Instance.PlayBlockPlace();


        // Reset rotation smoothly back to original
        transform.DORotateQuaternion(originalRotation, 0.3f)
            .SetEase(Ease.OutElastic, 0.5f, 0.3f);

        if (gridManager)
        {
            // NOW we snap to grid
            float cellSize = gridManager.GetCellSize();
            Vector3 gridOrigin = gridManager.GridStartPosition;

            // Find nearest grid position
            Vector2Int snappedGridPos = new Vector2Int(
                Mathf.RoundToInt((transform.position.x - gridOrigin.x) / cellSize),
                Mathf.RoundToInt((transform.position.z - gridOrigin.z) / cellSize)
            );

            // ---- FIX: HANDLING ROTATION ----
            // Get the rotation around Y axis (in degrees)
            float yRotation = transform.rotation.eulerAngles.y;

            // Round to nearest 90 degrees for snapping
            int rotationIndex = Mathf.RoundToInt(yRotation / 90f) % 4;

            // Get the properly rotated positions based on current rotation
            List<Vector2Int> rotatedOccupiedPositions;
            switch (rotationIndex)
            {
                case 1: // 90 degrees
                    rotatedOccupiedPositions = shape.GetRotatedPositions90();
                    break;
                case 2: // 180 degrees
                    rotatedOccupiedPositions = shape.GetRotatedPositions180();
                    break;
                case 3: // 270 degrees
                    rotatedOccupiedPositions = shape.GetRotatedPositions270();
                    break;
                default: // 0 degrees
                    rotatedOccupiedPositions = shape.GetOccupiedPositions();
                    break;
            }

            // Calculate all positions this rotated block would occupy
            Vector2Int[] newGridPositions = new Vector2Int[rotatedOccupiedPositions.Count];
            for (int i = 0; i < rotatedOccupiedPositions.Count; i++)
            {
                newGridPositions[i] = snappedGridPos + rotatedOccupiedPositions[i];
            }
            // ---- END FIX ----

            // Set final position
            float snappedX = gridOrigin.x + (snappedGridPos.x * cellSize);
            float snappedZ = gridOrigin.z + (snappedGridPos.y * cellSize);
            Vector3 snappedPosition = new Vector3(snappedX, yPos, snappedZ);

            // Final check to ensure valid placement
            bool finalCheck = gridManager.IsValidPlacement(this, newGridPositions);

            if (finalCheck)
            {
                // Get current position
                Vector3 startPosition = transform.position;

                // Calculate animation duration based on distance (faster for smaller distances)
                float distance = Vector3.Distance(startPosition, snappedPosition);
                float duration = Mathf.Clamp(distance * 0.1f, 0.1f, 0.25f); // 0.1-0.25 seconds

                // Animate to snapped position
                transform.DOMove(snappedPosition, duration)
                    .SetEase(Ease.OutBounce) // Add slight bounce effect
                    .OnComplete(() =>
                    {
                        // Inform grid manager about the placement
                        gridManager.PlaceBlock(this, newGridPositions, gridPosition);
                        outline.enabled = false;
                        // Update block's grid position
                        SetGridPosition(newGridPositions);

                        OnBlockPlaced?.Invoke(); // Trigger event for block placement

                    });

                return;
            }
        }
        outline.enabled = false;
        // Animate return to original position
        Vector3 returnPosition = new Vector3(originalPosition.x, yPos, originalPosition.z);
        transform.DOMove(returnPosition, 0.2f).SetEase(Ease.OutBounce);
    }
    #endregion
    #region Utility Methods
    private void ApplyTiltEffect(Vector3 dragDelta)
    {
        // Only calculate tilt if we have significant movement
        if (dragDelta.magnitude > 0.001f)
        {
            // Store drag delta for smoothing
            lastDragDelta = Vector3.Lerp(lastDragDelta, dragDelta, 0.2f);

            // Calculate tilt angles based on movement direction
            float tiltZ = -lastDragDelta.x * maxTiltAngle; // Tilt left/right
            float tiltX = lastDragDelta.z * maxTiltAngle;  // Tilt forward/backward

            // Get the current Y rotation from original rotation (to preserve the block's orientation)
            float currentYRotation = originalRotation.eulerAngles.y;

            // Create target rotation with tilt applied
            targetRotation = Quaternion.Euler(tiltX, currentYRotation, tiltZ);
        }
    }
    public void ConfigureMaterialToDissappear(GateDirection gateDirection, Vector3 gatePosition)
    {
        Material mat = gameObject.GetComponent<Renderer>().material;
        mat.SetVector("_GatePosition", gatePosition);
        Vector3 gateDirectionVector = Vector3.zero;
        switch (gateDirection)
        {
            case GateDirection.North:
                gateDirectionVector = new Vector3(0, 0, -1);
                break;
            case GateDirection.South:
                gateDirectionVector = new Vector3(0, 0, 1);
                break;
            case GateDirection.East:
                gateDirectionVector = new Vector3(-1, 0, 0);
                break;
            case GateDirection.West:
                gateDirectionVector = new Vector3(1, 0, 0);
                break;
        }
        mat.SetVector("_GateDirection", gateDirectionVector);
    }
    #endregion
    public BlockColorTypes GetCurrentColor()
    {
        return blockColor;
    }
    public void SetColor(BlockColorTypes newColor)
    {
        blockColor = newColor;
        Material mat = GameConstants.GetBlockColorMaterial(blockColor);
        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.materials = new Material[] { mat };
        }
    }
    void OnValidate()
    {
        Material mat = GameConstants.GetBlockColorMaterial(blockColor);
        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.materials = new Material[] { mat };
        }
    }
    void StartAnimation()
    {
        transform.DOLocalJump(
            new Vector3(transform.localPosition.x, yPos, transform.localPosition.z),
            jumpPower: 0.5f,
            numJumps: 1,
            duration: BlockProperties.GetBlockStartRandomAnimationDuration())
            .SetEase(Ease.OutQuad);
    }
}