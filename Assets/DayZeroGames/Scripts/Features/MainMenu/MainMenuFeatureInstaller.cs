using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class MainMenuFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private MainMenuView mainMenuView;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterComponent(mainMenuView);
            containerBuilder.RegisterEntryPoint<MainMenuController>().WithParameter(mainMenuView);
        }
    }
}
