using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LevelSelectionScreen : MonoBehaviour
{
    [Header("Level Selection Main Object")]
    [SerializeField] private CanvasGroup levelSelectionObjectCanvasGroup;
    [SerializeField] private Transform levelSelectionObjectTransform;
    [SerializeField] private Transform levelButtonContainer;

    [Header("Level Selection UI Elements")]
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private GameObject topLeftCornerLevelButtonPrefab;
    [SerializeField] private GameObject topRightCornerLevelButtonPrefab;
    [SerializeField] private GameObject bottomLeftCornerLevelButtonPrefab;
    [SerializeField] private GameObject bottomRightCornerLevelButtonPrefab;
    [SerializeField] private List<Color> unlockedLevelButtonColors;
    [SerializeField] private Color lockedButtonColor;


    [Header("Pagination")]
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;

    [Header("Animation Settings")]
    [SerializeField] private AudioSource buttonSpawnAudioSource;
    [SerializeField] private AudioClip buttonSpawnAudioClip;
    [SerializeField] private float buttonSpawnDelay = 0.05f;
    [SerializeField] private float buttonSwitchDelay = 0.03f;

    private List<LevelSelectionButtonItem> levelButtons = new List<LevelSelectionButtonItem>();
    private int totalLevels = 0;
    private int currentPage = 1;
    private int levelsPerPage = 30;
    private int rows = 5;
    private int columns = 6;

    // Use RectTransform references directly to avoid GetComponent calls later
    private RectTransform nextButtonRect;
    private RectTransform prevButtonRect;
    private Vector2 initialNextButtonAnchoredPos;
    private Vector2 initialPrevButtonAnchoredPos;

    Sequence screenInitializationSequence;    void Awake()
    {
        // Get RectTransform components once
        nextButtonRect = nextPageButton.GetComponent<RectTransform>();
        prevButtonRect = prevPageButton.GetComponent<RectTransform>();
        
        // Store original anchored positions (not local positions)
        initialNextButtonAnchoredPos = nextButtonRect.anchoredPosition;
        initialPrevButtonAnchoredPos = prevButtonRect.anchoredPosition;
    }
    
    private void Start()
    {
        if (nextPageButton) nextPageButton.onClick.AddListener(NextPage);
        if (prevPageButton) prevPageButton.onClick.AddListener(PreviousPage);
    }
    
    void OnEnable()
    {
        Initialize();
    }
    void OnDisable()
    {
        foreach (var button in levelButtons)
        {
            if (button != null && button.gameObject != null)
                Destroy(button.gameObject);
        }
    }
    private void Initialize()
    {
        // Move buttons completely off screen (below)
        nextButtonRect.anchoredPosition = new Vector2(
            initialNextButtonAnchoredPos.x,
            initialNextButtonAnchoredPos.y - Screen.height  // Use screen height to ensure it's off-screen
        );
        
        prevButtonRect.anchoredPosition = new Vector2(
            initialPrevButtonAnchoredPos.x,
            initialPrevButtonAnchoredPos.y - Screen.height  // Use screen height to ensure it's off-screen
        );
        
        // Set up the initial state for the main level selection container
        levelSelectionObjectCanvasGroup.alpha = 0f;
        levelSelectionObjectTransform.localScale = Vector3.one * 0.5f;
    
        // Create animation sequence
        screenInitializationSequence = DOTween.Sequence();
        
        // Animate main container
        screenInitializationSequence.Join(levelSelectionObjectCanvasGroup.DOFade(1f, 0.5f));
        screenInitializationSequence.Join(levelSelectionObjectTransform.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
        
        // Animate buttons to their original positions using anchoredPosition
        screenInitializationSequence.Join(
            nextButtonRect.DOAnchorPos(initialNextButtonAnchoredPos, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.2f)
        );
        
        screenInitializationSequence.Join(
            prevButtonRect.DOAnchorPos(initialPrevButtonAnchoredPos, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.2f)
        );
        
        screenInitializationSequence.OnComplete(() =>
        {
            totalLevels = GameManager.Instance.TotalLevels;
            InitializeLevelButtons();
        });
    }
    public void InitializeLevelButtons()
    {
        currentPage = 1;

        // Clear any existing buttons
        ClearAllButtons();

        // Create new buttons for the first page
        StartCoroutine(CreateLevelButtons());

        // Update pagination controls
        UpdatePaginationControls();
    }

    private void UpdatePaginationControls()
    {
        int totalPages = Mathf.CeilToInt((float)totalLevels / levelsPerPage);
    
        // Enable/disable next page button
        if (nextPageButton)
            nextPageButton.gameObject.GetComponent<Button>().interactable = (currentPage < totalPages);
    
        // Enable/disable previous page button
        if (prevPageButton)
            // Fixed condition: Enable prev button if we're NOT on the first page
            prevPageButton.gameObject.GetComponent<Button>().interactable = (currentPage > 1);
    }

    public void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)totalLevels / levelsPerPage);
        if (currentPage < totalPages)
        {
            currentPage++;
            StartCoroutine(UpdateButtonsForPage());
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            StartCoroutine(UpdateButtonsForPage());
        }
    }

    private IEnumerator CreateLevelButtons()
    {
        int startLevel = (currentPage - 1) * levelsPerPage + 1;
        int endLevel = Mathf.Min(currentPage * levelsPerPage, totalLevels);

        for (int i = startLevel; i <= endLevel; i++)
        {
            // Calculate row and column (0-indexed)
            int localIndex = i - startLevel;
            int row = localIndex / columns;
            int col = localIndex % columns;

            // Determine if this is a corner button
            bool isCorner = (row == 0 && col == 0) ||            // Top-left
                           (row == 0 && col == columns - 1) ||   // Top-right
                           (row == rows - 1 && col == 0) ||      // Bottom-left
                           (row == rows - 1 && col == columns - 1); // Bottom-right

            // Instantiate the appropriate prefab
            GameObject buttonObj = Instantiate(
                isCorner ? GetCornerPrefab(row, col) : levelButtonPrefab,
                levelButtonContainer
            );

            // Get button component
            LevelSelectionButtonItem button = buttonObj.GetComponent<LevelSelectionButtonItem>();
            if (button != null)
            {
                // Calculate color index based on level number
                int colorIndex = (i - 1) % unlockedLevelButtonColors.Count;
                Color buttonColor = unlockedLevelButtonColors[colorIndex];

                // Check if level is locked
                bool isLocked = i > GameConstants.highestUnlockedLevelIndex + 1;
                Color finalColor = isLocked ? lockedButtonColor : buttonColor;

                // Initialize the button using your existing method
                button.Initialize(i, finalColor, isLocked);
                buttonSpawnAudioSource.PlayOneShot(buttonSpawnAudioClip);
                // Add to list for tracking
                levelButtons.Add(button);
            }

            // Wait for the specified delay
            yield return new WaitForSeconds(buttonSpawnDelay);
        }

    }

    private IEnumerator UpdateButtonsForPage()
    {
        int startLevel = (currentPage - 1) * levelsPerPage + 1;
        int endLevel = Mathf.Min(currentPage * levelsPerPage, totalLevels);
    
        // Calculate how many buttons we need
        int buttonsNeeded = endLevel - startLevel + 1;
            
        // First, handle visibility and animations for each button
        for (int i = 0; i < levelButtons.Count; i++)
        {
            if (i < buttonsNeeded)
            {
                // This button will be used
                int levelNumber = startLevel + i;
    
                // Calculate color index based on level number
                int colorIndex = (levelNumber - 1) % unlockedLevelButtonColors.Count;
                Color buttonColor = unlockedLevelButtonColors[colorIndex];
    
                // Check if level is locked
                bool isLocked = levelNumber > GameConstants.highestUnlockedLevelIndex + 1;
                Color finalColor = isLocked ? lockedButtonColor : buttonColor;
    
                // Update the button using SwitchLevel
                levelButtons[i].SwitchLevel(levelNumber, finalColor, isLocked);
                buttonSpawnAudioSource.PlayOneShot(buttonSpawnAudioClip);
                // Make sure the button is active
                levelButtons[i].gameObject.SetActive(true);
            }
            else
            {
                // This button is not needed for this page
                levelButtons[i].Despawn(); // Call your despawn method
            }
    
            yield return new WaitForSeconds(buttonSwitchDelay);
        }
    
        // Update pagination controls
        UpdatePaginationControls();
    }
    GameObject GetCornerPrefab(int row, int col)
    {
        if (row == 0 && col == 0) return topLeftCornerLevelButtonPrefab;
        if (row == 0 && col == columns - 1) return topRightCornerLevelButtonPrefab;
        if (row == rows - 1 && col == 0) return bottomLeftCornerLevelButtonPrefab;
        if (row == rows - 1 && col == columns - 1) return bottomRightCornerLevelButtonPrefab;
        return levelButtonPrefab; // Fallback
    }
    private void ClearAllButtons()
    {
        foreach (var button in levelButtons)
        {
            if (button != null && button.gameObject != null)
                Destroy(button.gameObject);
        }

        levelButtons.Clear();
    }
}