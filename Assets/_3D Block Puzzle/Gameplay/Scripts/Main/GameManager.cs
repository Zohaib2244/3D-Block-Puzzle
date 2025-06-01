using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    [SerializeField] private LevelData levelData;
    public GameObject currentLevelPrefab;
    public LevelState currentLevelState = LevelState.None;
    public int TotalLevels => levelData.levelPrefabs.Count;
    public UnityEvent onLevelLoaded;
    void Start()
    {
        GameConstants.InitializeGame();
        onLevelLoaded.AddListener(ConfigureCamera);
    }
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelData.levelPrefabs.Count)
        {
            Debug.LogError("Invalid level index: " + levelIndex);
            return;
        }
        Destroy(currentLevelPrefab);
        currentLevelPrefab = Instantiate(levelData.levelPrefabs[levelIndex % levelData.levelPrefabs.Count]);
        currentLevelState = LevelState.InProgress;
        onLevelLoaded?.Invoke();
        FirebaseHandler.LogLevelEvent(FirebaseHandler.LevelState.Start, levelIndex + 1);
    }
    public void LoadNextLevel()
    {
        LoadLevel(GameConstants.CurrentLevelIndex % levelData.levelPrefabs.Count);
    }
    public void RetryLevel()
    {
        if (currentLevelState == LevelState.InProgress || currentLevelState == LevelState.Failed)
        {
            currentLevelState = LevelState.InProgress;
            GameUIManager.Instance.LevelScreen.StopTimer();
            Destroy(currentLevelPrefab);
            currentLevelPrefab = Instantiate(levelData.levelPrefabs[GameConstants.CurrentLevelIndex % levelData.levelPrefabs.Count]);
            onLevelLoaded?.Invoke();
        }
    }
    public void LevelFailed()
    {
        if (currentLevelState == LevelState.InProgress)
        {
            currentLevelState = LevelState.Failed;
            GameUIManager.Instance.ShowScreen(ScreenType.GameOver);
            FirebaseHandler.LogLevelEvent(FirebaseHandler.LevelState.Fail, GameConstants.CurrentLevelIndex + 1);
        }
    }
    public void LevelCompleted()
    {

        if (currentLevelState == LevelState.InProgress)
        {
            Debug.Log("Level Completed!");
            currentLevelState = LevelState.Completed;
            GameConstants.CurrentLevelIndex++;
            GameUIManager.Instance.ShowScreen(ScreenType.LevelCompleted);
            FirebaseHandler.LogLevelEvent(FirebaseHandler.LevelState.Complete, GameConstants.CurrentLevelIndex + 1);
        }
    }
    public void UnloadAllLevels()
    {
        if (currentLevelPrefab != null)
        {
            Destroy(currentLevelPrefab);
            currentLevelPrefab = null;
        }
        currentLevelState = LevelState.None;
    }

    void ConfigureCamera()
    {
        (Vector3 position, float fov) = currentLevelPrefab.GetComponent<LevelManager>().GetCameraProperties();
        Camera.main.transform.position = position;
        Camera.main.fieldOfView = fov;
    }
}