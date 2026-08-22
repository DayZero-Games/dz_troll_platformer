using System;
using System.Collections.Generic;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Features
{
    [Serializable]
    public sealed class LevelPuppetSlot
    {
        public const string PuppetFieldName = "_puppet";

        [SerializeField] private PuppetController _puppet;

        public LevelPuppetSlot()
        {
        }

        public LevelPuppetSlot(PuppetController puppet)
        {
            _puppet = puppet;
        }

        public string Id => _puppet != null ? _puppet.gameObject.name : string.Empty;
        public PuppetController Puppet => _puppet;
        public bool IsValid => _puppet != null;
    }

    public sealed class LevelContext : MonoBehaviour
    {
        public const string SpawnPointFieldName = "_SpawnPoint";
        public const string PuppetsFieldName = "_puppets";
        public const string StartControlTargetFieldName = "_startControlTarget";
        public const string StartPuppetIdFieldName = "_startPuppetId";

        [SerializeField] private Transform _SpawnPoint;

        [Tooltip("Puppets available to this level. Each puppet GameObject name must be unique.")]
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

            if (TryGetDuplicatePuppetName(out var duplicateName))
            {
                Debug.LogError(
                    $"{name}: multiple puppets use the GameObject name '{duplicateName}'. Puppet names must be unique.",
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
            EnsureStartPuppetId();
        }

        private void MigrateLegacyPuppet()
        {
            if (_puppet == null) return;

            var slot = FindPuppetSlot(_puppet);
            if (slot == null)
            {
                slot = new LevelPuppetSlot(_puppet);
                _puppets.Add(slot);
            }

            if (_startControllingPuppet)
            {
                _startControlTarget = LevelControlTarget.Puppet;
                _startPuppetId = slot.Id;
            }

            _puppet = null;
            _startControllingPuppet = false;
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

        private bool TryGetDuplicatePuppetName(out string duplicateName)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var puppet in GetPuppets())
            {
                var puppetName = puppet.gameObject.name;
                if (!names.Add(puppetName))
                {
                    duplicateName = puppetName;
                    return true;
                }
            }

            duplicateName = null;
            return false;
        }

    }
}
