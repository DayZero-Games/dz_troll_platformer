using System;
using UnityEngine;

namespace DZ.Features
{
    public sealed class LevelContext : MonoBehaviour
    {
        [SerializeField] private Transform _SpawnPoint;

        [Tooltip("Optional. Assign to hand control of this object to the player for the level: " +
                 "the player freezes at the spawn point and this object is driven by input instead.")]
        [SerializeField] private PuppetController _puppet;

        public Transform SpawnPoint => _SpawnPoint;
        public PuppetController Puppet => _puppet;
        public bool HasPuppet => _puppet != null;

        private void Awake()
        {
            if(_SpawnPoint == null) Debug.LogError($"{name}: no spawn point assigned",this);
        }
    }
}
