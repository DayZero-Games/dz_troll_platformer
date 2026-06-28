using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Core.Runtime
{
    public class BaseLifetimeScope : LifetimeScope
    {
        [SerializeField] private BaseFeatureInstaller[] featureInstallers;

        protected override void Configure(IContainerBuilder builder)
        {
	        if (featureInstallers.Length == 0) return;
	        
            foreach (var featureInstaller in featureInstallers)
            {
                if (featureInstaller!=null)
                {
                    featureInstaller.Register(builder);
                }
                else
                {
	                Debug.LogWarning("Feature Installer Not Found! Maybe you forgot to assign a feature installer?");
                }
            }
        }
    }
}