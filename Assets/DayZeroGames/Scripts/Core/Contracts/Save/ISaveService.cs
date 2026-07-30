using UnityEngine;

namespace DZ.Core
{
    public interface ISaveService
    {
        void SaveGame(GameSaveData gameData);
        GameSaveData LoadGame();

        bool HasGameSave();
    }
}
