using DZ.Core;
using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class ScreenFaderFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private ScreenFader _fader;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterComponent(_fader).As<IScreenFader>();
        }
    }
}
