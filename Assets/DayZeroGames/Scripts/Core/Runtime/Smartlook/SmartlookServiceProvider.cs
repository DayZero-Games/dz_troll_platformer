namespace DZ.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using SmartlookUnity;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using VContainer.Unity;

    public class SmartlookServiceProvider : ISmartlookService, IInitializable, IDisposable
    {
        private readonly Dictionary<string, string> _userProperties = new Dictionary<string, string>();
        private bool _isInitialized;
        private string _userIdentifier;

        public void Initialize()
        {
            if (_isInitialized) return;

            if (!IsSupportedRuntime())
            {
                return;
            }

            var settings = Settings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("Smartlook settings asset is missing. Open Smartlook/Edit Settings to create it.");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.ProjectKey))
            {
                Debug.LogWarning("Smartlook project key is missing. Open Smartlook/Edit Settings and set Project Key.");
                return;
            }

            try
            {
                var setupOptions = new SetupOptionsBuilder(settings.ProjectKey)
                    .SetFps(Mathf.Clamp(settings.FPS, 1, 30))
                    .Build();

                Smartlook.SetupAndStartRecording(setupOptions);
                _isInitialized = true;

                Smartlook.SetGlobalEventProperty("app_version", Application.version, true);
                Smartlook.SetGlobalEventProperty("unity_version", Application.unityVersion, true);

                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;

                ApplyUserIdentifier();
            }
            catch (Exception exception)
            {
                _isInitialized = false;
                Debug.LogWarning($"Smartlook initialization failed: {exception.Message}");
            }
        }

        void IInitializable.Initialize() => Initialize();

        public void StartRecording()
        {
            if (!_isInitialized) return;
            Smartlook.StartRecording();
        }

        public void StopRecording()
        {
            if (!_isInitialized) return;
            Smartlook.StopRecording();
        }

        public bool IsRecording()
        {
            return _isInitialized && Smartlook.IsRecording();
        }

        public void TrackCustomEvent(string eventName)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName)) return;

            try
            {
                Smartlook.TrackCustomEvent(eventName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Smartlook event '{eventName}' failed: {exception.Message}");
            }
        }

        public void TrackCustomEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(eventName)) return;

            try
            {
                var json = ToJsonObject(properties);
                if (string.IsNullOrEmpty(json))
                    Smartlook.TrackCustomEvent(eventName);
                else
                    Smartlook.TrackCustomEvent(eventName, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Smartlook event '{eventName}' failed: {exception.Message}");
            }
        }

        public void TrackNavigationEvent(string screenName, SmartlookNavigationEventType eventType)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(screenName)) return;

            try
            {
                Smartlook.TrackNavigationEvent(screenName, ToSmartlookNavigationEventType(eventType));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Smartlook navigation event '{screenName}' failed: {exception.Message}");
            }
        }

        public void SetUserIdentifier(string userIdentifier)
        {
            _userIdentifier = userIdentifier;
            ApplyUserIdentifier();
        }

        public void SetUserProperty(string propertyName, string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            _userProperties[propertyName] = propertyValue ?? string.Empty;
            ApplyUserIdentifier();
        }

        private void ApplyUserIdentifier()
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(_userIdentifier)) return;

            try
            {
                if (_userProperties.Count == 0)
                    Smartlook.SetUserIdentifier(_userIdentifier);
                else
                    Smartlook.SetUserIdentifier(_userIdentifier, ToJsonObject(_userProperties));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Smartlook user identifier update failed: {exception.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TrackNavigationEvent(scene.name, SmartlookNavigationEventType.Enter);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            TrackNavigationEvent(scene.name, SmartlookNavigationEventType.Exit);
        }

        private static bool IsSupportedRuntime()
        {
            return Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }

        private static Smartlook.NavigationEventType ToSmartlookNavigationEventType(SmartlookNavigationEventType eventType)
        {
            return eventType == SmartlookNavigationEventType.Exit
                ? Smartlook.NavigationEventType.exit
                : Smartlook.NavigationEventType.enter;
        }

        private static string ToJsonObject(Dictionary<string, object> properties)
        {
            if (properties == null || properties.Count == 0) return string.Empty;

            var builder = new StringBuilder();
            builder.Append('{');
            var hasProperties = false;

            foreach (var kvp in properties)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                if (hasProperties) builder.Append(',');
                AppendJsonString(builder, kvp.Key);
                builder.Append(':');
                AppendJsonValue(builder, kvp.Value);
                hasProperties = true;
            }

            builder.Append('}');
            return hasProperties ? builder.ToString() : string.Empty;
        }

        private static string ToJsonObject(Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0) return string.Empty;

            var builder = new StringBuilder();
            builder.Append('{');
            var hasProperties = false;

            foreach (var kvp in properties)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                if (hasProperties) builder.Append(',');
                AppendJsonString(builder, kvp.Key);
                builder.Append(':');
                AppendJsonString(builder, kvp.Value ?? string.Empty);
                hasProperties = true;
            }

            builder.Append('}');
            return hasProperties ? builder.ToString() : string.Empty;
        }

        private static void AppendJsonValue(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    AppendJsonString(builder, string.Empty);
                    return;
                case string stringValue:
                    AppendJsonString(builder, stringValue);
                    return;
                case bool boolValue:
                    builder.Append(boolValue ? "true" : "false");
                    return;
                case int intValue:
                    builder.Append(intValue.ToString(CultureInfo.InvariantCulture));
                    return;
                case long longValue:
                    builder.Append(longValue.ToString(CultureInfo.InvariantCulture));
                    return;
                case float floatValue:
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        AppendJsonString(builder, floatValue.ToString(CultureInfo.InvariantCulture));
                    else
                        builder.Append(floatValue.ToString(CultureInfo.InvariantCulture));
                    return;
                case double doubleValue:
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        AppendJsonString(builder, doubleValue.ToString(CultureInfo.InvariantCulture));
                    else
                        builder.Append(doubleValue.ToString(CultureInfo.InvariantCulture));
                    return;
                default:
                    AppendJsonString(builder, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                    return;
            }
        }

        private static void AppendJsonString(StringBuilder builder, string value)
        {
            builder.Append('"');

            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }

            builder.Append('"');
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
}
