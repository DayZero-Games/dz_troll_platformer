using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace DZ.Features
{
    public class MainMenuView : MonoBehaviour
    {
         [SerializeField] private CanvasGroup _mainPanel;
        [SerializeField] private CanvasGroup _levelSelectionPanel;
        [SerializeField] private float _fadeDuration = 0.2f;

        private void Awake()
        {
            SetPanel(_mainPanel, true);
            SetPanel(_levelSelectionPanel, false);
        }

        public UniTask ShowLevelSelectionAsync(CancellationToken cancellation = default)
            => CrossFadeAsync(_mainPanel, _levelSelectionPanel, cancellation);

        public UniTask ShowMainPanelAsync(CancellationToken cancellation = default)
            => CrossFadeAsync(_levelSelectionPanel, _mainPanel, cancellation);

        private async UniTask CrossFadeAsync(CanvasGroup from, CanvasGroup to, CancellationToken cancellation)
        {
            
            
            from.interactable = false;
            from.blocksRaycasts = false;
            to.interactable = false;

            to.gameObject.SetActive(true);
            to.alpha = 0f;

            await Tween.Alpha(from, 0f, _fadeDuration, useUnscaledTime: true)
                .ToUniTask().AttachExternalCancellation(cancellation);

            from.gameObject.SetActive(false);

            await Tween.Alpha(to, 1f, _fadeDuration, useUnscaledTime: true)
                .ToUniTask().AttachExternalCancellation(cancellation);

            to.interactable = true;
            to.blocksRaycasts = true;
        }

        private static void SetPanel(CanvasGroup panel, bool visible)
        {
            panel.alpha = visible ? 1f : 0f;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
            panel.gameObject.SetActive(visible);
        }
    }
}
