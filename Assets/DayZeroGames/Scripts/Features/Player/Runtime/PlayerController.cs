using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace DZ.Features
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerAnimationController))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [Inject] private readonly IInputReader _inputReader;
        [Inject] private readonly ISignalBus _signalBus;

        [Header("Player Settings")]
        [SerializeField] private PlayerConfigSo playerConfig;
        [SerializeField] private PlayerAnimationController playerAnimationController;

        [Header("Ground Check Sensor Settings")]
        [SerializeField]
        private Transform groundCheckPos;
        [SerializeField] private float checkRadius;
        [SerializeField]private Vector2 _groundCheckSize = new Vector2(0.5f,1f);
        [SerializeField] private LayerMask groundLayer;

        [Header("CameraShaker")]
        [SerializeField] private CameraShaker cameraShaker;

        private Rigidbody2D _playerRb;
        private bool _isFacingRight = true;
        private bool _isGrounded;
        private bool _isDead;

        [Header("Player States")]
        private PlayerStateMachine _playerStateMachine;
        private PlayerIdleState _idleState;
        private PlayerRunState _runState;
        private PlayerJumpState _jumpState;
        private PlayerDeadState _deadState;

        public bool IsGrounded => _isGrounded;
        public bool IsDead => _isDead;
        public Rigidbody2D PlayerRb => _playerRb;
        public PlayerIdleState IdleState => _idleState;
        public PlayerRunState RunState => _runState;
        public PlayerJumpState JumpState => _jumpState;
        public PlayerDeadState DeadState => _deadState;

        private void Awake()
        {
            _playerRb = GetComponent<Rigidbody2D>();
            playerAnimationController ??= GetComponent<PlayerAnimationController>();
        }

        private void Start()
        {
            Debug.Log(_inputReader.moveInput);
            CreatePlayerStates();
            _playerStateMachine.Initialize(_idleState);
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
            _idleState = new PlayerIdleState(this, playerAnimationController, _playerStateMachine, _inputReader);
            _runState = new PlayerRunState(this, playerAnimationController, _playerStateMachine, _inputReader);
            _jumpState = new PlayerJumpState(this, playerAnimationController, _playerStateMachine, _inputReader);
            _deadState  = new PlayerDeadState(this,playerAnimationController,_playerStateMachine,_inputReader);
        }

        private void UpdateGroundCheck()
        {
           // _isGrounded = Physics2D.OverlapCircle(groundCheckPos.position, checkRadius, groundLayer);
            _isGrounded = Physics2D.OverlapBox(groundCheckPos.position,_groundCheckSize,0f,groundLayer);
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

        public void Jump()
        {
            if (!_isGrounded) return;
            _playerRb.linearVelocityY = playerConfig.jumpForce;
        }

        public void ApplyFallMultiplier()
        {
            if (_playerRb.linearVelocityY >= 0f) return;
            _playerRb.linearVelocityY += Physics2D.gravity.y * _playerRb.gravityScale * playerConfig.fallMultiplier * Time.fixedDeltaTime;
        }

        public void Die()
        {
            _isDead = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(_playerStateMachine == null || _isDead) return;
             if(other.gameObject.CompareTag("Obstacles"))
            {
                _playerStateMachine.ChangeState(DeadState);
                cameraShaker.Shake(CameraShakeType.Bump);
                _signalBus.Publish(new PlayerDiedSignal());
                Debug.Log("Player Dead");
            }
        }

        public void TeleportTo(Vector3 position)
        {
            _playerRb.linearVelocity = Vector2.zero;
            _playerRb.angularVelocity = 0f;
            _playerRb.position = position;
            
            Physics2D.SyncTransforms();
            _isDead = false;
            
            _playerStateMachine.Initialize(_idleState);
        }
        


        #region Gizmo
        public void OnDrawGizmos()
        {
            if (groundCheckPos == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            //Gizmos.DrawWireSphere(groundCheckPos.position, checkRadius);
            Gizmos.DrawCube(groundCheckPos.position,_groundCheckSize);
        }
        #endregion
    }
}
