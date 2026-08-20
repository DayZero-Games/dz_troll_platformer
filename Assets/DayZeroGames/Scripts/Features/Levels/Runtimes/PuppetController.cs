using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    /// <summary>
    /// Hands control of a level object over to the player for that level: the real player
    /// stays frozen at the spawn point while this object is driven by the same input, and
    /// whatever kills this object kills the player.
    /// Put it on an object inside a level prefab and assign it to that level's
    /// <see cref="LevelContext"/> - the level flow does the rest.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PuppetController : MonoBehaviour, ILevelAvatar
    {
        [Inject] private readonly IInputReader _inputReader;
        [Inject] private readonly PlayerController _playerController;

        [Header("Movement")]
        [Tooltip("Speed, jump force and fall multiplier. Assign the player's own config asset to make " +
                 "the puppet handle exactly like the player, or a separate one to give it its own feel.")]
        [SerializeField] private PlayerConfigSo _config;

        [SerializeField] private bool _canJump = true;

        [Tooltip("Mirror the puppet to face the direction it is travelling. Turn off for objects that read the same both ways.")]
        [SerializeField] private bool _flipWithMoveDirection = true;

        [Header("Ground Check Sensor Settings")]
        [SerializeField] private Transform _groundCheckPos;
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.5f, 0.1f);
        [SerializeField] private LayerMask _groundLayer;

        [Header("References")]
        [Tooltip("Faded out by the exit door. Falls back to the first sprite renderer found on this object.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Tags")]
        [SerializeField] private string _obstacleTag = "Obstacles";
        [SerializeField] private string _fallingGroundTag = "FallingGround";

        private Rigidbody2D _rb;
        private Transform _originalParent;
        private bool _isLocked = true;
        private bool _isDead;
        private bool _isGrounded;
        private bool _isFacingRight = true;
        private bool _isSubscribed;

        // Runtime only, same as PlayerController: _config stays the shared baseline and is never
        // written to, while the level rules scale it per load. These have to be per-instance -
        // the multiplier scales this rigidbody, and the counter is this object's jump budget.
        private float _baseGravityScale = 1f;
        private int _maxAirJumps;
        private float _jumpForceMultiplier = 1f;
        private int _airJumpsUsed;

        public Transform Transform => transform;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public bool IsDead => _isDead;
        public bool IsGrounded => _isGrounded;
        public bool IsLocked => _isLocked;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _baseGravityScale = _rb.gravityScale;

            // Kept so falling-ground parenting can be undone without orphaning the puppet
            // from the level prefab - unlike the player, this object is part of the level.
            _originalParent = transform.parent;

            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_inputReader == null)
                Debug.LogError($"{name}: puppet was not created through the level flow, so it has no input.", this);

            if (_config == null)
                Debug.LogError($"{name}: puppet has no {nameof(PlayerConfigSo)} assigned, so it cannot move.", this);

            if (_canJump && _groundCheckPos == null)
                Debug.LogError($"{name}: puppet can jump but has no ground check transform assigned.", this);

            if (_canJump && _groundLayer.value == 0)
                Debug.LogError($"{name}: puppet ground layer mask is empty, so it can never jump.", this);

            // The classic one: a mask that includes the puppet's own layer makes the sensor
            // detect the puppet's own collider, so it reads as grounded forever and jumps
            // at any height. The player avoids this only because its mask happens to exclude
            // its own layer - make it loud here instead of leaving it to be discovered.
            if (_canJump && (_groundLayer.value & (1 << gameObject.layer)) != 0)
                Debug.LogError($"{name}: puppet ground layer mask includes its own layer " +
                               $"({LayerMask.LayerToName(gameObject.layer)}), so it will detect itself " +
                               $"and jump endlessly. Set the mask to the ground layers only.", this);
        }

        private void FixedUpdate()
        {
            UpdateGroundCheck();

            if (_isLocked || _isDead || _config == null) return;

            // moveInput already has the level's invert-controls rule baked in.
            Move(_inputReader.moveInput);
            ApplyFallMultiplier();
        }

        #region Control

        public void Unlock()
        {
            if (_isDead || !_isLocked) return;
            _isLocked = false;
            Subscribe();
        }

        public void Lock()
        {
            _isLocked = true;
            Unsubscribe();
            StopMoving();
        }

        private void Subscribe()
        {
            if (_isSubscribed || !_canJump || _inputReader == null) return;
            _inputReader.OnJumpPerformed += HandleJump;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _inputReader == null) return;
            _inputReader.OnJumpPerformed -= HandleJump;
            _isSubscribed = false;
        }

        #endregion

        #region Movement

        private void Move(float moveInput)
        {
            _rb.linearVelocityX = _config.speed * moveInput;
            if (_flipWithMoveDirection) Flip(moveInput);
        }

        private void StopMoving()
        {
            if (_rb == null) return;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        private void Flip(float moveInput)
        {
            if (_isFacingRight && moveInput < 0 || !_isFacingRight && moveInput > 0)
            {
                _isFacingRight = !_isFacingRight;
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (_isFacingRight ? 1 : -1);
                transform.localScale = scale;
            }
        }

        private void UpdateGroundCheck()
        {
            if (_groundCheckPos == null)
            {
                _isGrounded = true;
                return;
            }

            _isGrounded = Physics2D.OverlapBox(_groundCheckPos.position, _groundCheckSize, 0f, _groundLayer);
            if (_isGrounded) _airJumpsUsed = 0;
        }

        private void HandleJump()
        {
            if (_isLocked || _isDead || !_canJump || _config == null || !CanJump()) return;
            if (!_isGrounded) _airJumpsUsed++;
            _rb.linearVelocityY = _config.jumpForce * _jumpForceMultiplier;
        }

        /// (0 = none, 1 = double jump, negative = unlimited)
        private bool CanJump() => _isGrounded || _maxAirJumps < 0 || _airJumpsUsed < _maxAirJumps;

        private void ApplyFallMultiplier()
        {
            if (_rb.linearVelocityY >= 0f) return;
            _rb.linearVelocityY += Physics2D.gravity.y * _rb.gravityScale * _config.fallMultiplier * Time.fixedDeltaTime;
        }

        /// Pushed in by the level flow on load so a puppet obeys the same per-level
        /// physics rules the player would have.
        public void ApplyRules(LevelRules rules)
        {
            rules ??= LevelRules.Default;
            _rb.gravityScale = _baseGravityScale * rules.GravityScale;
            _maxAirJumps = rules.MaxAirJumps;
            _jumpForceMultiplier = rules.JumpForceMultiplier;
            _airJumpsUsed = 0;
        }

        #endregion

        #region Death

        /// The puppet stands in for the player, so its death is the player's death.
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;
            Lock();
            if (_playerController != null) _playerController.Kill();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDead || _isLocked) return;

            if (other.gameObject.CompareTag(_obstacleTag))
            {
                Die();
            }
            else if (other.gameObject.CompareTag(_fallingGroundTag))
            {
                transform.SetParent(other.transform);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag(_fallingGroundTag) && gameObject.activeInHierarchy)
            {
                transform.SetParent(_originalParent);
            }
        }

        #endregion

        private void OnDestroy() => Unsubscribe();

        #region Gizmo

        private void OnDrawGizmos()
        {
            if (_groundCheckPos == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(_groundCheckPos.position, _groundCheckSize);
        }

        #endregion
    }
}
