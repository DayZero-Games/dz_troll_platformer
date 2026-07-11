namespace DZ.Core.Contracts
{
    public interface IAudioService 
    {
      void PlaySfx(AudioId audioId);
      void PlayMusic(AudioId audioId);
      void StopMusic();
    }
}
