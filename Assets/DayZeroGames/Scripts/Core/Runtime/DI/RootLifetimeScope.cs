using DZ.Core.Contracts;
using VContainer;
using VContainer.Unity;

namespace DZ.Core.Runtime
{
    public class RootLifetimeScope : BaseLifetimeScope
    {
	    protected override void Configure(IContainerBuilder builder)
	    {
		    base.Configure(builder);

		    builder.Register<ISignalBus, SignalBus>(Lifetime.Singleton);
	    }
    }
}
