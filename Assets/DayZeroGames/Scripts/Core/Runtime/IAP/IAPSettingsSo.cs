using UnityEngine;

namespace DZ.Core
{
    [CreateAssetMenu(fileName = "IAPSettings", menuName = "DayZeroGames/IAPSettings")]
    public class IAPSettingsSo : ScriptableObject
    {
        [Header("Product Ids")]
        [Tooltip("Must match the product id in Google Play Console AND App Store Connect exactly.")]
        public string noAdsProductId = "com.dayzerogames.trollplatformer.noads";

        [Header("Editor / Debug")]
        public bool isDebugLoggingEnabled = true;
    }
}
