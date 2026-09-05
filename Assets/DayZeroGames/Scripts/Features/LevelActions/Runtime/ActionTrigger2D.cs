using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DZ.Features
{
    [RequireComponent(typeof(Collider2D))]
    public class ActionTrigger2D : MonoBehaviour
    {
        [SerializeField] private string _activatorTag = "Player";
        [SerializeField] private bool _disableColliderAfterSuccessfulActivation = true;
        [SerializeField] private LevelActionSequenceController[] _sequenceControllers;

        private Collider2D _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            _triggerCollider.isTrigger = true;

            if ((_sequenceControllers == null || _sequenceControllers.Length == 0) &&
                TryGetComponent(out LevelActionSequenceController sequenceController))
            {
                _sequenceControllers = new[] { sequenceController };
            }

            if (_sequenceControllers == null || _sequenceControllers.Length == 0)
                Debug.LogWarning($"{name}: no level action sequence controllers assigned.", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_activatorTag)) return;
            if (_sequenceControllers == null) return;

            var activatedAnyController = false;
            var cancellation = this.GetCancellationTokenOnDestroy();

            foreach (var controller in _sequenceControllers)
            {
                if (controller == null) continue;
                activatedAnyController |= controller.TryActivate(cancellation);
            }

            if (activatedAnyController && _disableColliderAfterSuccessfulActivation && _triggerCollider != null)
                _triggerCollider.enabled = false;
        }
    }
}
