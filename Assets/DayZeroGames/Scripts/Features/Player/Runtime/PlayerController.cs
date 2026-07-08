using System;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    public class PlayerController : MonoBehaviour
    {
        #region Dependencies

        [Inject] private readonly IInputReader _inputReader;

        #endregion

        #region Serialized Fields

        [Header("Player Settings")] [SerializeField]
        private PlayerConfigSo playerConfig;

        [Header("Ground Check Sensor Settings")] [SerializeField]
        private Transform groundCheckPos;

        [SerializeField] private float checkRadius;
        [SerializeField] private LayerMask groundLayer;

        #endregion

        #region Private Fields

        private Rigidbody2D _playerRb;
        private bool _isFacingRight = true;
        private bool _isGrounded;
        private PlayerState _currentPlayerState = PlayerState.Idle;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _playerRb = GetComponent<Rigidbody2D>();
            _inputReader.OnJumpPerformed += Jump;
            ChangeState(PlayerState.Idle);
        }

        private void Update()
        {
            UpdateGroundCheck();
            UpdatePlayerState();
            FlipPlayer();
            if (_playerRb.linearVelocityY < 0.01)
            {
                _playerRb.linearVelocityY -= Physics2D.gravity.y*playerConfig.fallMultiplier*Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        public void OnDrawGizmos()
        {
            if (groundCheckPos == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos.position, checkRadius);
        }

        private void OnDestroy()
        {
            _inputReader.OnJumpPerformed -= Jump;
        }

        #endregion

        #region State Machine

        private void ChangeState(PlayerState newState)
        {
            if (_currentPlayerState == newState) return;
            ExitState(_currentPlayerState);
            _currentPlayerState = newState;
            EnterState(newState);
        }

        private void UpdatePlayerState()
        {
            switch (_currentPlayerState)
            {
                case PlayerState.Idle:
                    HandlePlayerIdle();
                    break;
                case PlayerState.Running:
                    HandlePlayerRunning();
                    break;
                case PlayerState.Jumping:
                    HandlePlayerJump();
                    break;
                case PlayerState.Dead:
                    HandlePlayerDead();
                    break;
            }
        }

        private void EnterState(PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Idle:
                    return;
                case PlayerState.Running:
                    return;
                case PlayerState.Jumping:
                    return;
                case PlayerState.Dead:
                    return;
            }
        }

        private void ExitState(PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Idle:
                    return;
                case PlayerState.Running:
                    return;
                case PlayerState.Jumping:
                    return;
                case PlayerState.Dead:
                    return;
            }
        }

        #endregion

        #region State Handlers

        private void HandlePlayerIdle()
        {
            if (!_isGrounded)
            {
                ChangeState(PlayerState.Jumping);
                return;
            }

            if (_inputReader.moveInput != 0) ChangeState(PlayerState.Running);
        }

        private void HandlePlayerRunning()
        {
            if (!_isGrounded)
            {
                ChangeState(PlayerState.Jumping);
                return;
            }

            if (_inputReader.moveInput == 0) ChangeState(PlayerState.Idle);
        }

        private void HandlePlayerJump()
        {
            if (!_isGrounded || _playerRb.linearVelocityY>0.01f) return;
            if (_inputReader.moveInput == 0) ChangeState(PlayerState.Idle);
            else ChangeState(PlayerState.Running);
        }

        private void HandlePlayerDead()
        {
            Debug.Log("Player Dead");
        }

        #endregion

        #region Movement

        private void UpdateGroundCheck()
        {
            _isGrounded = Physics2D.OverlapCircle(groundCheckPos.position, checkRadius, groundLayer);
        }

        private void FlipPlayer()
        {
            if (_isFacingRight && _inputReader.moveInput < 0 || !_isFacingRight && _inputReader.moveInput > 0)
            {
                _isFacingRight = !_isFacingRight;
                transform.localScale = new Vector3(_isFacingRight ? 1 : -1, 1, 1);
            }
        }

        private void MovePlayer()
        {
            _playerRb.linearVelocityX = playerConfig.speed * _inputReader.moveInput;
        }

        private void Jump()
        {
            if (!_isGrounded) return;
            _playerRb.linearVelocityY = playerConfig.jumpForce;
        }

        #endregion
    }
}
