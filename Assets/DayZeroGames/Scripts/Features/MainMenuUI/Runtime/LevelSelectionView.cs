using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class LevelSelectionView : MonoBehaviour
    {
        [SerializeField] private Transform _gridRoot;
        [SerializeField] private LevelButtonView _levelButtonPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _scrollRect;

        private int _focusedLevelIndex = -1;

        public Transform GridRoot => _gridRoot;
        public LevelButtonView LevelButtonPrefab => _levelButtonPrefab;
        public Button BackButton => _backButton;

        private void Awake()
        {
            if (_scrollRect == null) _scrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        /// <summary>
        /// Remembers which level the grid should open on, and re-applies it every time the panel is shown.
        /// </summary>
        public void FocusLevel(int levelIndex)
        {
            _focusedLevelIndex = levelIndex;
            if (isActiveAndEnabled) ScrollToFocusedLevelAsync().Forget();
        }

        private void OnEnable() => ScrollToFocusedLevelAsync().Forget();

        private async UniTaskVoid ScrollToFocusedLevelAsync()
        {
            ScrollToFocusedLevel();

            // The panel is switched on mid-frame, so re-apply once the canvas has run its own layout pass.
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
            ScrollToFocusedLevel();
        }

        private void ScrollToFocusedLevel()
        {
            if (_scrollRect == null || _gridRoot == null) return;
            if (_focusedLevelIndex < 0 || _focusedLevelIndex >= _gridRoot.childCount) return;

            var content = _scrollRect.content;
            var viewport = _scrollRect.viewport;
            if (content == null || viewport == null) return;

            if (_gridRoot.GetChild(_focusedLevelIndex) is not RectTransform target) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            var viewportHeight = viewport.rect.height;
            var scrollableHeight = content.rect.height - viewportHeight;

            _scrollRect.StopMovement();

            if (scrollableHeight <= 0f)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            // Children hang below the content's top edge, so distances grow towards negative Y.
            var targetTopY = target.localPosition.y + (1f - target.pivot.y) * target.rect.height;
            var distanceFromTop = content.rect.yMax - targetTopY;

            // Put the target's row at the top of the viewport, keeping the grid's usual top padding.
            var offset = Mathf.Clamp(distanceFromTop - GridTopPadding(), 0f, scrollableHeight);
            _scrollRect.verticalNormalizedPosition = 1f - offset / scrollableHeight;
        }

        private float GridTopPadding()
            => _gridRoot.TryGetComponent<LayoutGroup>(out var layoutGroup) ? layoutGroup.padding.top : 0f;
    }
}
