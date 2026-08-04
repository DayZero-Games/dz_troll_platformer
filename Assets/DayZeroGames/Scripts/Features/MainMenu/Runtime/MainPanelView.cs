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
        [SerializeField] private GameObject _musicOffIcon;
        [SerializeField] private GameObject _sfxOnIcon;
        [SerializeField] private GameObject _sfxOffIcon;

        public Button PlayButton => _playButton;
        public Button MusicButton => _musicButton;
        public Button SfxButton => _sfxButton;

        public void SetMusicIcon(bool isOn) => SwapIcons(_musicOnIcon, _musicOffIcon, isOn);
        public void SetSfxIcon(bool isOn) => SwapIcons(_sfxOnIcon, _sfxOffIcon, isOn);

        private static void SwapIcons(GameObject onIcon, GameObject offIcon, bool isOn)
        {
            if (onIcon != null) onIcon.SetActive(isOn);
            if (offIcon != null) offIcon.SetActive(!isOn);
        }
    }
}
