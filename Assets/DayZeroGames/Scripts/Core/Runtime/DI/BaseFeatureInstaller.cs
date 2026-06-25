using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Core.Runtime
{
    public abstract class BaseFeatureInstaller : MonoBehaviour
    {
        public abstract void Register(IContainerBuilder containerBuilder);
    }
}
