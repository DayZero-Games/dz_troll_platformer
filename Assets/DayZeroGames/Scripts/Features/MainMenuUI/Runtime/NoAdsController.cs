using System;
using DZ.Core;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Features
{
    public class NoAdsController : IStartable, IDisposable
    {
        private readonly MainPanelView _view;
        private readonly IIAPService _iap;

        public NoAdsController(MainPanelView view, IIAPService iap)
        {
            _view = view;
            _iap = iap;
        }

        public void Start()
        {
            if (_view.NoAdsButton != null)
                _view.NoAdsButton.onClick.AddListener(OnNoAdsClicked);

            if (_view.RestorePurchasesButton != null)
                _view.RestorePurchasesButton.onClick.AddListener(OnRestoreClicked);

            _iap.NoAdsChanged += ApplyOwnedState;
            _iap.StoreReady += OnStoreReady;
            _iap.PurchaseCompleted += OnPurchaseCompleted;

            // Cached value on a cold start, the store's real answer if it already replied.
            ApplyOwnedState(_iap.IsNoAdsPurchased);

            // Nothing to tap until the store hands us a real product. Staying disabled
            // while offline is the correct outcome — there's no purchase to be made.
            _view.SetNoAdsInteractable(_iap.IsStoreReady);
        }

        // Straight to the Google Play / App Store purchase sheet. That native sheet is
        // the storefront — product name, price and payment method all come from it.
        private void OnNoAdsClicked()
        {
            _view.SetNoAdsInteractable(false); // double-tap guard while the sheet opens
            _iap.PurchaseNoAds();
        }

        private void OnRestoreClicked()
        {
            _view.SetNoAdsInteractable(false);
            _iap.RestorePurchases();
        }

        // The store now has a real product, so the button becomes tappable.
        private void OnStoreReady() => _view.SetNoAdsInteractable(!_iap.IsNoAdsPurchased);

        // Fires from the entitlement check at boot AND from a fresh purchase.
        private void ApplyOwnedState(bool owned) => _view.SetNoAdsButtonVisible(!owned);

        private void OnPurchaseCompleted(IAPPurchaseResult result)
        {
            // Success hides the button through NoAdsChanged. Every other outcome —
            // cancelled, declined card, store unreachable — hands control back.
            if (result != IAPPurchaseResult.Success)
                _view.SetNoAdsInteractable(true);

            if (result == IAPPurchaseResult.Failed || result == IAPPurchaseResult.StoreNotReady)
                Debug.Log($"[IAP] No-ads purchase did not complete: {result}");
        }

        public void Dispose()
        {
            if (_view.NoAdsButton != null)
                _view.NoAdsButton.onClick.RemoveListener(OnNoAdsClicked);

            if (_view.RestorePurchasesButton != null)
                _view.RestorePurchasesButton.onClick.RemoveListener(OnRestoreClicked);

            _iap.NoAdsChanged -= ApplyOwnedState;
            _iap.StoreReady -= OnStoreReady;
            _iap.PurchaseCompleted -= OnPurchaseCompleted;
        }
    }
}
