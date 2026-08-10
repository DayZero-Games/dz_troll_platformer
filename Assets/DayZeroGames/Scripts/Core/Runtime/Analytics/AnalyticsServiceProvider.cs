using UnityEngine;

namespace DZ.Core
{
    using System;
    using System.Collections.Generic;
    using Firebase;
    using Firebase.Analytics;
    using UnityEngine;
    using VContainer.Unity;
    public class AnalyticsServiceProvider : IAnalyticsService, IInitializable, IDisposable
    {
        private readonly AnalyticsSettingsSo _analyticsSettings;
        private bool _isInitialized;
        private readonly Queue<Action> _pendingEvents = new Queue<Action>();
        public AnalyticsServiceProvider(AnalyticsSettingsSo settings)
        {
            _analyticsSettings = settings;
        }
        public void Initialize()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    FirebaseApp app = FirebaseApp.DefaultInstance;
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(_analyticsSettings.isCollectionEnabled);
                    _isInitialized = true;


                    if (_analyticsSettings.isDebugLoggingEnabled)
                    {
                        Debug.Log("Firebase Analytics initialized successfully.");
                    }
                    // Flush pending events queued during initialization
                    lock (_pendingEvents)
                    {
                        while (_pendingEvents.Count > 0)
                        {
                            var action = _pendingEvents.Dequeue();
                            action?.Invoke();
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies:{dependencyStatus}");

                }
            });
        }
        void IInitializable.Initialize() => Initialize();
        public void LogEvent(string eventName)
        {
            ExecuteOrQueue(() =>
            {
                FirebaseAnalytics.LogEvent(eventName);
                LogDebug($"Event Logged: {eventName}");
            });
        }
        public void LogEvent(string eventName, string parameterName, string parameterValue)
        {
            ExecuteOrQueue(() =>
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
                LogDebug($"Event Logged: {eventName} | {parameterName} = {parameterValue}");
            });
        }
        public void LogEvent(string eventName, string parameterName, long parameterValue)
        {
            ExecuteOrQueue(() =>
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
                LogDebug($"Event Logged: {eventName} | {parameterName} = {parameterValue}");
            });
        }
        public void LogEvent(string eventName, string parameterName, double parameterValue)
        {
            ExecuteOrQueue(() =>
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
                LogDebug($"Event Logged: {eventName} | {parameterName} = {parameterValue}");
            });
        }
        public void LogEvent(string eventName, Dictionary<string, object> parameters)

        {
            ExecuteOrQueue(() =>
            {
                if (parameters == null || parameters.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventName);
                    return;
                }
                List<Parameter> firebaseParameters = new List<Parameter>();
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is long lVal)
                        firebaseParameters.Add(new Parameter(kvp.Key, lVal));
                    else if (kvp.Value is int iVal)
                        firebaseParameters.Add(new Parameter(kvp.Key, iVal));
                    else if (kvp.Value is double dVal)
                        firebaseParameters.Add(new Parameter(kvp.Key, dVal));
                    else if (kvp.Value is float fVal)
                        firebaseParameters.Add(new Parameter(kvp.Key, (double)fVal));
                    else
                        firebaseParameters.Add(new Parameter(kvp.Key, kvp.Value?.ToString() ??
            string.Empty));
                }
                FirebaseAnalytics.LogEvent(eventName, firebaseParameters.ToArray());
                LogDebug($"Event Logged: {eventName} with {parameters.Count} parameters.");
            });
        }
        public void SetUserId(string userId)
        {
            ExecuteOrQueue(() =>
            {
                FirebaseAnalytics.SetUserId(userId);
                LogDebug($"User ID set: {userId}");
            });
        }
        public void SetUserProperty(string propertyName, string propertyValue)
        {
            ExecuteOrQueue(() =>
            {
                FirebaseAnalytics.SetUserProperty(propertyName, propertyValue);
                LogDebug($"User Property set: {propertyName} = {propertyValue}");
            });
        }
        private void ExecuteOrQueue(Action action)
        {
            if (_isInitialized)
            {
                action?.Invoke();
            }
            else
            {
                lock (_pendingEvents)
                {
                    _pendingEvents.Enqueue(action);
                }
            }
        }

        private void LogDebug(string message)
        {
            if (_analyticsSettings.isDebugLoggingEnabled)
            {
                Debug.Log($"[Analytics] {message}");
            }
        }
        public void Dispose()
        {
            // Cleanup if required
        }
    }

}
