using System;
using System.Collections.Generic;
using UnityEngine;

namespace DZ.Core.Runtime
{
    [CreateAssetMenu(fileName = "AudioLibrarySO", menuName = "Scriptable Objects/AudioLibrarySO")]
    public class AudioLibrarySo : ScriptableObject,IAudioLibrary
    {
        [SerializeField] public List<AudioEntry> audioEntries =new();
        private readonly Dictionary<AudioId, AudioEntry> _audioMap = new();

        private void OnEnable()
        {
            BuildMap();
        }

        private void BuildMap()
        {
            _audioMap.Clear();
            foreach (var audioEntry in audioEntries)
            {
                _audioMap.TryAdd(audioEntry.audioId, audioEntry);
            }
        }

        public bool TryGet(AudioId audioId, out AudioEntry audioEntry)
        {
            return _audioMap.TryGetValue(audioId, out audioEntry);
        }
    }
}
