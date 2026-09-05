using System;
using UnityEngine;

namespace DZ.Core
{
    public interface IAdService
    {
        void Initialize();
        
        void ShowBanner();
        void HideBanner();
        
        void LoadInterstitial();
        bool IsInterstitialReady();
        void ShowInterstitial(Action onClosed = null);
        
        void LoadRewarded();
        bool IsRewardedReady();
        void ShowRewarded(Action<bool> onRewardProcessed);
    }
}
