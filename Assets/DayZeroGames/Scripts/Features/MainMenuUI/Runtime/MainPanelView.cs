using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class MainPanelView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _musicButton;
        [SerializeField] private Button _sfxButton;

        [Header("Toggle Icons (optional)")]
        [SerializeField] private GameObject _musicOnIcon;
        
        [SerializeField] private GameObject _sfxOnIcon;
        

        public Button PlayButton => _playButton;
        public Button MusicButton => _musicButton;
        public Button SfxButton => _sfxButton;

        public void SetMusicIcon(bool isOn) => SwapIcons(_musicOnIcon, isOn);
        public void SetSfxIcon(bool isOn) => SwapIcons(_sfxOnIcon, isOn);

        private static void SwapIcons(GameObject onIcon, bool isOn)
        {
            if (onIcon != null) onIcon.SetActive(isOn);
            
        }
    }
}
