using DZ.Core.Runtime;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Features
{
    public class CameraFeatureInstaller : BaseFeatureInstaller
    {
        [Tooltip("Transform to shake. Must NOT be a transform a follow script writes to — " +
                 "make it a child of the follow rig instead, or the shake will snap the " +
                 "camera back to wherever it stood at startup.")]
        [SerializeField] private Transform _shakeTarget;

        [SerializeField] private CameraShakeConfigSo _cameraConfig;

        public override void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterEntryPoint<CameraShaker>()
                .WithParameter(_shakeTarget)
                .WithParameter(_cameraConfig);
        }
    }
}
