using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class MainPanelView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _musicButton;
        [SerializeField] private Button _sfxButton;

        [Header("No Ads (IAP)")]
        [SerializeField] private Button _noAdsButton;

        [Tooltip("iOS only — App Store review requires a visible restore path. Leave " +
                 "unassigned on Android, where the launch entitlement check already " +
                 "restores from the signed-in Google account.")]
        [SerializeField] private Button _restorePurchasesButton;

        [Header("Toggle Icons (optional)")]
        [SerializeField] private GameObject _musicOnIcon;
        //[SerializeField] private GameObject _musicOffIcon;
        [SerializeField] private GameObject _sfxOnIcon;
        //[SerializeField] private GameObject _sfxOffIcon;

        public Button PlayButton => _playButton;
        public Button MusicButton => _musicButton;
        public Button SfxButton => _sfxButton;
        public Button NoAdsButton => _noAdsButton;
        public Button RestorePurchasesButton => _restorePurchasesButton;

        // Once they own it there's nothing left to sell or restore — hide both.
        public void SetNoAdsButtonVisible(bool visible)
        {
            if (_noAdsButton != null) _noAdsButton.gameObject.SetActive(visible);
            if (_restorePurchasesButton != null) _restorePurchasesButton.gameObject.SetActive(visible);
        }

        // Greyed out until the store hands us a real product, and while a purchase is open.
        public void SetNoAdsInteractable(bool interactable)
        {
            if (_noAdsButton != null) _noAdsButton.interactable = interactable;
            if (_restorePurchasesButton != null) _restorePurchasesButton.interactable = interactable;
        }

        public void SetMusicIcon(bool isOn) => SwapIcons(_musicOnIcon, isOn);
        public void SetSfxIcon(bool isOn) => SwapIcons(_sfxOnIcon, isOn);

        private static void SwapIcons(GameObject onIcon, bool isOn)
        {
            if (onIcon != null) onIcon.SetActive(isOn);
            //if (offIcon != null) offIcon.SetActive(!isOn);
        }
    }
}
