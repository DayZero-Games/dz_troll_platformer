using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
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
#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly IAudioService _audioService;

        public InAppUpdateService(IAudioService audioService)
        {
            _audioService = audioService;
        }
#endif

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

            // ── 2. Is an update available (or already mid-flight)? ───────────
            // DeveloperTriggeredUpdateInProgress means an immediate update was started in a
            // previous session and the user backed out of it — resume the full-screen flow.
            var availability = appUpdateInfo.UpdateAvailability;

            if (availability != UpdateAvailability.UpdateAvailable &&
                availability != UpdateAvailability.DeveloperTriggeredUpdateInProgress)
            {
                Debug.Log($"[InAppUpdate] No update to offer (availability: {availability}, " +
                          $"installed version code: {Application.version}).");
                return;
            }

            var immediateOptions = AppUpdateOptions.ImmediateAppUpdateOptions();

            if (!appUpdateInfo.IsUpdateTypeAllowed(immediateOptions))
            {
                Debug.Log("[InAppUpdate] Immediate update not allowed for this update.");
                return;
            }

            Debug.Log($"[InAppUpdate] Update available — version code: {appUpdateInfo.AvailableVersionCode}, " +
                      $"priority: {appUpdateInfo.UpdatePriority}, staleness: {appUpdateInfo.ClientVersionStalenessDays}");

            // ── 3. Start the immediate update ────────────────────────────────
            // Play takes over with a blocking full-screen UI, downloads, installs and
            // restarts the app itself — there is no CompleteUpdate() step for this flow.
            // Play's UI is a separate Android activity and the player is configured with
            // Run In Background — without this the music keeps playing underneath it.
            Debug.Log("[InAppUpdate] Starting immediate (full-screen) update...");
            _audioService.PauseMusicForOverlay();

            var updateRequest = appUpdateManager.StartUpdate(appUpdateInfo, immediateOptions);
            await updateRequest.ToUniTask(cancellation);

            // A successful immediate update restarts the app, so reaching this line means the
            // update did not land — resume music so the player isn't left in silence.
            _audioService.ResumeMusicAfterOverlay();

            if (updateRequest.Error != AppUpdateErrorCode.NoError)
            {
                // ErrorUserCanceled here means the user dismissed the full-screen prompt.
                Debug.LogWarning($"[InAppUpdate] Update request failed: {updateRequest.Error}");
                return;
            }

            Debug.LogWarning($"[InAppUpdate] Immediate update ended with status: {updateRequest.Status}");
        }
#endif
    }
}
