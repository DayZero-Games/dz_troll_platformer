using System.Threading;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using DZ.Core.Contracts;

namespace DZ.Core.Runtime
{
	public sealed class BootstrapEntryPoint : IAsyncStartable
	{
		private readonly ISceneLoader _sceneLoader;
		private readonly SceneId _startScene;

		public BootstrapEntryPoint(ISceneLoader sceneLoader, SceneId startScene)
		{
			_sceneLoader = sceneLoader;
			_startScene = startScene;
		}

		public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
		{

			if (_startScene == SceneId.Bootstrap) return;

			await _sceneLoader.LoadAsync(_startScene, cancellation);
		}
	}
}
