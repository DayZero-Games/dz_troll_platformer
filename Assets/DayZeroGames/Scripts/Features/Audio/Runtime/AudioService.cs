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
		private bool _musicPausedForOverlay;

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

			if (_currentMusic == audioId && musicSource.clip == audioEntry.clip) return;

			_currentMusic = audioId;
			if (!MusicEnabled) return;

			musicSource.Stop();
			musicSource.clip = audioEntry.clip;
			musicSource.volume = audioEntry.volume;
			musicSource.Play();

			if (_musicPausedForOverlay) musicSource.Pause();
		}

public void StopMusic()
		{
			musicSource.Stop();
			_musicPausedForOverlay = false;
		}

public void PauseMusicForOverlay()
		{
			if (!MusicEnabled || !musicSource.isPlaying) return;

			musicSource.Pause();
			_musicPausedForOverlay = true;
		}

		public void ResumeMusicAfterOverlay()
		{
			if (!_musicPausedForOverlay) return;

			_musicPausedForOverlay = false;

			if (MusicEnabled && musicSource.clip != null)
			{
				musicSource.UnPause();
			}
		}


public void SetMusicEnabled(bool enabled)
		{
			MusicEnabled = enabled;
			_playerPrefsSaveService.SaveBool(SaveKeys.MusicEnabled, enabled);

			if (enabled)
			{
				if (_musicPausedForOverlay) return;

				if (musicSource.clip != null)
				{
					musicSource.UnPause();
				}
				else
				{
					PlayMusic(_currentMusic);
				}
			}
			else
			{
				musicSource.Pause();
			}
		}

		public void SetSfxEnabled(bool enabled)
		{
			SfxEnabled = enabled;
			_playerPrefsSaveService.SaveBool(SaveKeys.SfxEnabled, enabled);
		}
	}
}