using DZ.Core.Contracts;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Core.Runtime
{
	public sealed class SceneLoader : ISceneLoader
	{
		public void Load(SceneId sceneId)
		{
			SceneManager.LoadScene(sceneId.ToString());
		}

		public async UniTask LoadAsync(SceneId sceneId)
		{
			await SceneManager.LoadSceneAsync(sceneId.ToString());
		}
	}
}