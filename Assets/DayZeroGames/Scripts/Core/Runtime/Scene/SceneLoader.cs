using DZ.Core.Contracts;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace DZ.Core.Runtime
{
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly IScreenFader _screenFader;

        public SceneLoader(IScreenFader screenFader)
        {
            _screenFader = screenFader;
        }

        public bool IsSceneLoaded(SceneId sceneId)
        {
            var scene = SceneManager.GetSceneByName(sceneId.ToString());
            return scene.isLoaded;
        }

        public async UniTask LoadAsync(SceneId sceneId, CancellationToken cancellation = default)
        {
            if (IsSceneLoaded(sceneId))
            {
                Debug.LogWarning($"Scene {sceneId} is already loaded.");
                return;
            }

            if (!_screenFader.IsCovered)
            {
                await _screenFader.FadeToBlackAsync(cancellation);
            }
            var loadOperation = SceneManager.LoadSceneAsync(sceneId.ToString(), LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"Failed to start loading scene '{sceneId}'. " +
                    "Make sure it is included in the Build Settings.");
            }
            await loadOperation.ToUniTask(cancellationToken: cancellation);

        }

        public async UniTask UnloadAsync(SceneId sceneId, CancellationToken cancellation = default)
        {
            if (!IsSceneLoaded(sceneId))
            {
                Debug.LogWarning($"Scene {sceneId} is not loaded.");
                return;
            }
            var unloadOperation = SceneManager.UnloadSceneAsync(sceneId.ToString());
            if (unloadOperation == null)
            {
                throw new InvalidOperationException(
                    $"Failed to start unloading scene '{sceneId}'. " +
                    "Make sure it is included in the Build Settings.");
            }
            await unloadOperation.ToUniTask(cancellationToken: cancellation);
        }

        public async UniTask SwitchSceneAsync(SceneId fromSceneId, SceneId toSceneId, CancellationToken cancellation = default)
        {
            if (IsSceneLoaded(toSceneId))
            {
                Debug.LogWarning($"Already in Scene {toSceneId}");
                return;
            }
            await LoadAsync(toSceneId, cancellation);

            var loadedScene = SceneManager.GetSceneByName(toSceneId.ToString());
            if (loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);
            }
            await UnloadAsync(fromSceneId, cancellation);
        }
    }
}