using UnityEngine;

namespace DZ.Features
{
    [DisallowMultipleComponent]
    public class SawRotator : MonoBehaviour
    {
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField, Min(0f)] private float _rotationSpeed = 720f;

        private void Update()
        {
            transform.Rotate(-Vector3.forward * _rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
