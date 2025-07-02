using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private int levelTime = 60;
    [SerializeField] private Vector3 cameraPosition;
    [SerializeField] private float cameraFOV;
    private int blockCount = 0;
    private bool _levelTimerStarted = false;

    void Start()
    {
        gridManager.onBlockRemoved.AddListener(OnBlockRemoved);
        gridManager.onGridInitialized.AddListener(InitLevel);
        GameUIManager.Instance.ShowScreen(ScreenType.GamePlay);
        SpawnLevel();
        GameUIManager.Instance.LevelScreen.InitLevelTime(levelTime);
    }
    void OnDestroy()
    {
        gridManager.onBlockRemoved.RemoveListener(OnBlockRemoved);
        gridManager.onGridInitialized.RemoveListener(InitLevel);
        gridManager.onBlockClicked.RemoveAllListeners();
    }
    void InitLevel()
    {
        this.blockCount = gridManager.GetBlockCount();
        gridManager.onBlockClicked.AddListener(() =>
        {
            if (!_levelTimerStarted)
            {
                // GameUIManager.Instance.LevelScreen.StartLevelTime(levelTime);
                GameUIManager.Instance.LevelScreen.StartTimer();
                _levelTimerStarted = true;
            }
        });
    }
    void OnBlockRemoved()
    {
        DebugLogger.Log("Block removed!", DebugColor.Orange);
        blockCount--;
        if (blockCount <= 0)
        {
            GameUIManager.Instance.LevelScreen.StopTimer();
            DespawnLevel();
            DOVirtual.DelayedCall(0.75f, () =>
            {
                GameManager.Instance.LevelCompleted();
            });
        }
    }
    void SpawnLevel()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.75f).SetEase(Ease.OutBack);
    }
    void DespawnLevel()
    {
        transform.DOScale(Vector3.zero, 0.75f).SetEase(Ease.InBack).OnComplete(() =>
        {
        });
    }
    public (Vector3 position, float fov) GetCameraProperties()
    {
        return (cameraPosition, cameraFOV);
    }
#if UNITY_EDITOR
    void GetGridReference()
    {
        if (gridManager == null)
        {
            gridManager = GetComponentInChildren<GridManager>();
            if (gridManager == null)
            {

                Debug.LogError("GridManager not found in children.");
            }
        }
    }
    void SetCamera()
    {
        cameraPosition = Camera.main.transform.position;
        cameraFOV = Camera.main.fieldOfView;

        // Mark the object as dirty to ensure Unity saves the changes
        EditorUtility.SetDirty(this);

        // If this is part of a prefab, record the modifications
        if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        // Mark the scene as dirty to ensure it gets saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
    public void ConfigureLevel()
    {
        GetGridReference();
        SetCamera();

        // If this is part of a prefab, apply changes to the prefab
        if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (prefabRoot != null)
            {
                try
                {
                    PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Failed to apply prefab instance: " + ex.Message);
                }
            }
        }
    }
    public void MoveCameraToPosition()
    {
        if (Camera.main)
        {
            Camera.main.transform.position = cameraPosition;
            Camera.main.fieldOfView = cameraFOV;

        }
    }
    public void RenameLevel(string newName)
    {
        gameObject.name = newName;
        if (gridManager != null && gridManager.SavedGridData != null)
        {
            // Rename the actual ScriptableObject asset file
            string assetPath = AssetDatabase.GetAssetPath(gridManager.SavedGridData);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string result = AssetDatabase.RenameAsset(assetPath, newName);
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogError("Error renaming asset: " + result);
                }
                else
                {
                    AssetDatabase.SaveAssets();
                }
            }
        }

        // Update the prefab if this GameObject is a prefab instance
        GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
        if (prefabRoot != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(prefabRoot);

            // If this is a prefab asset, rename the asset file as well
            Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
            if (prefabAsset != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                    if (prefabName != newName)
                    {
                        string result = AssetDatabase.RenameAsset(prefabPath, newName);
                        if (!string.IsNullOrEmpty(result))
                        {
                            Debug.LogError("Error renaming prefab asset: " + result);
                        }
                    }
                }
            }
        }
    }

#endif
}