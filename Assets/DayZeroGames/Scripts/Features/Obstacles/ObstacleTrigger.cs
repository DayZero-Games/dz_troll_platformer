using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [RequireComponent(typeof(Collider2D))]
    public class ObstacleTrigger : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private bool _disableColliderAfterSuccessfulActivation;
        [SerializeField] private ObstacleController[] _controllers;

        private Collider2D _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            _triggerCollider.isTrigger = true;

            if ((_controllers == null || _controllers.Length == 0) &&
                TryGetComponent(out ObstacleController controller))
            {
                _controllers = new[] { controller };
            }

            if (_controllers == null || _controllers.Length == 0)
            {
                Debug.LogWarning($"{name}: no obstacle controllers assigned.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_playerTag)) return;
            if (_controllers == null) return;

            var activatedAnyController = false;
            var cancellation = this.GetCancellationTokenOnDestroy();
            foreach (var controller in _controllers)
            {
                if (controller == null) continue;
                activatedAnyController |= controller.TryActivate(cancellation);
            }

            if (activatedAnyController && _disableColliderAfterSuccessfulActivation && _triggerCollider != null)
                _triggerCollider.enabled = false;
        }
    }
}
