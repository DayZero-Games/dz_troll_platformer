using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class LevelButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _lockedOverlay;

        private int _levelIndex = -1;
        private Action<int> _onClicked;
        private bool _listenerHooked;

        public void Bind(int levelIndex, string label, bool unlocked, Action<int> onClicked)
        {

            if (_button == null)
            {
                Debug.LogError($"{nameof(LevelButtonView)}.{nameof(Bind)}: button is null");
            }

            if (!_listenerHooked)
            {
                _button.onClick.AddListener(HandleClick);
                _listenerHooked = true;
            }

            _levelIndex = levelIndex;
            _onClicked = onClicked;

            if (_label != null) _label.text = label;
            if (_lockedOverlay != null) _lockedOverlay.SetActive(!unlocked);
            _button.interactable = unlocked;
        }

        private void HandleClick() => _onClicked?.Invoke(_levelIndex);

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
            _onClicked = null;
        }
    }
}
