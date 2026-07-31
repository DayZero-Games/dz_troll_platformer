using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DZ.Core;
using DZ.Core.Contracts;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    public class LevelExitDoor : MonoBehaviour
    {
        [Inject] private readonly ISignalBus _signalBus;
        [Header("Refrences")]
        [SerializeField] private Animator _doorAnimator;
        [Tooltip("Where the player lines up before fading out. Child of the door.")]
        [SerializeField] private Transform _entryAnchor;
        [SerializeField] private string _playerTag = "Player";

        [Header("Timing")]
        [SerializeField] private float _walkInDuration = 0.2f;
        [SerializeField] private float _playerFadeDuration = 0.3f;
        [SerializeField] private float _doorCloseDuration = 1f;

        private static readonly int CloseHash = Animator.StringToHash("Close");
        private bool _isRunning;


        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (_entryAnchor == null) _entryAnchor = transform;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isRunning || !other.CompareTag(_playerTag)) return;
            if (!other.TryGetComponent(out PlayerController player)) return;
            RunExitSequenceAsync(player, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunExitSequenceAsync(PlayerController player, CancellationToken ct)
        {
            _isRunning = true;

            try
            {
                // Must come first: the movement states drive linearVelocity every
                // FixedUpdate, which would fight the position tween below.
                player.LockPlayer();

                //Bring Player To Door
                await Tween.Position(player.transform, _entryAnchor.position,
                                     _walkInDuration, Ease.OutQuad)
                           .ToUniTask().AttachExternalCancellation(ct);
                //Fade Player
                await Tween.Alpha(player.SpriteRenderer, 0f,
                                  _playerFadeDuration, Ease.InQuad)
                           .ToUniTask().AttachExternalCancellation(ct);
                //Play Door Close Anim
                if (_doorAnimator != null) _doorAnimator.SetTrigger(CloseHash);
                await UniTask.Delay(TimeSpan.FromSeconds(_doorCloseDuration), cancellationToken: ct);

                _signalBus.Publish(new LevelExitReachedSignal());
            }
            catch (OperationCanceledException)
            {
                // level torn down mid-sequence
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
