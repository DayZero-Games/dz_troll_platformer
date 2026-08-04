using System;
using DZ.Core.Contracts;
using VContainer.Unity;

namespace DZ.Features
{
    public class MainMenuController : IStartable, IDisposable
    {
        private readonly MainMenuView _router;
        private readonly MainPanelView _view;
        private readonly IAudioService _audioService;

        public MainMenuController(MainMenuView router, MainPanelView view, IAudioService audioService)
        {
            _router = router;
            _view = view;
            _audioService = audioService;
        }

        public void Start()
        {
            _view.PlayButton.onClick.AddListener(OnPlayClicked);
            _view.MusicButton.onClick.AddListener(ToggleMusic);
            _view.SfxButton.onClick.AddListener(ToggleSfx);

            _view.SetMusicIcon(_audioService.MusicEnabled);
            _view.SetSfxIcon(_audioService.SfxEnabled);
        }

        private void OnPlayClicked() => _router.ShowLevelSelection();

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
        }
    }
}
