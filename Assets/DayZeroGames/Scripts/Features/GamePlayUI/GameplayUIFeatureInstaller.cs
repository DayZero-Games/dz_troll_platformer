using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class GameplayUIFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private GameplayView _gameplayView;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterEntryPoint<GameplayController>().WithParameter(_gameplayView);
        }
    }
}
