using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using DZ.Core.Contracts;
using VContainer.Unity;

namespace DZ.Features
{
    public class GameplayController:IStartable, IDisposable
    {
        private readonly GameplayView _gameplayView;
        private readonly ISceneLoader _sceneLoader;
        private readonly IAdService _adService;
        private readonly IAudioService _audioService;
        private readonly ISignalBus _signalBus;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public GameplayController(
            GameplayView gameplayView,
            ISceneLoader sceneLoader,
            IAdService adService,
            IAudioService audioService,
            ISignalBus signalBus)
        {
            _gameplayView = gameplayView;
            _sceneLoader = sceneLoader;
            _adService = adService;
            _audioService = audioService;
            _signalBus = signalBus;
        }

        public void Start()
        {
            _signalBus.Subscribe<GameCompletedSignal>(OnGameCompleted);
            _adService.ShowBanner();
            _gameplayView.BackButton.onClick.AddListener(OnBackClicked);
        }

        private void OnGameCompleted(GameCompletedSignal signal) => _sceneLoader.SwitchSceneAsync(SceneId.Gameplay,SceneId.MainMenu,_cts.Token);

        private void OnBackClicked()
        {
            _audioService.PlaySfx(AudioId.UIButtonPressed);
            LoadMainMenuAsync().Forget();
        }

        private async UniTaskVoid LoadMainMenuAsync()
        {
            try
            {
                await _sceneLoader.SwitchSceneAsync(SceneId.Gameplay, SceneId.MainMenu,_cts.Token);
            }
            catch (OperationCanceledException)
            {
                
            }
        }

        public void Dispose()
        {
            _gameplayView.BackButton.onClick.RemoveListener(OnBackClicked);
            _cts.Cancel();
        }
    }
}
