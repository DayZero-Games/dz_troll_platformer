using System;
using GoogleMobileAds.Api;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Core
{
    public class AdServiceProvider : IAdService, IInitializable, IDisposable

    {
        private readonly AdsSettingsSo _adsSettings;
        private readonly IIAPService _iapService;
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;

        // Callbacks
        private Action _onInterstitialClosed;
        private Action<bool> _onRewardProcessed;

        // Single gate for the whole service, so a new ad call site can't forget the check.
        private bool AdsRemoved => _iapService.IsNoAdsPurchased;

        public AdServiceProvider(AdsSettingsSo adsSettings, IIAPService iapService)
        {
            _adsSettings = adsSettings;
            _iapService = iapService;
        }
        void IInitializable.Initialize() => Initialize();

        public void Initialize()
        {
            _iapService.NoAdsChanged += OnNoAdsChanged;

            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("AdMob Initialized.");
                // Rewarded ads survive the purchase — they're opt-in and hand out rewards,
                // so they aren't what "remove ads" refers to.
                LoadRewarded();

                if (!AdsRemoved) LoadInterstitial();
            });
        }

        // Fires when the store confirms ownership a second or two after boot, and the
        // instant a purchase lands. Tears down whatever is already live.
        private void OnNoAdsChanged(bool removed)
        {
            if (!removed) return;

            _bannerView?.Destroy();
            _bannerView = null;

            _interstitialAd?.Destroy();
            _interstitialAd = null;
        }

        private AdRequest CreateAdRequest()
        {
            return new AdRequest();
        }

        #region Banner
        public void ShowBanner()
        {
            if (AdsRemoved) return;

            if (_bannerView == null)
            {
                _bannerView = new BannerView(_adsSettings.GetBannerId(), AdSize.Banner,
               AdPosition.Bottom);
            }
            _bannerView.LoadAd(CreateAdRequest());
        }
        public void HideBanner()
        {
            // Destroy rather than Hide — a hidden BannerView stays alive and keeps
            // requesting fills.
            _bannerView?.Destroy();
            _bannerView = null;
        }
        #endregion

        #region Interstitial
        public void LoadInterstitial()
        {
            if (AdsRemoved) return;

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
            // Still invoke the callback — any flow that waits on "ad closed" to continue
            // (level transitions) would hang forever otherwise.
            if (AdsRemoved)
            {
                onClosed?.Invoke();
                return;
            }

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
            _iapService.NoAdsChanged -= OnNoAdsChanged;

            _bannerView?.Destroy();
            _interstitialAd?.Destroy();
            _rewardedAd?.Destroy();
        }

    }
}
