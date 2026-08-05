using DZ.Core.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DZ.Core.Runtime
{
	public class RootLifetimeScope : BaseLifetimeScope
	{

		[SerializeField] private InputReaderSo _inputReader;
		[SerializeField] private AudioLibrarySo _audioLibrary;
		[SerializeField] private LevelCatalogSo _levelCatalog;
		[SerializeField] private AdsSettingsSo _adsSettings;
		
		[SerializeField] private SceneId _startScene = SceneId.MainMenu;

		[Tooltip("Scene loaded additively once the root container is up. Bootstrap = load nothing.")]
		protected override LifetimeScope FindParent() => null;

		protected override void Configure(IContainerBuilder builder)
		{
			base.Configure(builder);
			
			builder.RegisterInstance(_levelCatalog);
			builder.RegisterInstance(_inputReader).As<IInputReader>();
			builder.RegisterInstance(_audioLibrary).As<IAudioLibrary>();
			builder.RegisterInstance(_adsSettings);

			builder.Register<ISignalBus, SignalBus>(Lifetime.Singleton);
			builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
			builder.Register<ILevelSelection, LevelSelection>(Lifetime.Singleton);
			builder.Register<IPlayerPrefsSaveService, PlayerPrefsSaveService>(Lifetime.Singleton);
			builder.Register<IAdService, AdServiceProvider>(Lifetime.Singleton);
			
			builder.RegisterEntryPoint<BootstrapEntryPoint>().WithParameter(_startScene);
			builder.RegisterEntryPoint<LevelProgressService>();
		}
	}
}
