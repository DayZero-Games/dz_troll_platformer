using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Core.Runtime
{
	public class RootLifetimeScope : BaseLifetimeScope
	{
		private static RootLifetimeScope _instance;
		
		[SerializeField] private InputReaderSo _inputReader;
		[SerializeField] private AudioLibrarySo _audioLibrary;
		[SerializeField] private AudioService _audioService;

		protected override void Awake()
		{
			if (_instance != null)
			{
				Destroy(gameObject);
				return;
			}

			_instance = this;
			DontDestroyOnLoad(gameObject);
			base.Awake();
		}

		protected override void Configure(IContainerBuilder builder)
		{
			base.Configure(builder);

			builder.Register<ISignalBus, SignalBus>(Lifetime.Singleton);
			builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
			builder.RegisterInstance(_inputReader).As<IInputReader>();
			builder.RegisterInstance(_audioLibrary).As<IAudioLibrary>();
			builder.RegisterComponent(_audioService).As<IAudioService>();

			builder.RegisterEntryPoint<BootstrapEntryPoint>();
		}
	}
}