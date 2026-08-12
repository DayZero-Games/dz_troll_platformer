using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DZ.Core;
using PrimeTween;
using UnityEngine;

namespace DZ.Features
{
    public class ScreenFader : MonoBehaviour, IScreenFader
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _duration = 0.5f;
        private Tween fadeTween;

        public bool IsCovered => _canvasGroup.alpha > 0.999f;
        public async UniTask FadeFromBlackAsync(CancellationToken cancellationToken = default) => await FadeAsync(0f, cancellationToken);
        public async UniTask FadeToBlackAsync(CancellationToken cancellationToken = default) => await FadeAsync(1f, cancellationToken);

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetCoveredImmediately(true);
        }
        public void SetCoveredImmediately(bool covered)
        {
            if (fadeTween.isAlive) fadeTween.Stop();
            _canvasGroup.alpha = covered ? 1f : 0f;
            _canvasGroup.blocksRaycasts = covered;
        }

        private async UniTask FadeAsync(float targetAlpha, CancellationToken cancellation)
        {
            if (fadeTween.isAlive) fadeTween.Stop();
            _canvasGroup.blocksRaycasts = true;
            fadeTween = Tween.Alpha(_canvasGroup, targetAlpha, _duration, ease: Ease.InOutQuad, useUnscaledTime: true);
            try
            {
                await fadeTween.ToUniTask().AttachExternalCancellation(cancellation);
            }
            catch (OperationCanceledException)
            {
                
            }
            finally
            {
                if (fadeTween.isAlive) fadeTween.Stop();
                _canvasGroup.alpha = targetAlpha;
                _canvasGroup.blocksRaycasts = targetAlpha > 0.001f;
            }
        }
    }
}
