using System;
using UnityEngine;

namespace DZ.Core
{
    public interface IAdService
    {
        void Initialize();
        // Banner
        void ShowBanner();
        void HideBanner();
        // Interstitial
        void LoadInterstitial();
        bool IsInterstitialReady();
        void ShowInterstitial(Action onClosed = null);
        // Rewarded
        void LoadRewarded();
        bool IsRewardedReady();
        void ShowRewarded(Action<bool> onRewardProcessed);
    }
}
