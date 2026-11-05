namespace VisualNovel.Novel
{
    /// <summary>
    /// Заглушка аудио (Null Object): ничего не играет, ничего не требует.
    /// Используется по умолчанию, пока не подключён FMOD.
    /// </summary>
    public sealed class NullAudioService : IAudioService
    {
        public static readonly NullAudioService Instance = new();

        private NullAudioService() { }

        public void PlayMusic(string musicId) { }
        public void StopMusic() { }
        public void PlaySfx(string sfxId) { }
        public void PlayVoice(string voiceId) { }
    }
}
