using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class LevelFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private LevelCatalogSo _levelCatalogSo;
        [SerializeField] private Transform _levelRoot;
        [SerializeField] private int _startLevelIndex;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterInstance(_levelCatalogSo);
            containerBuilder.RegisterEntryPoint<LevelFlowController>()
                .WithParameter(_levelRoot)
                .WithParameter(_startLevelIndex);

        }
    }
}
