using UnityEngine;

namespace DZ.Features
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("moveX");
        private static readonly int MoveYHash = Animator.StringToHash("moveY");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int DeadHash = Animator.StringToHash("dead");

        private Animator _playerAnimator;

        private void Awake()
        {
            _playerAnimator = GetComponent<Animator>();
        }

        public void PlayMoveAnimation(float moveInput, bool isGrounded)
        {
            _playerAnimator.SetInteger(MoveXHash, (int)Mathf.Abs(moveInput));
            _playerAnimator.SetBool(IsGroundedHash, isGrounded);
        }

        public void PlayJumpAnimation(float jumpInput, bool isGrounded)
        {
            _playerAnimator.SetInteger(MoveYHash, GetVerticalDirection(jumpInput, isGrounded));
            _playerAnimator.SetBool(IsGroundedHash, isGrounded);
        }

        private int GetVerticalDirection(float verticalVelocity, bool isGrounded)
        {
            if (isGrounded) return 0;
            if (verticalVelocity > 0) return 1;
            if (verticalVelocity < 0) return -1;
            return 0;
        }
    }
}
