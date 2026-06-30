using DZ.Core.Contracts;
using DZ.Core.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace DZ.Core
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("AudioSources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        
        private IAudioLibrary _audioLibrary;

        public void PlaySfx(AudioId audioId)
        {
            if (!_audioLibrary.TryGet(audioId, out var audioEntry)) return;
            sfxSource.PlayOneShot(audioEntry.clip);
        }

        public void PlayMusic(AudioId audioId)
        {
            if (!_audioLibrary.TryGet(audioId, out var audioEntry)) return;
            
            musicSource.Stop();
            musicSource.clip = audioEntry.clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }
    }
}
