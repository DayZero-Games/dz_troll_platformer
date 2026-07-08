using Cysharp.Threading.Tasks;

namespace DZ.Core.Contracts
{
	public interface ISceneLoader
	{
		public void Load(SceneId sceneId);
		public UniTask LoadAsync(SceneId sceneId);
	}
}