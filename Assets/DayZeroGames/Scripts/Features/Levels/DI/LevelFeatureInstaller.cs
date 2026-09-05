
using DZ.Core.Runtime;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class LevelFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private Transform _levelRoot;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterEntryPoint<LevelFlowController>()
                .As<ILevelRuntimeActions>()
                .WithParameter(_levelRoot);
        }
    }
}
