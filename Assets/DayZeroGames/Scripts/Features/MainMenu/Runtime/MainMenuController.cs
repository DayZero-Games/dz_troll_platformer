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

        private bool _musicOn = true;
        private bool _sfxOn = true;

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

            _view.SetMusicIcon(_musicOn);
            _view.SetSfxIcon(_sfxOn);
        }

        private void OnPlayClicked() => _router.ShowLevelSelection();

        private void ToggleMusic()
        {
            _musicOn = !_musicOn;

            if (_musicOn) _audioService.PlayMusic(AudioId.BackgroundMusic);
            else _audioService.StopMusic();

            _view.SetMusicIcon(_musicOn);
        }

        private void ToggleSfx()
        {
            _sfxOn = !_sfxOn;
            _view.SetSfxIcon(_sfxOn);
        }

        public void Dispose()
        {
            _view.PlayButton.onClick.RemoveListener(OnPlayClicked);
            _view.MusicButton.onClick.RemoveListener(ToggleMusic);
            _view.SfxButton.onClick.RemoveListener(ToggleSfx);
        }
    }
}
