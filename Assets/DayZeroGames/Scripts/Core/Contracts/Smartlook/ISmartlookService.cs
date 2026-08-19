using System.Collections.Generic;

namespace DZ.Core
{
    public enum SmartlookNavigationEventType
    {
        Enter,
        Exit
    }

    public interface ISmartlookService
    {
        void Initialize();
        void StartRecording();
        void StopRecording();
        bool IsRecording();
        void TrackCustomEvent(string eventName);
        void TrackCustomEvent(string eventName, Dictionary<string, object> properties);
        void TrackNavigationEvent(string screenName, SmartlookNavigationEventType eventType);
        void SetUserIdentifier(string userIdentifier);
        void SetUserProperty(string propertyName, string propertyValue);
    }
}
