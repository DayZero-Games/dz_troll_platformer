using System;
using DZ.Core.Contracts;
using UnityEngine;
using VInspector;

namespace DZ.Core.Runtime
{
	public class AudioService : MonoBehaviour, IAudioService
	{
		[Header("AudioSources")] [SerializeField]
		private AudioSource musicSource;

		[SerializeField] private AudioSource sfxSource;

		[SerializeField] private AudioLibrarySo _audioLibrary;
		//TODO Use Audio Library Interface Here and initialize the IAudioLibrary from Vinspector. Use COnstructor. 
		//private IAudioLibrary _audioLibrary;


		private void Start()
		{
			PlayMusic(AudioId.BackgroundMusic);
		}

		public void PlaySfx(AudioId audioId)
		{
			if (!_audioLibrary.TryGet(audioId, out var audioEntry))
			{
				Debug.Log($"{audioId} not found");
				return;
			}
			sfxSource.PlayOneShot(audioEntry.clip, audioEntry.volume);
		}

		public void PlayMusic(AudioId audioId)
		{
			if (!_audioLibrary.TryGet(audioId, out var audioEntry)) return;

			musicSource.Stop();
			musicSource.clip = audioEntry.clip;
			musicSource.volume = audioEntry.volume;
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