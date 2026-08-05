using UnityEngine;

namespace DZ.Core
{
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "DayZeroGames/AdsSettings")]
    public class AdsSettingsSo : ScriptableObject
    {
        [Header("Global Settings")]
        public bool isTestMode = true;
        [Header("Android Ad Units")]
        public string androidBannerId = "ca-app-pub-3940256099942544/6300978111";
        public string androidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public string androidRewardedId = "ca-app-pub-3940256099942544/5224354917";
        [Header("iOS Ad Units")]
        public string iosBannerId = "ca-app-pub-3940256099942544/2934735716";
        public string iosInterstitialId = "ca-app-pub-3940256099942544/4411468910";
        public string iosRewardedId = "ca-app-pub-3940256099942544/1712485313";
        public string GetBannerId() => Application.platform == RuntimePlatform.IPhonePlayer ?
       iosBannerId : androidBannerId;
        public string GetInterstitialId() => Application.platform == RuntimePlatform.IPhonePlayer ?
       iosInterstitialId : androidInterstitialId;
        public string GetRewardedId() => Application.platform == RuntimePlatform.IPhonePlayer ?
       iosRewardedId : androidRewardedId;

    }
}
