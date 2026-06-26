using DZ.Core.Contracts;
using VContainer;

namespace DZ.Core.Runtime
{
    public class GameLifetimeScope : BaseLifetimeScope
    {
        public InputReaderSo inputReaderSo;

        protected override void Configure(IContainerBuilder containerBuilder)
        {
            base.Configure(containerBuilder);
            containerBuilder.RegisterInstance(inputReaderSo).As<IInputReader>();
        }
    }
}
