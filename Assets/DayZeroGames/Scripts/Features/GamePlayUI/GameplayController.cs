using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;
using VContainer.Unity;

namespace DZ.Features
{
    public class GameplayController:IStartable, IDisposable
    {
        private readonly GameplayView _gameplayView;
        private readonly ISceneLoader _sceneLoader;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public GameplayController(GameplayView gameplayView, ISceneLoader sceneLoader)
        {
            _gameplayView = gameplayView;
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            _gameplayView.BackButton.onClick.AddListener(OnBackClicked);
        }

        private void OnBackClicked()
        {
            LoadMainMenuAsync().Forget();
        }

        private async UniTaskVoid LoadMainMenuAsync()
        {
            try
            {
                await _sceneLoader.SwitchSceneAsync(SceneId.Sandbox, SceneId.MainMenu,_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Menu scope torn down mid-switch; nothing to clean up.
            }
        }

        public void Dispose()
        {
            _gameplayView.BackButton.onClick.RemoveListener(OnBackClicked);
            _cts.Cancel();
        }
    }
}
