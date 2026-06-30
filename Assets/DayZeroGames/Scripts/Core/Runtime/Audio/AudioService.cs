using DZ.Core.Contracts;
using DZ.Core.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VInspector;

namespace DZ.Core
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("AudioSources")] [SerializeField]
        private AudioSource musicSource;

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioLibrarySo _audioLibrary;
        //TODO Use Audio Library Interface Here and initialize the IAudioLibrary from Vinspector. Use COnstructor. 
        //private IAudioLibrary _audioLibrary;

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

        [Button("Test Sound")]
        public void TestSound()
        {
            PlaySfx(AudioId.Jump);
        }
    }
}