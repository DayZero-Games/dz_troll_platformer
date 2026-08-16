using System;
using DZ.Core.Contracts;
using GoogleMobileAds.Api;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Core
{
    public class AdServiceProvider : IAdService, IInitializable, IDisposable
    {
        private readonly AdsSettingsSo _adsSettings;
        private readonly IAudioService _audioService;
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;

        private Action _onInterstitialClosed;
        private Action<bool> _onRewardProcessed;

        public AdServiceProvider(AdsSettingsSo adsSettings, IAudioService audioService)
        {
            _adsSettings = adsSettings;
            _audioService = audioService;
        }

        void IInitializable.Initialize() => Initialize();

        public void Initialize()
        {
            MobileAds.Initialize(initStatus =>
            {
                LoadRewarded();
                LoadInterstitial();
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
                _bannerView = new BannerView(_adsSettings.GetBannerId(), AdSize.Banner, AdPosition.Bottom);
            }

            _bannerView.LoadAd(CreateAdRequest());
        }

        public void HideBanner()
        {
            _bannerView?.Destroy();
            _bannerView = null;
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
                    _interstitialAd.OnAdFullScreenContentOpened += _audioService.PauseMusicForAd;
                    _interstitialAd.OnAdFullScreenContentClosed += CompleteInterstitial;
                    _interstitialAd.OnAdFullScreenContentFailed += _ => CompleteInterstitial();
                });
        }

        private void CompleteInterstitial()
        {
            _audioService.ResumeMusicAfterAd();

            var onClosed = _onInterstitialClosed;
            _onInterstitialClosed = null;
            onClosed?.Invoke();

            LoadInterstitial();
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
                onClosed?.Invoke();
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
                    _rewardedAd.OnAdFullScreenContentOpened += _audioService.PauseMusicForAd;
                    _rewardedAd.OnAdFullScreenContentClosed += CompleteRewarded;
                    _rewardedAd.OnAdFullScreenContentFailed += _ => CompleteRewarded();
                });
        }

        private void CompleteRewarded()
        {
            _audioService.ResumeMusicAfterAd();
            LoadRewarded();
        }

        public bool IsRewardedReady() => _rewardedAd != null && _rewardedAd.CanShowAd();

        public void ShowRewarded(Action<bool> onRewardProcessed)
        {
            if (IsRewardedReady())
            {
                _onRewardProcessed = onRewardProcessed;
                _rewardedAd.Show(reward =>
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
