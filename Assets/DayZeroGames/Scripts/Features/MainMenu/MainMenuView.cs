using System;
using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _levelSelectionPanel;
        
       public Button playButton;
       public Button musicButton;
       public Button sfxButton;

       private void Start()
       {
           _mainPanel.SetActive(true);
           _levelSelectionPanel.SetActive(false);
       }

       public void ShowLevelSelection()
       {
           _mainPanel.SetActive(false);
           _levelSelectionPanel.SetActive(true);
       }
    }
}
