using System;
using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace DZ.Features
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerAnimationController))]
    public class PlayerController : MonoBehaviour, IPlayerController, ILevelAvatar
    {
        [Inject] private readonly IInputReader _inputReader;
        [Inject] private readonly IAudioService _audioService;
        [Inject] private readonly ISignalBus _signalBus;

        [Header("Player Settings")]
        [SerializeField] private PlayerConfigSo playerConfig;
        [SerializeField] private PlayerAnimationController playerAnimationController;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Ground Check Sensor Settings")]
        [SerializeField]
        private Transform groundCheckPos;
        [SerializeField] private float checkRadius;
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.5f, 1f);
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D _playerRb;
        private bool _isFacingRight = true;
        private bool _isGrounded;
        private bool _isDead;

        // Per-level rules, pushed in by the level flow on every load. Runtime only -
        // playerConfig stays the shared baseline and is never written to.
        private float _baseGravityScale = 1f;
        private int _maxAirJumps;
        private float _jumpForceMultiplier = 1f;
        private int _airJumpsUsed;
        private bool _jumpEnabled = true;

        [Header("Player States")]
        private PlayerStateMachine _playerStateMachine;
        private PlayerIdleState _idleState;
        private PlayerRunState _runState;
        private PlayerJumpState _jumpState;
        private PlayerDeadState _deadState;
        private PlayerLockedState _lockedState;

        public bool IsGrounded => _isGrounded;
        public bool IsDead => _isDead;
        public bool IsLocked => _playerStateMachine.CurrentState == _lockedState;
        public Rigidbody2D PlayerRb => _playerRb;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public PlayerIdleState IdleState => _idleState;
        public PlayerRunState RunState => _runState;
        public PlayerJumpState JumpState => _jumpState;
        public PlayerDeadState DeadState => _deadState;
        public PlayerLockedState LockedState => _lockedState;

        private void Awake()
        {
            _playerRb = GetComponent<Rigidbody2D>();
            playerAnimationController ??= GetComponent<PlayerAnimationController>();
            _baseGravityScale = _playerRb.gravityScale;

            
            
            CreatePlayerStates();
        }

        private void Start()
        {
            
            
            if (_playerStateMachine.CurrentState == null)
            {
                _playerStateMachine.Initialize(_idleState);
            }
        }

        private void Update()
        {
            _playerStateMachine.Update();
        }

        private void FixedUpdate()
        {
            UpdateGroundCheck();
            _playerStateMachine.FixedUpdate();
        }

        private void CreatePlayerStates()
        {
            _playerStateMachine = new PlayerStateMachine();

            var context = new PlayerContext(this, playerAnimationController, _playerStateMachine, _inputReader,_audioService,_signalBus);

            _idleState = new PlayerIdleState(context);
            _runState = new PlayerRunState(context);
            _jumpState = new PlayerJumpState(context);
            _deadState = new PlayerDeadState(context);
            _lockedState = new PlayerLockedState(context);
        }
        public void LockPlayer() => _playerStateMachine.ChangeState(_lockedState);

        public void UnlockPlayer() => _playerStateMachine.ChangeState(_idleState);
        Transform ILevelAvatar.Transform => transform;
        void ILevelAvatar.Lock() => LockPlayer();
        void ILevelAvatar.Unlock() => UnlockPlayer();
        
        public void Kill()
        {
            if (_isDead || _playerStateMachine == null) return;
            _playerStateMachine.ChangeState(_deadState);
        }

        private void UpdateGroundCheck()
        {
            
            _isGrounded = Physics2D.OverlapBox(groundCheckPos.position, _groundCheckSize, 0f, groundLayer);
            if (_isGrounded) _airJumpsUsed = 0;
        }

        private void FlipPlayer(float moveInput)
        {
            if (_isFacingRight && moveInput < 0 || !_isFacingRight && moveInput > 0)
            {
                _isFacingRight = !_isFacingRight;
                transform.localScale = new Vector3(_isFacingRight ? 1 : -1, 1, 1);
            }
        }

        public void MovePlayer(float moveInput)
        {
            _playerRb.linearVelocityX = playerConfig.speed * moveInput;
            FlipPlayer(moveInput);
        }
        public void StopMovingPlayer()
        {
            _playerRb.linearVelocityX = 0;
        }
        
        /// (0 = none, 1 = double jump, negative = unlimited)
        public bool CanJump() =>
            _jumpEnabled &&
            _jumpForceMultiplier > 0f &&
            (_isGrounded || _maxAirJumps < 0 || _airJumpsUsed < _maxAirJumps);

        public bool Jump()
        {
            if (!CanJump()) return false;
            if (!_isGrounded) _airJumpsUsed++;
            _playerRb.linearVelocityY = playerConfig.jumpForce * _jumpForceMultiplier;
            return true;
        }

        /// level Rules applied to player so that levels cannot inherit the previous level's physics.
        public void ApplyRules(LevelRules rules)
        {
            rules ??= LevelRules.Default;
            SetGravityScale(rules.GravityScale);
            SetJumpRules(rules.MaxAirJumps, rules.JumpForceMultiplier);
            SetJumpEnabled(rules.JumpForceMultiplier > 0f);
        }

        public void SetGravityScale(float gravityScale)
        {
            _playerRb.gravityScale = _baseGravityScale * gravityScale;
        }

        public void SetJumpRules(int maxAirJumps, float jumpForceMultiplier)
        {
            _maxAirJumps = maxAirJumps;
            _jumpForceMultiplier = jumpForceMultiplier;
            _airJumpsUsed = 0;
        }

        public void SetJumpEnabled(bool enabled)
        {
            _jumpEnabled = enabled;
            if (!enabled) _airJumpsUsed = 0;
        }

        public void ApplyFallMultiplier()
        {
            if (_playerRb.linearVelocityY >= 0f) return;
            _playerRb.linearVelocityY += Physics2D.gravity.y * _playerRb.gravityScale * playerConfig.fallMultiplier * Time.fixedDeltaTime;
        }

        public void Die()
        {
            this.transform.SetParent(null);
            _isDead = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_playerStateMachine == null || _isDead || IsLocked) return;
            if (other.gameObject.CompareTag("Obstacles"))
            {
                _playerStateMachine.ChangeState(DeadState);
            }
            else if (other.gameObject.CompareTag("FallingGround"))
            {
                this.transform.SetParent(other.transform);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("FallingGround") && this.gameObject.activeInHierarchy)
            {
                this.transform.SetParent(null);
            }
        }


        public void TeleportTo(Vector3 position)
        {
            _playerRb.linearVelocity = Vector2.zero;
            _playerRb.angularVelocity = 0f;
            _playerRb.position = position;
            transform.position = position;

            Physics2D.SyncTransforms();
            _isDead = false;
            _airJumpsUsed = 0;

            RestoreSpriteAlpha();
        }
        private void RestoreSpriteAlpha()
        {
            var c = _spriteRenderer.color;
            c.a = 1f;
            _spriteRenderer.color = c;
        }

        public void OnDestroy()
        {
            _playerStateMachine.ShutDown();
        }


        #region Gizmo
        public void OnDrawGizmos()
        {
            if (groundCheckPos == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            
            Gizmos.DrawCube(groundCheckPos.position, _groundCheckSize);
        }
        #endregion
    }
}
