using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace DZ.Features
{
    public class LevelSelectionController : IStartable, IDisposable
    {
        private readonly MainMenuView _router;
        private readonly LevelSelectionView _view;
        private readonly LevelCatalogSo _levelCatalog;
        private readonly ILevelSelection _levelSelection;
        private readonly ILevelProgress _levelProgress;
        private readonly ISceneLoader _sceneLoader;

        private readonly List<LevelButtonView> _levelButtons = new List<LevelButtonView>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _isLoading;

        public LevelSelectionController(
            MainMenuView router,
            LevelSelectionView view,
            LevelCatalogSo levelCatalog,
            ILevelSelection levelSelection,
            ILevelProgress levelProgress,
            ISceneLoader sceneLoader)
        {
            _router = router;
            _view = view;
            _levelCatalog = levelCatalog;
            _levelSelection = levelSelection;
            _levelProgress = levelProgress;
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            if (_view.BackButton != null)
                _view.BackButton.onClick.AddListener(OnBackClicked);

            BuildLevelButtons();
        }

        private void BuildLevelButtons()
        {
            var prefab = _view.LevelButtonPrefab;
            var gridRoot = _view.GridRoot;

            if (prefab == null || gridRoot == null)
            {
                Debug.LogError($"{nameof(LevelSelectionView)}: grid root or level button prefab not assigned.");
                return;
            }

            for (int i = 0; i < _levelCatalog.Count; i++)
            {
                var button = Object.Instantiate(prefab, gridRoot);
                button.name = $"LevelButton_{i + 1}";
                button.Bind(i, (i + 1).ToString(), IsUnlocked(i), OnLevelClicked);

                _levelButtons.Add(button);
            }
        }

        
        private bool IsUnlocked(int levelIndex) => _levelProgress.IsUnlocked(levelIndex);

        private void OnBackClicked() => _router.ShowMainPanelAsync();

        private void OnLevelClicked(int levelIndex)
        {
            if (_isLoading) return;

            if (!_levelCatalog.HasLevel(levelIndex))
            {
                Debug.LogWarning($"No level at index {levelIndex}");
                return;
            }

            _isLoading = true;

            
            _levelSelection.SelectLevel(levelIndex);
            LoadGameplayAsync().Forget();
        }

        private async UniTaskVoid LoadGameplayAsync()
        {
            try
            {
                await _sceneLoader.SwitchSceneAsync(SceneId.MainMenu, SceneId.Sandbox, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                
            }
        }

        public void Dispose()
        {
            if (_view.BackButton != null)
                _view.BackButton.onClick.RemoveListener(OnBackClicked);

            _levelButtons.Clear();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
