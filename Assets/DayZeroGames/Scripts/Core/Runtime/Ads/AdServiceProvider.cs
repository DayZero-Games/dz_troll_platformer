using System;
using GoogleMobileAds.Api;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Core
{
    public class AdServiceProvider : IAdService, IInitializable, IDisposable

    {
        private readonly AdsSettingsSo _adsSettings;
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;

        // Callbacks
        private Action _onInterstitialClosed;
        private Action<bool> _onRewardProcessed;

        public AdServiceProvider(AdsSettingsSo adsSettings)
        {
            _adsSettings = adsSettings;
        }
        void IInitializable.Initialize() => Initialize();

        public void Initialize()
        {
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("AdMob Initialized.");
                // Preload ads once SDK is ready
                LoadInterstitial();
                LoadRewarded();
            });
        }

        private AdRequest CreateAdRequest()
        {
            return new AdRequest();
        }

        #region Banner
        public void ShowBanner()
        {
            if (_bannerView == null)
            {
                _bannerView = new BannerView(_adsSettings.GetBannerId(), AdSize.Banner,
               AdPosition.Bottom);
            }
            _bannerView.LoadAd(CreateAdRequest());
        }
        public void HideBanner()
        {
            _bannerView?.Hide();
        }
        #endregion

        #region Interstitial
        public void LoadInterstitial()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
            InterstitialAd.Load(_adsSettings.GetInterstitialId(), CreateAdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null) return;
                _interstitialAd = ad;
                _interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    _onInterstitialClosed?.Invoke();
                    LoadInterstitial(); // Auto-reload next ad
                };
            });
        }
        public bool IsInterstitialReady() => _interstitialAd != null && _interstitialAd.CanShowAd();
        public void ShowInterstitial(Action onClosed = null)
        {
            if (IsInterstitialReady())
            {
                _onInterstitialClosed = onClosed;
                _interstitialAd.Show();
            }
            else
            {
                onClosed?.Invoke(); // Fallback if ad isn't ready
            }
        }
        #endregion

        #region Rewarded
        public void LoadRewarded()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
            RewardedAd.Load(_adsSettings.GetRewardedId(), CreateAdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null) return;
                _rewardedAd = ad;
                _rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    LoadRewarded();
                };
            });
        }
        public bool IsRewardedReady() => _rewardedAd != null && _rewardedAd.CanShowAd();
        public void ShowRewarded(Action<bool> onRewardProcessed)
        {
            if (IsRewardedReady())
            {
                _onRewardProcessed = onRewardProcessed;
                _rewardedAd.Show((Reward reward) =>
                {
                    _onRewardProcessed?.Invoke(true);
                    _onRewardProcessed = null; 
                });
                
            }
            else
            {
                onRewardProcessed?.Invoke(false);
            }
        }
        #endregion
        public void Dispose()
        {
            _bannerView?.Destroy();
            _interstitialAd?.Destroy();
            _rewardedAd?.Destroy();
        }

    }
}
