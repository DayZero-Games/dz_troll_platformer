using System;

namespace DZ.Core
{
    public enum IAPPurchaseResult
    {
        Success,
        AlreadyOwned,
        Cancelled,
        Failed,
        StoreNotReady
    }

    public interface IIAPService
    {
        bool IsStoreReady { get; }
        bool IsNoAdsPurchased { get; }

        event Action<bool> NoAdsChanged;
        event Action<IAPPurchaseResult> PurchaseCompleted;
        event Action StoreReady;

        void PurchaseNoAds();
        void RestorePurchases();
    }
}
