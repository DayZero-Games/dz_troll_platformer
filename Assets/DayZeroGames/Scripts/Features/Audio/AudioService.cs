using System;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
	public class AudioService : MonoBehaviour, IAudioService
	{
		[Inject] private readonly IAudioLibrary _audioLibrary;
		
		[Header("AudioSources")]
		[SerializeField] private AudioSource musicSource;
		[SerializeField] private AudioSource sfxSource;


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
	}
}