using UnityEngine;

namespace DZ.Core
{
    [System.Serializable]
    public sealed class LevelEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _levelPrefab;

        public string Id => _id;
        public GameObject LevelPrefab => _levelPrefab;
    }



    [CreateAssetMenu(fileName = "LevelCatalogSo", menuName = "DayZeroGames/Level Catalog")]
    public class LevelCatalogSo : ScriptableObject
    {
        [SerializeField] private LevelEntry[] _levels;
        public int Count => _levels.Length;
        public bool HasLevel(int index) => index >= 0 && index < _levels.Length;
        public LevelEntry GetLevel(int index) => _levels[index];
    }
}
