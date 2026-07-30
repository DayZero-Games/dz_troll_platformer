using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class LevelWiseFeature : BaseFeatureInstaller
    {
        [SerializeField] private LevelExitDoor _exitDoor;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterComponent(_exitDoor);
        }
    }
}
