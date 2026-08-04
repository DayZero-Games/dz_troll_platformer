using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class LevelSelectionView : MonoBehaviour
    {
        [SerializeField] private Transform _gridRoot;
        [SerializeField] private LevelButtonView _levelButtonPrefab;
        [SerializeField] private Button _backButton;

        public Transform GridRoot => _gridRoot;
        public LevelButtonView LevelButtonPrefab => _levelButtonPrefab;
        public Button BackButton => _backButton;
    }
}
