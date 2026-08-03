using UnityEngine;

namespace DZ.Features
{
    public class MainMenuController
    {
        private MainMenuView _mainMenuView;
        public MainMenuController(MainMenuView mainMenuView)
        {
            _mainMenuView = mainMenuView;
            
            _mainMenuView.playButton.onClick.AddListener(OnPlayButtonClicked);
            _mainMenuView.musicButton.onClick.AddListener(ToggleMusic);
            _mainMenuView.sfxButton.onClick.AddListener(ToggleSfx);
        }

        private void ToggleSfx()
        {
            //Turn On and Off Sfx throgh IAudioService API.
        }

        private void ToggleMusic()
        {
            //Turn On and Off Music Though IAudioService API.
        }

        private void OnPlayButtonClicked()
        {
            //_mainMenuView.ShowLevelSelection();
        }
    }
}