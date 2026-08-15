using System;
using DZ.Core;
using DZ.Core.Contracts;
using UnityEngine;
using VContainer;

namespace DZ.Features
{
	public class AudioService : MonoBehaviour, IAudioService
	{
		[Inject] private readonly IAudioLibrary _audioLibrary;
		[Inject] private readonly IPlayerPrefsSaveService _playerPrefsSaveService;

		[Header("AudioSources")]
		[SerializeField] private AudioSource musicSource;
		[SerializeField] private AudioSource sfxSource;

		
		
		private AudioId _currentMusic = AudioId.None;

		public bool MusicEnabled { get; private set; } = true;
		public bool SfxEnabled { get; private set; } = true;

		private void Start()
		{
			MusicEnabled = _playerPrefsSaveService.LoadBool(SaveKeys.MusicEnabled, true);
			SfxEnabled = _playerPrefsSaveService.LoadBool(SaveKeys.SfxEnabled, true);
			PlayMusic(AudioId.BackgroundMusic);
		}

		public void PlaySfx(AudioId audioId)
		{
			if (!SfxEnabled) return;

			if (!_audioLibrary.TryGet(audioId, out var audioEntry))
			{
				Debug.LogWarning($"{audioId} not found");
				return;
			}
			sfxSource.PlayOneShot(audioEntry.clip, audioEntry.volume);
		}

		public void PlayMusic(AudioId audioId)
		{
			if (!_audioLibrary.TryGet(audioId, out var audioEntry)) return;
			_currentMusic = audioId;
			if (!MusicEnabled) return;

			musicSource.Stop();
			musicSource.clip = audioEntry.clip;
			musicSource.volume = audioEntry.volume;
			musicSource.Play();
		}

		public void StopMusic() => musicSource.Stop();

		public void SetMusicEnabled(bool enabled)
		{
			MusicEnabled = enabled;
			_playerPrefsSaveService.SaveBool(SaveKeys.MusicEnabled, enabled);
			if (enabled) PlayMusic(_currentMusic);
			else StopMusic();
		}

		public void SetSfxEnabled(bool enabled)
		{
			SfxEnabled = enabled;
			_playerPrefsSaveService.SaveBool(SaveKeys.SfxEnabled, enabled);
		}
	}
}