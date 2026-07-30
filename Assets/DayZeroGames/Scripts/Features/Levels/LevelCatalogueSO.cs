using System.Data.SqlTypes;
using UnityEngine;

namespace DZ.Features
{
    [System.Serializable]
    public sealed class LevelEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _levelPrefab;

        public string Id => _id;
        public GameObject LevelPrefab => _levelPrefab;
    }



    [CreateAssetMenu(fileName = "LevelCatalogueSO", menuName = "DayZeroGames/Level Catalogue")]
    public class LevelCatalogSo : ScriptableObject
    {
        private LevelEntry[] _levels;
        public int Count => _levels.Length;
        public bool HasLevel(int index) => index>0 && index <_levels.Length;
        public LevelEntry GetLevel(int index)=> _levels[index];
    }
}
