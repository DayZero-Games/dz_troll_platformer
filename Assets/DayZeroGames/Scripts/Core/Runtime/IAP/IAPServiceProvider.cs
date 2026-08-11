using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using VContainer.Unity;

namespace DZ.Core
{
    public class IAPServiceProvider : IIAPService, IInitializable, IDisposable
    {
        private readonly IAPSettingsSo _settings;
        private readonly IPlayerPrefsSaveService _save;

        private StoreController _store;
        private Product _noAdsProduct;

        private bool _isNoAdsPurchased;
        private bool _isStoreReady;
        private bool _isPurchaseInFlight;

        public bool IsStoreReady => _isStoreReady;
        public bool IsNoAdsPurchased => _isNoAdsPurchased;

        public event Action<bool> NoAdsChanged;
        public event Action<IAPPurchaseResult> PurchaseCompleted;
        public event Action StoreReady;

        public IAPServiceProvider(IAPSettingsSo settings, IPlayerPrefsSaveService save)
        {
            _settings = settings;
            _save = save;

            // Read the cache synchronously. The menu needs an answer on frame 1;
            // the store's answer arrives a second or two later and wins.
            _isNoAdsPurchased = _save.LoadBool(SaveKeys.NoAdsPurchased, false);
        }

        void IInitializable.Initialize() => InitializeAsync().Forget();

        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                _store = UnityIAPServices.StoreController();

                // Subscribe before Connect() — IAP warns if these two are missing at connect time.
                _store.OnStoreConnected += OnStoreConnected;
                _store.OnStoreDisconnected += OnStoreDisconnected;

                _store.OnProductsFetched += OnProductsFetched;
                _store.OnProductsFetchFailed += OnProductsFetchFailed;
                _store.OnPurchasePending += OnPurchasePending;
                _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                _store.OnPurchaseFailed += OnPurchaseFailed;
                _store.OnCheckEntitlement += OnCheckEntitlement;

                await _store.Connect();

                _store.FetchProducts(new List<ProductDefinition>
                {
                    new ProductDefinition(_settings.noAdsProductId, ProductType.NonConsumable)
                });
            }
            catch (Exception e)
            {
                // Store unreachable. We keep running on the cached entitlement —
                // never downgrade a paying player because the network was down.
                Log($"Store connect failed: {e.Message}. Running on cached entitlement ({_isNoAdsPurchased}).");
            }
        }

        #region Store readiness

        private void OnStoreConnected() => Log("Store connected.");

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            // Refuse purchase attempts until products are fetched again. The entitlement
            // we already resolved stays put — losing the connection is not a refund.
            _isStoreReady = false;
            Log($"Store disconnected: {failure.Message} (retryable: {failure.IsRetryable})");
        }

        private void OnProductsFetched(List<Product> products)
        {
            _noAdsProduct = products.FirstOrDefault(p => p.definition.id == _settings.noAdsProductId);

            if (_noAdsProduct == null)
            {
                Log($"Store returned no product '{_settings.noAdsProductId}'. " +
                    "Check the id in the store console and that the product is ACTIVE.");
                return;
            }

            _isStoreReady = true;
            StoreReady?.Invoke();

            // The ownership question — answered by the store, not by us.
            _store.CheckEntitlement(_noAdsProduct);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Log($"Product fetch failed: {failure.FailureReason}. Keeping cached entitlement.");
        }

        #endregion

        #region Entitlement — "does this user already own no-ads?"

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            if (entitlement.Product == null ||
                entitlement.Product.definition.id != _settings.noAdsProductId)
                return;

            switch (entitlement.Status)
            {
                case EntitlementStatus.FullyEntitled:
                    SetNoAds(true);
                    break;

                case EntitlementStatus.EntitledButNotFinished:
                    // They paid, but the transaction was never confirmed — app killed
                    // mid-purchase, most likely. Grant first, then close the loop.
                    SetNoAds(true);
                    if (entitlement.Order is PendingOrder pending)
                        _store.ConfirmPurchase(pending);
                    break;

                case EntitlementStatus.NotEntitled:
                    // Authoritative "no" — safe to clear a stale local cache
                    // (e.g. a refund, or a device restored from someone else's backup).
                    SetNoAds(false);
                    break;

                case EntitlementStatus.Unknown:
                    // The QUERY failed. This is not a "no". Treating it as one is how you
                    // show ads to someone who paid you. Keep whatever we already had.
                    Log($"Entitlement unknown ({entitlement.ErrorMessage}). " +
                        $"Keeping cached value: {_isNoAdsPurchased}.");
                    break;
            }
        }

        private void SetNoAds(bool value)
        {
            if (_isNoAdsPurchased == value) return;

            _isNoAdsPurchased = value;
            _save.SaveBool(SaveKeys.NoAdsPurchased, value);
            NoAdsChanged?.Invoke(value);

            Log($"No-ads entitlement is now {value}.");
        }

        #endregion

        #region Purchase

        public void PurchaseNoAds()
        {
            if (_isNoAdsPurchased)
            {
                PurchaseCompleted?.Invoke(IAPPurchaseResult.AlreadyOwned);
                return;
            }

            if (!_isStoreReady || _noAdsProduct == null)
            {
                PurchaseCompleted?.Invoke(IAPPurchaseResult.StoreNotReady);
                return;
            }

            if (_isPurchaseInFlight) return; // double-tap guard

            _isPurchaseInFlight = true;
            _store.PurchaseProduct(_noAdsProduct);
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (!IsNoAdsOrder(order)) return;

            // Grant BEFORE confirming. If the process dies in between, next boot's
            // entitlement check reports EntitledButNotFinished and recovers.
            SetNoAds(true);
            _store.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (!IsNoAdsOrder(order)) return;

            _isPurchaseInFlight = false;

            if (order is FailedOrder failed)
            {
                // Payment went through; only *finishing* the transaction failed.
                // We keep the grant — next launch re-resolves it properly.
                Log($"Confirm failed: {failed.FailureReason} — {failed.Details}");
                PurchaseCompleted?.Invoke(IAPPurchaseResult.Failed);
                return;
            }

            PurchaseCompleted?.Invoke(IAPPurchaseResult.Success);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            if (!IsNoAdsOrder(order)) return;

            _isPurchaseInFlight = false;

            PurchaseCompleted?.Invoke(order.FailureReason == PurchaseFailureReason.UserCancelled
                ? IAPPurchaseResult.Cancelled
                : IAPPurchaseResult.Failed);
        }

        public void RestorePurchases()
        {
            if (_store == null || !_isStoreReady)
            {
                PurchaseCompleted?.Invoke(IAPPurchaseResult.StoreNotReady);
                return;
            }

            _store.RestoreTransactions((success, error) =>
            {
                Log($"Restore finished. success={success} error={error}");

                if (success && _noAdsProduct != null)
                    _store.CheckEntitlement(_noAdsProduct); // re-resolve ownership
                else if (!success)
                    PurchaseCompleted?.Invoke(IAPPurchaseResult.Failed);
            });
        }

        private bool IsNoAdsOrder(Order order)
        {
            var item = order?.CartOrdered?.Items()?.FirstOrDefault();
            return item?.Product?.definition?.id == _settings.noAdsProductId;
        }

        #endregion

        private void Log(string message)
        {
            if (_settings.isDebugLoggingEnabled) Debug.Log($"[IAP] {message}");
        }

        public void Dispose()
        {
            if (_store == null) return;

            _store.OnStoreConnected -= OnStoreConnected;
            _store.OnStoreDisconnected -= OnStoreDisconnected;

            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnPurchaseFailed -= OnPurchaseFailed;
            _store.OnCheckEntitlement -= OnCheckEntitlement;
        }
    }
}
