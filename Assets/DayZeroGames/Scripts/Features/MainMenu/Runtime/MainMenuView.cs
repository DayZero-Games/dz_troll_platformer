using UnityEngine;

namespace DZ.Features
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _levelSelectionPanel;

        private void Awake() => ShowMainPanel();

        public void ShowMainPanel()
        {
            _levelSelectionPanel.SetActive(false);
            _mainPanel.SetActive(true);
        }

        public void ShowLevelSelection()
        {
            _mainPanel.SetActive(false);
            _levelSelectionPanel.SetActive(true);
        }
    }
}
