using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerAnimationController))]
    public class PlayerController : MonoBehaviour
    {
        [Inject] private readonly IInputReader _inputReader;

        [Header("Player Settings")]
        [SerializeField] private PlayerConfigSo playerConfig;
        [SerializeField] private PlayerAnimationController playerAnimationController;

        [Header("Ground Check Sensor Settings")]
        [SerializeField]
        private Transform groundCheckPos;
        [SerializeField] private float checkRadius;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D _playerRb;
        private bool _isFacingRight = true;
        private bool _isGrounded;

        [Header("Player States")]
        private PlayerStateMachine _playerStateMachine;
        private PlayerIdleState _idleState;
        private PlayerRunState _runState;
        private PlayerJumpState _jumpState;

        public bool IsGrounded => _isGrounded;
        public Rigidbody2D PlayerRb => _playerRb;
        public PlayerIdleState IdleState => _idleState;
        public PlayerRunState RunState => _runState;
        public PlayerJumpState JumpState => _jumpState;


        private void Awake()
        {
            _playerRb = GetComponent<Rigidbody2D>();
            playerAnimationController ??= GetComponent<PlayerAnimationController>();
        }

        private void Start()
        {
            CreatePlayerStates();
            _playerStateMachine.Initialize(_idleState);
        }

        private void Update()
        {
            UpdateGroundCheck();
            _playerStateMachine.Update();
        }

        private void FixedUpdate()
        {
            _playerStateMachine.FixedUpdate();
        }

        private void CreatePlayerStates()
        {
            _playerStateMachine = new PlayerStateMachine();
            _idleState = new PlayerIdleState(this,playerAnimationController, _playerStateMachine, _inputReader);
            _runState = new PlayerRunState(this,playerAnimationController ,_playerStateMachine, _inputReader);
            _jumpState = new PlayerJumpState(this,playerAnimationController ,_playerStateMachine, _inputReader);
        }

        private void UpdateGroundCheck()
        {
            _isGrounded = Physics2D.OverlapCircle(groundCheckPos.position, checkRadius, groundLayer);
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

        #region Gizmo
        public void OnDrawGizmos()
        {
            if (groundCheckPos == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos.position, checkRadius);
        }
        #endregion
    }
}
