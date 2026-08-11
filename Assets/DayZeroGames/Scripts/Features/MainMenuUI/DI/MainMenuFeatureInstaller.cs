using DZ.Core.Runtime;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class MainMenuFeatureInstaller : BaseFeatureInstaller
    {
        [FormerlySerializedAs("mainMenuView")]
        [SerializeField] private MainMenuView _mainMenuView;
        [SerializeField] private MainPanelView _mainPanelView;
        [SerializeField] private LevelSelectionView _levelSelectionView;

        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterEntryPoint<MainMenuController>()
                .WithParameter(_mainMenuView)
                .WithParameter(_mainPanelView);
            containerBuilder.RegisterEntryPoint<LevelSelectionController>()
                .WithParameter(_mainMenuView)
                .WithParameter(_levelSelectionView);
            containerBuilder.RegisterEntryPoint<NoAdsController>()
                .WithParameter(_mainPanelView);
        }
    }
}
