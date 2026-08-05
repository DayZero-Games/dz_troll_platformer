using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using DZ.Core.Contracts;
using VContainer.Unity;

namespace DZ.Features
{
    public class MainMenuController : IStartable, IDisposable
    {
        private readonly MainMenuView _router;
        private readonly MainPanelView _view;
        private readonly IAudioService _audioService;
        private readonly IScreenFader _screenFader;
        private readonly IAdService _adService;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public MainMenuController(MainMenuView router, MainPanelView view, IAudioService audioService, IScreenFader screenFader, IAdService adService)
        {
            _router = router;
            _view = view;
            _audioService = audioService;
            _screenFader = screenFader;
            _adService = adService;
        }

        public void Start()
        {
            _view.PlayButton.onClick.AddListener(OnPlayClicked);
            _view.MusicButton.onClick.AddListener(ToggleMusic);
            _view.SfxButton.onClick.AddListener(ToggleSfx);

            _view.SetMusicIcon(_audioService.MusicEnabled);
            _view.SetSfxIcon(_audioService.SfxEnabled);

            _screenFader.FadeFromBlackAsync(_cts.Token).Forget();
            _adService.ShowBanner();
        }

        private void OnPlayClicked() => _router.ShowLevelSelectionAsync(_cts.Token);

        private void ToggleMusic()
        {
            var _musicEnabled = !_audioService.MusicEnabled;
            _audioService.SetMusicEnabled(_musicEnabled);
            _view.SetMusicIcon(_musicEnabled);
        }

        private void ToggleSfx()
        {
            var _sfxEnabled = !_audioService.SfxEnabled;
            _audioService.SetSfxEnabled(_sfxEnabled);
            _view.SetSfxIcon(_sfxEnabled);       }

        public void Dispose()
        {
            _view.PlayButton.onClick.RemoveListener(OnPlayClicked);
            _view.MusicButton.onClick.RemoveListener(ToggleMusic);
            _view.SfxButton.onClick.RemoveListener(ToggleSfx);
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
