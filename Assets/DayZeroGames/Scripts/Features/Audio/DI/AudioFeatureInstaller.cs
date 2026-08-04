using DZ.Core.Contracts;
using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class AudioFeatureInstaller : BaseFeatureInstaller
    {
        [SerializeField] private AudioService _audioService;
        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterComponent(_audioService).As<IAudioService>();
        }
    }
}
