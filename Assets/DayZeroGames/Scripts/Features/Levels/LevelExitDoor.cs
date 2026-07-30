using System;
using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
    public class LevelExitDoor : MonoBehaviour
    {
        [Inject] private readonly ISignalBus _signalBus;
        [SerializeField] private string _playerTag = "Player";

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == _playerTag)
            {
                _signalBus.Publish(new LevelCompletedSignal(0));
            }
        }
    }
}
