using UnityEngine;

namespace DZ.Features
{
    [DisallowMultipleComponent]
    public class SawRotator : MonoBehaviour
    {
        private const float MinMovementSqrMagnitude = 0.000001f;

        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField, Min(0f)] private float _rotationSpeed = 720f;

        private Vector3 _lastPosition;
        private float _rotationDirection = 1f;

        private void OnEnable()
        {
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            var currentPosition = transform.position;
            var movement = currentPosition - _lastPosition;
            UpdateMovementDirection(movement);
            _lastPosition = currentPosition;

            transform.Rotate(0f, 0f, _rotationSpeed * _rotationDirection * Time.deltaTime, Space.Self);
        }

        private void UpdateMovementDirection(Vector3 movement)
        {
            if (movement.sqrMagnitude <= MinMovementSqrMagnitude) return;

            var horizontalMovement = Mathf.Abs(movement.x);
            var verticalMovement = Mathf.Abs(movement.y);
            var dominantMovement = horizontalMovement >= verticalMovement ? movement.x : movement.y;

            if (Mathf.Approximately(dominantMovement, 0f)) return;

            _rotationDirection = dominantMovement > 0f ? -1f : 1f;
        }
    }
}
