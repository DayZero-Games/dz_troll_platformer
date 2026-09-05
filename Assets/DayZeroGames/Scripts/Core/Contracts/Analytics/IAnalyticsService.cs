using System.Collections.Generic;
using UnityEngine;

namespace DZ.Core
{
    public interface IAnalyticsService
    {
        void Initialize();
        void LogEvent(string eventName);
        void LogEvent(string eventName, string parameterName, string parameterValue);
        void LogEvent(string eventName, string parameterName, long parameterValue);
        void LogEvent(string eventName, string parameterName, double parameterValue);
        void LogEvent(string eventName, Dictionary<string, object> parameters);
        void SetUserId(string userId);
        void SetUserProperty(string propertyName, string propertyValue);
    }
}
