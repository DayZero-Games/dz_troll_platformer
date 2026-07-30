using UnityEngine;

namespace DZ.Core
{
    public interface IPlayerPrefsSaveService
    {
        void SaveFloat(string key, float value);
        float LoadFloat(string key, float defaultValue = 0f);
        
        void SaveInt(string key, int value);
        int LoadInt(string key, int defaultValue = 0);
        
        void SaveBool(string key, bool value);
        bool LoadBool(string key, bool defaultValue = false);
        
        void SaveString(string key, string value);
        string LoadString(string key, string defaultValue = "");
        
        bool HasKey(string key);
        void DeleteKey(string key);
    }
}
