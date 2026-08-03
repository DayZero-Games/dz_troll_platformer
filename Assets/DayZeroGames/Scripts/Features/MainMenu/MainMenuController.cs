using System.Threading;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class MainMenuController : IStartable
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly MainMenuView _mainMenuView;

        public MainMenuController( ISceneLoader sceneLoader,MainMenuView mainMenuView)
        {
            Debug.Log("MainMenuController");
            _mainMenuView = mainMenuView;
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            Debug.Log("MainMenuController started");
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
            Debug.Log("Loading SandBox Scene");
            _sceneLoader.SwitchSceneAsync(SceneId.MainMenu,SceneId.Sandbox, new CancellationToken());
            //_mainMenuView.ShowLevelSelection();
        }
    }
}