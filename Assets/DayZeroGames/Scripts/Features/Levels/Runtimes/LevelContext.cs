using System;
using UnityEngine;

namespace DZ.Features
{
    public sealed class LevelContext : MonoBehaviour
    {
        [SerializeField] private Transform _SpawnPoint;
        [SerializeField] private GameObject _exitDoor;
        
        public Transform SpawnPoint => _SpawnPoint;
        public GameObject ExitDoor => _exitDoor;

        private void Awake()
        {
            if(_SpawnPoint == null) Debug.LogError($"{name}: no spawn point assigned",this);
            if(_exitDoor == null) Debug.LogError($"{name}: no spawn door assigned",this);
        }
    }
}
