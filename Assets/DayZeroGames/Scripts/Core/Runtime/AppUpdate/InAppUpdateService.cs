using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

namespace DZ.Core.Runtime
{
    public sealed class InAppUpdateService : IAsyncStartable
    {
        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                await CheckForUpdate(cancellation);
            }
            catch (OperationCanceledException)
            {
                // Scope torn down mid-check — nothing to report.
            }
            catch (Exception e)
            {
                Debug.LogError($"[InAppUpdate] Update check threw: {e}");
            }
#else
            Debug.Log("[InAppUpdate] Skipped — Play in-app updates only run on an Android device.");
            await UniTask.CompletedTask;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private async UniTask CheckForUpdate(CancellationToken cancellation)
        {
            var appUpdateManager = new AppUpdateManager();

            // ── 1. Request update info from Google Play ──────────────────────
            Debug.Log("[InAppUpdate] Requesting update info from Google Play...");
            var appUpdateInfoOp = appUpdateManager.GetAppUpdateInfo();
            await appUpdateInfoOp.ToUniTask(cancellation);

            if (!appUpdateInfoOp.IsSuccessful)
            {
                // ErrorAppNotOwned / ErrorApiNotAvailable here almost always means the build
                // was not installed by the Play Store (sideloaded / adb install).
                Debug.LogWarning($"[InAppUpdate] Failed to fetch update info: {appUpdateInfoOp.Error}");
                return;
            }

            var appUpdateInfo = appUpdateInfoOp.GetResult();
            Debug.Log($"[InAppUpdate] Play responded: {appUpdateInfo}");

            // ── 2. Is an update available & allowed as Flexible? ─────────────
            if (appUpdateInfo.UpdateAvailability != UpdateAvailability.UpdateAvailable)
            {
                Debug.Log($"[InAppUpdate] No update to offer (availability: {appUpdateInfo.UpdateAvailability}, " +
                          $"installed version code: {Application.version}).");
                return;
            }

            var flexibleOptions = AppUpdateOptions.FlexibleAppUpdateOptions();

            if (!appUpdateInfo.IsUpdateTypeAllowed(flexibleOptions))
            {
                Debug.Log("[InAppUpdate] Flexible update not allowed for this update.");
                return;
            }

            Debug.Log($"[InAppUpdate] Update available — version code: {appUpdateInfo.AvailableVersionCode}, " +
                      $"priority: {appUpdateInfo.UpdatePriority}, staleness: {appUpdateInfo.ClientVersionStalenessDays}");

            // ── 3. Start the flexible update (downloads in background) ───────
            var updateRequest = appUpdateManager.StartUpdate(appUpdateInfo, flexibleOptions);
            await updateRequest.ToUniTask(cancellation);

            if (updateRequest.Error != AppUpdateErrorCode.NoError)
            {
                Debug.LogWarning($"[InAppUpdate] Update request failed: {updateRequest.Error}");
                return;
            }

            if (updateRequest.Status != AppUpdateStatus.Downloaded)
            {
                Debug.LogWarning($"[InAppUpdate] Update ended with status: {updateRequest.Status}");
                return;
            }

            // ── 4. Complete the update — this will restart the app ───────────
            Debug.Log("[InAppUpdate] Download complete. Completing update (app will restart)...");
            var completeOp = appUpdateManager.CompleteUpdate();
            await completeOp.ToUniTask(cancellation);

            // If we reach here, the update failed to install (successful update restarts the app).
            if (!completeOp.IsSuccessful)
            {
                Debug.LogWarning($"[InAppUpdate] CompleteUpdate failed: {completeOp.Error}");
            }
        }
#endif
    }
}
