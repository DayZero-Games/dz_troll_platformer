using System.IO;
using UnityEngine;

namespace DZ.Core
{
    public class SaveService: ISaveService
    {
        private static readonly string gameSavePath =  Application.persistentDataPath + "/gameSave.json";

        public void SaveGame(GameSaveData gameData)
        {
            string json = JsonUtility.ToJson(gameData);
            File.WriteAllText(gameSavePath, json);
            Debug.Log("Game saved");
        }

        public GameSaveData LoadGame()
        {
            if (!File.Exists(gameSavePath))
            {
                return new GameSaveData();
            }
            string json = File.ReadAllText(gameSavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        public bool HasGameSave()
        {
            return File.Exists(gameSavePath);
        }
    }
}
