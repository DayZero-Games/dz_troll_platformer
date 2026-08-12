using System;
using UnityEngine;

namespace DZ.Features
{
    public sealed class LevelContext : MonoBehaviour
    {
        [SerializeField] private Transform _SpawnPoint;
        public Transform SpawnPoint => _SpawnPoint;
        private void Awake()
        {
            if(_SpawnPoint == null) Debug.LogError($"{name}: no spawn point assigned",this);
        }
    }
}
