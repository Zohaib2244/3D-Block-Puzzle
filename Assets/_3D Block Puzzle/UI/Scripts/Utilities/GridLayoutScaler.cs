using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dynamically adjusts a GridLayoutGroup's cell size and spacing based on screen resolution
/// </summary>
public class GridLayoutScaler : MonoBehaviour
{
    [Header("Reference Resolution")]
    [Tooltip("The resolution width this layout was designed for")]
    public float referenceWidth = 1080f;
    [Tooltip("The resolution height this layout was designed for")]
    public float referenceHeight = 1920f;

    [Header("GridLayout Settings")]
    [Tooltip("Original cell size at reference resolution")]
    public Vector2 referenceCellSize = new Vector2(100f, 100f);
    [Tooltip("Original spacing at reference resolution")]
    public Vector2 referenceSpacing = new Vector2(10f, 10f);
    
    [Header("Reference Padding")]
    [Tooltip("Original left padding at reference resolution")]
    public int referencePaddingLeft = 10;
    [Tooltip("Original right padding at reference resolution")]
    public int referencePaddingRight = 10;
    [Tooltip("Original top padding at reference resolution")]
    public int referencePaddingTop = 10;
    [Tooltip("Original bottom padding at reference resolution")]
    public int referencePaddingBottom = 10;
    
    [Header("Scaling Options")]
    [Range(0f, 1f)]
    [Tooltip("0 = Scale based on width only, 1 = Scale based on height only, 0.5 = Balance between both")]
    public float heightWidthBalance = 0.5f;
    [Tooltip("Minimum allowed scale factor")]
    public float minScaleFactor = 0.5f;
    [Tooltip("Maximum allowed scale factor")]
    public float maxScaleFactor = 1.5f;

    private GridLayoutGroup gridLayout;
    private float currentScaleFactor = 1f;
    private RectOffset referencePadding;

    private void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("GridLayoutScaler requires a GridLayoutGroup component");
            enabled = false;
            return;
        }
        
        // Create reference padding in Awake instead of during field initialization
        referencePadding = new RectOffset(
            referencePaddingLeft,
            referencePaddingRight,
            referencePaddingTop,
            referencePaddingBottom
        );
        
        // Store initial values if not set in inspector
        if (referenceCellSize == Vector2.zero)
            referenceCellSize = gridLayout.cellSize;
        if (referenceSpacing == Vector2.zero)
            referenceSpacing = gridLayout.spacing;
        if (referencePadding.left == 0 && referencePadding.right == 0 && 
            referencePadding.top == 0 && referencePadding.bottom == 0)
        {
            referencePaddingLeft = gridLayout.padding.left;
            referencePaddingRight = gridLayout.padding.right;
            referencePaddingTop = gridLayout.padding.top;
            referencePaddingBottom = gridLayout.padding.bottom;
            referencePadding = new RectOffset(
                referencePaddingLeft,
                referencePaddingRight,
                referencePaddingTop,
                referencePaddingBottom
            );
        }
            
        // Apply scaling on start
        UpdateGridLayoutScale();
    }

    private void OnRectTransformDimensionsChange()
    {
        // Update when the parent RectTransform changes (orientation change, etc)
        UpdateGridLayoutScale();
    }

    public void UpdateGridLayoutScale()
    {
        if (gridLayout == null) return;
        
        // Calculate width and height scale factors
        float widthFactor = Screen.width / referenceWidth;
        float heightFactor = Screen.height / referenceHeight;
        
        // Blend between width and height factors based on the balance setting
        currentScaleFactor = Mathf.Lerp(widthFactor, heightFactor, heightWidthBalance);
        
        // Apply min/max constraints
        currentScaleFactor = Mathf.Clamp(currentScaleFactor, minScaleFactor, maxScaleFactor);
        
        // Apply to grid layout properties
        gridLayout.cellSize = referenceCellSize * currentScaleFactor;
        gridLayout.spacing = referenceSpacing * currentScaleFactor;
        
        // Scale the padding
        gridLayout.padding.left = Mathf.RoundToInt(referencePadding.left * currentScaleFactor);
        gridLayout.padding.right = Mathf.RoundToInt(referencePadding.right * currentScaleFactor);
        gridLayout.padding.top = Mathf.RoundToInt(referencePadding.top * currentScaleFactor);
        gridLayout.padding.bottom = Mathf.RoundToInt(referencePadding.bottom * currentScaleFactor);
    }

    // Call this method if you need to update the layout manually
    public void ForceUpdate()
    {
        UpdateGridLayoutScale();
    }

    // For debugging/testing in editor
    #if UNITY_EDITOR
    [ContextMenu("Apply Scaling Now")]
    void ApplyScalingNow()
    {
        UpdateGridLayoutScale();
    }
    #endif
}
