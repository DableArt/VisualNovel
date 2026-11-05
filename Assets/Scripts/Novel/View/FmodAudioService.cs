using System;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Адаптер к FMOD. FMOD в проекте стоит, но пока НЕ настроен, поэтому
    /// обращение идёт через рефлексию: если FMODUnity.RuntimeManager недоступен
    /// или событие не найдено в банке — пишем предупреждение в консоль,
    /// игра не падает. После настройки FMOD Studio события начнут играть
    /// автоматически (id в JSON = путь события, напр. "event:/Music/Theme").
    ///
    /// Озвучка (PlayVoice) ЗАРЕЗЕРВИРОВАНА: сейчас только лог; пути событий
    /// согласуем, когда появятся записи голоса.
    /// </summary>
    public sealed class FmodAudioService : MonoBehaviour, IAudioService
    {
        private static Type runtimeManagerType;
        private static bool runtimeManagerResolved;
        private static bool missingTypeWarned;

        private object musicInstance; // упакованный FMOD.Studio.EventInstance

        public void PlayMusic(string musicId)
        {
            StopMusic();
            if (string.IsNullOrEmpty(musicId)) return;

            var rm = ResolveRuntimeManager();
            if (rm == null) return;

            try
            {
                var create = rm.GetMethod("CreateInstance", new[] { typeof(string) });
                musicInstance = create?.Invoke(null, new object[] { musicId });
                musicInstance?.GetType().GetMethod("start")?.Invoke(musicInstance, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Novel] FMOD: музыка '{musicId}' не проиграна: {e.Message}", this);
                musicInstance = null;
            }
        }

        public void StopMusic()
        {
            if (musicInstance == null) return;

            try
            {
                var type = musicInstance.GetType();
                var stopMode = Enum.Parse(type.Assembly.GetType("FMOD.Studio.STOP_MODE"), "ALLOWFADEOUT");
                type.GetMethod("stop")?.Invoke(musicInstance, new[] { stopMode });
                type.GetMethod("release")?.Invoke(musicInstance, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Novel] FMOD: ошибка остановки музыки: {e.Message}", this);
            }

            musicInstance = null;
        }

        public void PlaySfx(string sfxId) => PlayOneShotSafe("SFX", sfxId);

        public void PlayVoice(string voiceId)
        {
            // ЗАРЕЗЕРВИРОВАНО под озвучку. Когда появятся записи — voiceId станет
            // путём события (напр. "event:/Voice/intro_01") и заиграет сам.
            if (!string.IsNullOrEmpty(voiceId))
                Debug.Log($"[Novel] Озвучка (зарезервировано): {voiceId}", this);
        }

        private void PlayOneShotSafe(string kind, string eventPath)
        {
            if (string.IsNullOrEmpty(eventPath)) return;

            var rm = ResolveRuntimeManager();
            if (rm == null) return;

            try
            {
                var play = rm.GetMethod("PlayOneShot", new[] { typeof(string), typeof(Vector3) });
                play?.Invoke(null, new object[] { eventPath, transform.position });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Novel] FMOD: {kind} '{eventPath}' не проигран: {e.Message}", this);
            }
        }

        private static Type ResolveRuntimeManager()
        {
            if (runtimeManagerResolved) return runtimeManagerType;

            runtimeManagerResolved = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                runtimeManagerType = assembly.GetType("FMODUnity.RuntimeManager");
                if (runtimeManagerType != null) break;
            }

            if (runtimeManagerType == null && !missingTypeWarned)
            {
                missingTypeWarned = true;
                Debug.LogWarning("[Novel] FMODUnity.RuntimeManager не найден — FMOD не настроен, аудио-кью только в логе.");
            }

            return runtimeManagerType;
        }

        private void OnDestroy() => StopMusic();
    }
}
