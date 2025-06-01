using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class NoInternetScreen : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private GameObject noInternetPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueOfflineButton;
    
    [Header("Settings")]
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private string pingAddress = "8.8.8.8"; // Google DNS
    [SerializeField] private int timeout = 2; // Timeout in seconds
    
    [Header("Events")]
    [Tooltip("Called when internet connection is lost")]
    public UnityEvent OnInternetLost = new UnityEvent();
    
    [Tooltip("Called when internet connection is restored")]
    public UnityEvent OnInternetRestored = new UnityEvent();
    
    [Tooltip("Called when user chooses to continue offline")]
    public UnityEvent OnContinueOffline = new UnityEvent();
    
    private bool wasConnectedBefore = true;
    private bool isCheckingConnection = false;
    private Coroutine connectionCheckCoroutine;

    private void Start() {
        if (retryButton != null) {
            retryButton.onClick.AddListener(RetryConnection);
        }
        
        if (continueOfflineButton != null) {
            continueOfflineButton.onClick.AddListener(ContinueOffline);
        }
        
        // Hide the panel initially
        if (noInternetPanel != null) {
            noInternetPanel.SetActive(false);
        }
        
        // Start checking connection
        StartConnectionCheck();
    }
    
    private void OnEnable() {
        StartConnectionCheck();
    }
    
    private void OnDisable() {
        StopConnectionCheck();
    }
    
    private void OnDestroy() {
        StopConnectionCheck();
    }
    
    /// <summary>
    /// Starts the periodic connection check
    /// </summary>
    public void StartConnectionCheck() {
        if (!isCheckingConnection) {
            isCheckingConnection = true;
            connectionCheckCoroutine = StartCoroutine(CheckConnection());
        }
    }
    
    /// <summary>
    /// Stops the periodic connection check
    /// </summary>
    public void StopConnectionCheck() {
        if (connectionCheckCoroutine != null) {
            StopCoroutine(connectionCheckCoroutine);
            connectionCheckCoroutine = null;
        }
        isCheckingConnection = false;
    }
    
    private IEnumerator CheckConnection() {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        
        while (true) {
            bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;
            
            // If we need more accurate connection check:
            if (isConnected) {
                Ping ping = new Ping(pingAddress);
                float startTime = Time.time;
                
                // Wait for ping or timeout
                while (!ping.isDone && Time.time - startTime < timeout) {
                    yield return null;
                }
                
                isConnected = ping.isDone && ping.time >= 0;
            }
            
            // Handle connection state change
            if (!isConnected && wasConnectedBefore) {
                // Just lost connection
                ShowNoInternetPanel(true);
                OnInternetLost?.Invoke();
                Debug.Log("Internet connection lost");
            } 
            else if (isConnected && !wasConnectedBefore) {
                // Connection restored
                ShowNoInternetPanel(false);
                OnInternetRestored?.Invoke();
                Debug.Log("Internet connection restored");
            }
            
            wasConnectedBefore = isConnected;
            yield return wait;
        }
    }
    
    /// <summary>
    /// Shows or hides the no internet panel
    /// </summary>
    public void ShowNoInternetPanel(bool show) {
        if (noInternetPanel != null) {
            noInternetPanel.SetActive(show);
        }
    }
    
    /// <summary>
    /// Triggers a manual connection check when retry button is clicked
    /// </summary>
    public void RetryConnection() {
        StartCoroutine(PerformManualConnectionCheck());
    }
    
    private IEnumerator PerformManualConnectionCheck() {
        bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;
        
        if (isConnected) {
            Ping ping = new Ping(pingAddress);
            float startTime = Time.time;
            
            // Wait for ping or timeout
            while (!ping.isDone && Time.time - startTime < timeout) {
                yield return null;
            }
            
            isConnected = ping.isDone && ping.time >= 0;
        }
        
        if (isConnected) {
            ShowNoInternetPanel(false);
            OnInternetRestored?.Invoke();
            wasConnectedBefore = true;
        } else {
            // Still no connection
            Debug.Log("Retry failed - still no internet connection");
        }
    }
    
    /// <summary>
    /// Called when the user chooses to continue playing offline
    /// </summary>
    public void ContinueOffline() {
        ShowNoInternetPanel(false);
        OnContinueOffline?.Invoke();
    }
    
    /// <summary>
    /// Public method to manually check if internet is available
    /// </summary>
    public bool IsInternetAvailable() {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
}