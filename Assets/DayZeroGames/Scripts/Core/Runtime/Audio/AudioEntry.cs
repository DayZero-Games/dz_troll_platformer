using System;
using UnityEngine;

namespace DZ.Core.Runtime
{
    [Serializable]
    public enum AudioId
    {
        None,
        Jump,
        Death
    }
    
    [Serializable]
    public class AudioEntry 
    {
        public AudioId audioId;
        public AudioClip clip;
    }
}
