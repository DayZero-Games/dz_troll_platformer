using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class PlayerFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private PlayerController playerController;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterComponentInNewPrefab(playerController, Lifetime.Scoped);
        }
    }
}
