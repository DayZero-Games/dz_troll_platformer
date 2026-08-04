using System;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer.Unity;

namespace DZ.Core
{
    public class LevelProgressService : ILevelProgress, IStartable, IDisposable
    {
        private readonly IPlayerPrefsSaveService _playerPrefsSaveService;
        private readonly ISignalBus _signalBus;
        private readonly LevelCatalogSo _levelCatalog;
        private int _highestUnlocked;
        public int HighestUnlockedIndex => _highestUnlocked;

        public LevelProgressService(IPlayerPrefsSaveService playerPrefsSaveService, ISignalBus signalBus, LevelCatalogSo levelCatalog)
        {
            _playerPrefsSaveService = playerPrefsSaveService;
            _signalBus = signalBus;
            _levelCatalog = levelCatalog;

            _highestUnlocked = ClampToCatalog(_playerPrefsSaveService.LoadInt(SaveKeys.HighestUnlockedLevel, 0));
        }

        public bool IsUnlocked(int index)=> index>=0 && index <= _highestUnlocked;
        public void Start()=> _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
        public void Dispose()=> _signalBus.Unsubscribe<LevelCompletedSignal>(OnLevelCompleted);

        private void OnLevelCompleted(LevelCompletedSignal signal)
        {
            var nextLevelIndex = signal.LevelIndex + 1;
            if (nextLevelIndex > _highestUnlocked)
            {
                _highestUnlocked = ClampToCatalog(nextLevelIndex);
                _playerPrefsSaveService.SaveInt(SaveKeys.HighestUnlockedLevel, _highestUnlocked);
            }
        }

        private int ClampToCatalog(int index)
        {
            return Mathf.Clamp(index, 0, _levelCatalog.Count - 1);
        }

        public void ResetProgress()
        {
            _highestUnlocked = 0;
            _playerPrefsSaveService.SaveInt(SaveKeys.HighestUnlockedLevel, _highestUnlocked);
        }
    }
}
