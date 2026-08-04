using System.Threading;
using Cysharp.Threading.Tasks;

namespace DZ.Core.Contracts
{
	public interface ISceneLoader
	{
		bool IsSceneLoaded(SceneId sceneId);	
		UniTask LoadAsync(SceneId sceneId, CancellationToken cancellation=default);
		UniTask UnloadAsync(SceneId sceneId, CancellationToken cancellation=default);
		UniTask SwitchSceneAsync(SceneId fromSceneId, SceneId toSceneId, CancellationToken cancellation = default);
	}
}