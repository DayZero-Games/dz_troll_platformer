using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Core.Runtime
{
    public class MainMenuLifetimeScope : BaseLifetimeScope
    {
        protected override void Configure(IContainerBuilder containerBuilder)
        {
            base.Configure(containerBuilder);
            
        }
    }
}
