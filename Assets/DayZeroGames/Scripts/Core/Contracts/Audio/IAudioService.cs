namespace DZ.Core.Contracts
{
    public interface IAudioService 
    {
      void PlaySfx(AudioId audioId);
      void PlayMusic(AudioId audioId);
      void StopMusic();
      void PauseMusicForOverlay();
      void ResumeMusicAfterOverlay();

      bool MusicEnabled { get;}
      bool SfxEnabled { get;}

      void SetMusicEnabled(bool enabled);
      void SetSfxEnabled(bool enabled);
    }
}
