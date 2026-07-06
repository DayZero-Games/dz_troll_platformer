using System;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    public class PlayerController : MonoBehaviour
    {
       [Inject] private readonly IInputReader _inputReader;
       
       [Header("Player Settings")]
       [SerializeField] private PlayerConfigSo playerConfig;

       [Header("Ground Check Sensor Settings")]
       [SerializeField] private Transform groundCheckPos;
       [SerializeField] private float checkRadius;
       [SerializeField] private LayerMask groundLayer;
       
       private Rigidbody2D _playerRb;
       private bool _isFacingRight = true;
       private bool _isGrounded;

        private void Start()
        { 
            _playerRb = GetComponent<Rigidbody2D>();
            _inputReader.OnJumpPerformed += Jump;
        }

        private void Update()
        {
            MovePlayer();
            FlipPlayer();
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
            if (_isGrounded)
            {
                Debug.Log("Player Jumped");
            }
            
        }

        public void OnDrawGizmos()
        {
            if (groundCheckPos == null) return;
           Gizmos.color = _isGrounded ? Color.green : Color.red;
           Gizmos.DrawWireSphere(groundCheckPos.position, checkRadius);
        }
    }
}
