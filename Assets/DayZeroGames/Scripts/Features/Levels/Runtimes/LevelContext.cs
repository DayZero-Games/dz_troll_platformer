using System;
using System.Collections.Generic;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public sealed class LevelPuppetSlot
    {
        public const string IdFieldName = "_id";
        public const string PuppetFieldName = "_puppet";

        [Tooltip("Unique ID used by level actions. Defaults to the puppet GameObject name.")]
        [SerializeField] private string _id;

        [SerializeField] private PuppetController _puppet;

        public LevelPuppetSlot()
        {
        }

        public LevelPuppetSlot(string id, PuppetController puppet)
        {
            _id = id;
            _puppet = puppet;
        }

        public string Id => _id;
        public PuppetController Puppet => _puppet;
        public bool IsValid => _puppet != null;

        internal void SetId(string id) => _id = id;
    }

    public sealed class LevelContext : MonoBehaviour
    {
        public const string SpawnPointFieldName = "_SpawnPoint";
        public const string PuppetsFieldName = "_puppets";
        public const string StartControlTargetFieldName = "_startControlTarget";
        public const string StartPuppetIdFieldName = "_startPuppetId";

        [SerializeField] private Transform _SpawnPoint;

        [Tooltip("Puppets available to this level. IDs default to GameObject names and must be unique.")]
        [SerializeField] private List<LevelPuppetSlot> _puppets = new();

        [SerializeField] private LevelControlTarget _startControlTarget = LevelControlTarget.Player;

        [SerializeField] private string _startPuppetId;

        [SerializeField, HideInInspector] private PuppetController _puppet;
        [SerializeField, HideInInspector] private bool _startControllingPuppet = true;

        public Transform SpawnPoint => _SpawnPoint;
        public IReadOnlyList<LevelPuppetSlot> PuppetSlots => _puppets ??= new List<LevelPuppetSlot>();
        public bool HasPuppets => FindFirstValidPuppetSlot() != null;
        public LevelControlTarget StartControlTarget => HasPuppets ? _startControlTarget : LevelControlTarget.Player;
        public string StartPuppetId => _startPuppetId;

        private void Awake()
        {
            NormalizeConfiguration();

            if(_SpawnPoint == null) Debug.LogError($"{name}: no spawn point assigned",this);

            if (TryGetDuplicatePuppetId(out var duplicateId))
            {
                Debug.LogError(
                    $"{name}: multiple puppets use the ID '{duplicateId}'. Puppet IDs must be unique.",
                    this);
            }

            if (_startControlTarget == LevelControlTarget.Puppet &&
                !TryGetPuppet(_startPuppetId, out _))
            {
                Debug.LogWarning(
                    $"{name}: start control is set to puppet, but no matching puppet is registered.",
                    this);
            }
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
        }

        public bool TryGetPuppet(string puppetId, out PuppetController puppet)
        {
            var slot = FindPuppetSlot(puppetId);
            if (slot == null && string.IsNullOrWhiteSpace(puppetId))
                slot = FindFirstValidPuppetSlot();

            puppet = slot?.Puppet;
            return puppet != null;
        }

        public IEnumerable<PuppetController> GetPuppets()
        {
            if (_puppets == null) yield break;

            foreach (var slot in _puppets)
            {
                if (slot?.Puppet != null)
                    yield return slot.Puppet;
            }
        }

        private void NormalizeConfiguration()
        {
            _puppets ??= new List<LevelPuppetSlot>();

            MigrateLegacyPuppet();
            EnsureDefaultPuppetIds();
            EnsureStartPuppetId();
        }

        private void MigrateLegacyPuppet()
        {
            if (_puppet == null) return;

            var slot = FindPuppetSlot(_puppet);
            if (slot == null)
            {
                slot = new LevelPuppetSlot(_puppet.gameObject.name, _puppet);
                _puppets.Add(slot);
            }
            else if (string.IsNullOrWhiteSpace(slot.Id))
            {
                slot.SetId(_puppet.gameObject.name);
            }

            if (_startControllingPuppet)
            {
                _startControlTarget = LevelControlTarget.Puppet;
                _startPuppetId = slot.Id;
            }

            _puppet = null;
            _startControllingPuppet = false;
        }

        private void EnsureDefaultPuppetIds()
        {
            foreach (var slot in _puppets)
            {
                if (slot == null || slot.Puppet == null) continue;

                slot.SetId(string.IsNullOrWhiteSpace(slot.Id)
                    ? slot.Puppet.gameObject.name
                    : slot.Id.Trim());
            }
        }

        private void EnsureStartPuppetId()
        {
            if (_startControlTarget != LevelControlTarget.Puppet) return;

            if (TryGetPuppet(_startPuppetId, out _)) return;

            var firstSlot = FindFirstValidPuppetSlot();
            _startPuppetId = firstSlot?.Id ?? string.Empty;
        }

        private LevelPuppetSlot FindPuppetSlot(string puppetId)
        {
            if (_puppets == null || string.IsNullOrWhiteSpace(puppetId)) return null;

            foreach (var slot in _puppets)
            {
                if (slot == null || slot.Puppet == null) continue;
                if (string.Equals(slot.Id, puppetId, StringComparison.Ordinal))
                    return slot;
            }

            return null;
        }

        private LevelPuppetSlot FindPuppetSlot(PuppetController puppet)
        {
            if (_puppets == null || puppet == null) return null;

            foreach (var slot in _puppets)
            {
                if (slot?.Puppet == puppet)
                    return slot;
            }

            return null;
        }

        private LevelPuppetSlot FindFirstValidPuppetSlot()
        {
            if (_puppets == null) return null;

            foreach (var slot in _puppets)
            {
                if (slot?.Puppet != null)
                    return slot;
            }

            return null;
        }

        private bool TryGetDuplicatePuppetId(out string duplicateId)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in _puppets)
            {
                if (slot == null || !slot.IsValid || string.IsNullOrWhiteSpace(slot.Id)) continue;
                if (!ids.Add(slot.Id))
                {
                    duplicateId = slot.Id;
                    return true;
                }
            }

            duplicateId = null;
            return false;
        }

    }
}
