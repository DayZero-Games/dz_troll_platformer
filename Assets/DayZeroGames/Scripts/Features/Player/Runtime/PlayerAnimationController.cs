using UnityEngine;

namespace DZ.Features
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int JumpUp = Animator.StringToHash("JumpUp");
        private static readonly int JumpDown = Animator.StringToHash("JumpDown");
        private static readonly int Dead = Animator.StringToHash("Dead");


        private Animator _playerAnimator;
        private int _currentHash;

        private void Awake()
        {
            _playerAnimator = GetComponent<Animator>();
        }

        public void PlayIdleAnimation()
        {
            ChangeAnimation(Idle);
        }

        public void PlayRunAnimation()
        {
            ChangeAnimation(Run);
        }

        public void PlayJumpUpAnimation()
        {
            ChangeAnimation(JumpUp);
        }

        public void PlayJumpDownAnimation()
        {
            ChangeAnimation(JumpDown);
        }

        public void PlayDeadAnimation()
        {
            ChangeAnimation(Dead);
        }

        private void ChangeAnimation(int stateHash)
        {
            if (_currentHash == stateHash) return;
            _currentHash = stateHash;
            _playerAnimator.CrossFadeInFixedTime(_currentHash,0f);
        }
    }
}
