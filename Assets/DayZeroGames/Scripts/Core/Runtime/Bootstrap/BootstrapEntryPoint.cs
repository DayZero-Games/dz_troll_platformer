using System.Threading;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;

namespace DZ.Core.Runtime
{
	public sealed class BootstrapEntryPoint : IAsyncStartable
	{
		private readonly ISceneLoader _sceneLoader;

		public BootstrapEntryPoint(ISceneLoader sceneLoader)
		{
			_sceneLoader = sceneLoader;
		}
		
		public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
		{
			await _sceneLoader.LoadAsync(SceneId.MainMenu);
		}
	}
}