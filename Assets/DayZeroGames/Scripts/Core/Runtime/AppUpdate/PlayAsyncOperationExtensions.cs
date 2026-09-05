#if UNITY_ANDROID
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Play.AppUpdate;
using Google.Play.Common;

namespace DZ.Core.Runtime
{
    /// <summary>
    /// Bridges Google Play's callback-based async types to UniTask so we can
    /// <c>await</c> them with cancellation support.
    /// </summary>
    public static class PlayAsyncOperationExtensions
    {
        /// <summary>
        /// Awaits a <see cref="PlayAsyncOperation{TResult,TError}"/> as a UniTask.
        /// </summary>
        public static UniTask ToUniTask<TResult, TError>(
            this PlayAsyncOperation<TResult, TError> operation,
            CancellationToken cancellation = default)
        {
            if (operation.IsDone)
                return UniTask.CompletedTask;

            var source = new UniTaskCompletionSource();

            var registration = cancellation.Register(() => source.TrySetCanceled());

            operation.Completed += _ =>
            {
                registration.Dispose();
                source.TrySetResult();
            };

            return source.Task;
        }

        /// <summary>
        /// Awaits an <see cref="AppUpdateRequest"/> (CustomYieldInstruction) as a UniTask.
        /// The request tracks the entire immediate-update download and install lifecycle.
        /// </summary>
        public static UniTask ToUniTask(
            this AppUpdateRequest request,
            CancellationToken cancellation = default)
        {
            if (request.IsDone)
                return UniTask.CompletedTask;

            var source = new UniTaskCompletionSource();

            var registration = cancellation.Register(() => source.TrySetCanceled());

            request.Completed += _ =>
            {
                registration.Dispose();
                source.TrySetResult();
            };

            return source.Task;
        }
    }
}
#endif
