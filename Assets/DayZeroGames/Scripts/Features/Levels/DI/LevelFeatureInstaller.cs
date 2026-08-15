
using DZ.Core.Runtime;
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
                .WithParameter(_levelRoot);
        }
    }
}
