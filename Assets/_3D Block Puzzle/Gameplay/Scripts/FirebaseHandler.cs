using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;

public static class FirebaseHandler
{
    public enum LevelState
    {
        Start,
        Complete,
        Fail
    }
    
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                // Firebase is ready to use
                isInitialized = true;                
                // Enable analytics data collection (Firebase automatically tracks sessions)
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                
                // Set user properties that will be included with all events
                FirebaseAnalytics.SetUserProperty("app_version", Application.version);
                FirebaseAnalytics.SetUserProperty("platform", Application.platform.ToString());
                FirebaseAnalytics.SetUserProperty("device_model", SystemInfo.deviceModel);
                
                Debug.Log("Firebase Analytics initialized successfully");
            }
            else
            {
                Debug.LogError("Failed to initialize Firebase Analytics: " + task.Result.ToString());
            }
        });
    }

    #region Analytics Events

        public static void LogLevelEvent(LevelState state, int levelNumber, float timeSpent = 0, string failReason = "time_up")
    {
        if (!isInitialized) return;
        
        try
        {
            string eventName;
            List<Parameter> parameters = new List<Parameter>
            {
                new Parameter(FirebaseAnalytics.ParameterLevelName, $"level_{levelNumber}"),
                new Parameter("level_number", levelNumber)
            };
            
            switch(state)
            {
                case LevelState.Start:
                    eventName = FirebaseAnalytics.EventLevelStart;
                    break;
                    
                case LevelState.Complete:
                    eventName = "level_end";
                    parameters.Add(new Parameter("time_spent", timeSpent));
                    break;
                    
                case LevelState.Fail:
                    eventName = "level_failed";
                    parameters.Add(new Parameter("time_spent", timeSpent));
                    parameters.Add(new Parameter("failure_reason", failReason));
                    break;
                    
                default:
                    Debug.LogError($"Firebase: Unknown level state: {state}");
                    return;
            }
            
            FirebaseAnalytics.LogEvent(eventName, parameters.ToArray());
            Debug.Log($"Firebase: Level {levelNumber} {state.ToString().ToLower()} event logged");
        }
        catch (Exception e)
        {
            Debug.LogError($"Firebase: Failed to log level {state.ToString().ToLower()} event: {e.Message}");
        }
    }
    public static void LogCustomEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!isInitialized) return;
        
        try
        {
            // Create a list to store parameters
            List<Parameter> parametersList = new List<Parameter>();
            
            // Add any provided custom parameters
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (param.Value is string stringValue)
                        parametersList.Add(new Parameter(param.Key, stringValue));
                    else if (param.Value is int intValue)
                        parametersList.Add(new Parameter(param.Key, intValue));
                    else if (param.Value is long longValue)
                        parametersList.Add(new Parameter(param.Key, longValue));
                    else if (param.Value is double doubleValue)
                        parametersList.Add(new Parameter(param.Key, doubleValue));
                    else if (param.Value is float floatValue)
                        parametersList.Add(new Parameter(param.Key, floatValue));
                }
            }
            
            FirebaseAnalytics.LogEvent(eventName, parametersList.ToArray());
            Debug.Log($"Firebase: Custom event '{eventName}' logged");
        }
        catch (Exception e)
        {
            Debug.LogError($"Firebase: Failed to log custom event: {e.Message}");
        }
    }

    #endregion
}