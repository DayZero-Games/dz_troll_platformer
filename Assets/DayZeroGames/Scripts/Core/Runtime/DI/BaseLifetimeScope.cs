using DZ.Core.Contracts;
using UnityEngine;
using UnityEngine.Scripting;
using VContainer;
using VContainer.Unity;

namespace DZ.Core.Runtime
{
    public class BaseLifetimeScope : LifetimeScope
    {
        [SerializeField] private BaseFeatureInstaller[] featureInstallers;

        protected override void Configure(IContainerBuilder builder)
        {
            foreach (var featureInstaller in featureInstallers)
            {
                if (featureInstaller!=null)
                {
                    featureInstaller.Register(builder);
                }
                else
                {
                    Debug.LogWarning($"{featureInstaller.name} does not implement IFeatureInstaller interface");
                }
            }
        }
    }
}