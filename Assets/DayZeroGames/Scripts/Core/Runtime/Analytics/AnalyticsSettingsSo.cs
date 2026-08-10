using UnityEngine;

namespace DZ.Core
{
    [CreateAssetMenu(fileName = "AnalyticsSettingsSo", menuName = "DayZeroGames/AnalyticsSettingsSo")]
    public class AnalyticsSettingsSo : ScriptableObject
    {
        [Header("Configuration")]
        [Tooltip("Enable detailed debug logging for analytics events.")]
        public bool isDebugLoggingEnabled = true;
        [Tooltip("Whether analytics collection is enabled on startup.")]
        public bool isCollectionEnabled = true;

    }
}
