using System;
using DZ.Core.Contracts;
using UnityEngine;

namespace DZ.Core.Runtime
{
    [Serializable]
    public class AudioEntry 
    {
        public AudioId audioId;
        public AudioClip clip;
        [Range(0,1)]public float volume;
    }
}
