namespace VisualNovel.Novel
{
    /// <summary>
    /// Аудио-выход ядра (паттерн Strategy): музыка, эффекты, озвучка.
    /// Озвучка зарезервирована — реализацию добавим, когда появятся записи.
    /// Сейчас рабочие реализации: <see cref="NullAudioService"/> (тишина)
    /// и FmodAudioService (мост к FMOD, пока он не настроен — только логи).
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Сменить фоновую музыку. Пустой id — оставить текущую.</summary>
        void PlayMusic(string musicId);

        void StopMusic();

        /// <summary>Разовый звуковой эффект.</summary>
        void PlaySfx(string sfxId);

        /// <summary>Озвучка реплики — ЗАРЕЗЕРВИРОВАНО.</summary>
        void PlayVoice(string voiceId);
    }
}
