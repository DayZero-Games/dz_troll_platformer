using UnityEngine;

namespace DZ.Core
{
    using System;
    using System.Collections.Generic;
    using Firebase;
    using Firebase.Analytics;
    using Firebase.Extensions;
    using VContainer.Unity;

    public class AnalyticsServiceProvider : IAnalyticsService, IInitializable, IDisposable
    {
        private readonly AnalyticsSettingsSo _analyticsSettings;
        private bool _isInitialized;
        private bool _isInitializing;
        private readonly Queue<Action> _pendingEvents = new Queue<Action>();

        public AnalyticsServiceProvider(AnalyticsSettingsSo settings)
        {
            _analyticsSettings = settings;
        }

        public void Initialize()
        {
            if (_isInitialized || _isInitializing) return;
            _isInitializing = true;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                _isInitializing = false;

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"Firebase dependency check failed: {task.Exception}");
                    ClearPendingEvents();
                    return;
                }

                var dependencyStatus = task.Result;
                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies:{dependencyStatus}");
                    ClearPendingEvents();
                    return;
                }

                FirebaseAnalytics.SetAnalyticsCollectionEnabled(_analyticsSettings.isCollectionEnabled);
                _isInitialized = true;
                LogDebug($"Firebase Analytics ready (collection enabled: {_analyticsSettings.isCollectionEnabled})");

                lock (_pendingEvents)
                {
                    while (_pendingEvents.Count > 0)
                    {
                        var action = _pendingEvents.Dequeue();
                        action?.Invoke();
                    }
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
                        firebaseParameters.Add(new Parameter(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
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

        private void ClearPendingEvents()
        {
            lock (_pendingEvents)
            {
                _pendingEvents.Clear();
            }
        }

        private void LogDebug(string message)
        {
            if (_analyticsSettings != null && _analyticsSettings.isDebugLoggingEnabled)
                Debug.Log($"[Analytics] {message}");
        }

        public void Dispose()
        {
        }
    }
}
