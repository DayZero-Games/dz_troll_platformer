using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    public enum CameraShakeType
    {
        Bump,       // light landing thud
        Impact,     // hit: position + a little roll
        Explosion,  // big, slow, long falloff
        Rumble,     // sustained, no falloff
        Death       // rotational roll + zoom punch
    }

    [RequireComponent(typeof(Camera))]
    public class CameraShaker : MonoBehaviour
    {

        [Header("Targets")]
        [Tooltip("Transform to shake. Leave empty to use this object. " +
                 "Must NOT be a transform a follow script writes to.")]
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private Camera _camera;

        [Header("Presets")]
        [SerializeField]
        private ShakeSettings _bump = new ShakeSettings(
            strength: new Vector3(0.08f, 0.12f), duration: 0.18f, frequency: 22f);

        [SerializeField]
        private ShakeSettings _impact = new ShakeSettings(
            strength: new Vector3(0.35f, 0.35f), duration: 0.35f, frequency: 16f);

        [SerializeField]
        private ShakeSettings _impactRoll = new ShakeSettings(
            strength: new Vector3(0f, 0f, 2.5f), duration: 0.35f, frequency: 14f);

        [SerializeField]
        private ShakeSettings _explosion = new ShakeSettings(
            strength: new Vector3(0.9f, 0.9f), duration: 0.8f, frequency: 9f,
            easeBetweenShakes: Ease.OutQuad);

        // enableFalloff:false → constant intensity for the whole duration
        [SerializeField]
        private ShakeSettings _rumble = new ShakeSettings(
            strength: new Vector3(0.05f, 0.05f), duration: 1.5f, frequency: 30f,
            enableFalloff: false);

        [SerializeField]
        private ShakeSettings _deathRoll = new ShakeSettings(
            strength: new Vector3(0f, 0f, 6f), duration: 0.9f, frequency: 6f);

        [Header("Zoom Punch")]
        [SerializeField] private float _zoomPunch = 0.6f;
        [SerializeField] private float _zoomDuration = 0.3f;

        private Tween _positionTween;
        private Tween _rotationTween;
        private Tween _zoomTween;

        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private float _baseOrthographicSize;

        private void Awake()
        {
            if (_shakeTarget == null) _shakeTarget = transform;
            if (_camera == null) _camera = GetComponent<Camera>();

            _baseLocalPosition = _shakeTarget.localPosition;
            _baseLocalRotation = _shakeTarget.localRotation;
            if (_camera != null) _baseOrthographicSize = _camera.orthographicSize;
        }

        public void Shake(CameraShakeType type)
        {
            switch (type)
            {
                case CameraShakeType.Bump:
                    PlayPositionShake(_bump);
                    break;

                case CameraShakeType.Impact:
                    PlayPositionShake(_impact);
                    PlayRotationShake(_impactRoll);
                    break;

                case CameraShakeType.Explosion:
                    PlayPositionShake(_explosion);
                    PlayZoomPunch();
                    break;

                case CameraShakeType.Rumble:
                    PlayPositionShake(_rumble);
                    break;

                case CameraShakeType.Death:
                    PlayRotationShake(_deathRoll);
                    PlayZoomPunch();
                    break;
            }
        }

        /// <summary>Directional kick — recoil, or knockback away from a hit.</summary>
        public void Punch(Vector2 direction, float strength = 0.4f, float duration = 0.3f)
        {
            CancelPositionShake();
            _positionTween = Tween.PunchLocalPosition(_shakeTarget,
                new ShakeSettings(direction.normalized * strength, duration, frequency: 12f));
        }

        public void PlayZoomPunch()
        {
            if (_camera == null || !_camera.orthographic) return;

            if (_zoomTween.isAlive) _zoomTween.Stop();
            _camera.orthographicSize = _baseOrthographicSize;

            _zoomTween = Tween.CameraOrthographicSize(
                _camera,
                endValue: _baseOrthographicSize - _zoomPunch,
                duration: _zoomDuration * 0.5f,
                ease: Ease.OutQuad,
                cycles: 2,
                cycleMode: CycleMode.Yoyo);
        }

        public void StopAll()
        {
            CancelPositionShake();
            CancelRotationShake();
            if (_zoomTween.isAlive) _zoomTween.Stop();
            if (_camera != null) _camera.orthographicSize = _baseOrthographicSize;
        }

        private void PlayPositionShake(ShakeSettings settings)
        {
            CancelPositionShake();
            _positionTween = Tween.ShakeLocalPosition(_shakeTarget, settings);
        }

        private void PlayRotationShake(ShakeSettings settings)
        {
            CancelRotationShake();
            _rotationTween = Tween.ShakeLocalRotation(_shakeTarget, settings);
        }

        // Stop() does NOT restore the start value mid-shake — reset by hand,
        // otherwise the leftover offset sticks when one shake interrupts another.
        private void CancelPositionShake()
        {
            if (_positionTween.isAlive) _positionTween.Stop();
            _shakeTarget.localPosition = _baseLocalPosition;
        }

        private void CancelRotationShake()
        {
            if (_rotationTween.isAlive) _rotationTween.Stop();
            _shakeTarget.localRotation = _baseLocalRotation;
        }

        private void OnDisable() => StopAll();
    }
}
