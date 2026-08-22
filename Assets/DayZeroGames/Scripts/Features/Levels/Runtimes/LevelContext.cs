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

        [Tooltip("When a puppet is assigned, start the level controlling the puppet. Turn this off when a trigger should switch to the puppet later.")]
        [SerializeField] private bool _startControllingPuppet = true;

        public Transform SpawnPoint => _SpawnPoint;
        public PuppetController Puppet => _puppet;
        public bool HasPuppet => _puppet != null;
        public bool StartControllingPuppet => HasPuppet && _startControllingPuppet;

        private void Awake()
        {
            if(_SpawnPoint == null) Debug.LogError($"{name}: no spawn point assigned",this);
        }
    }
}
