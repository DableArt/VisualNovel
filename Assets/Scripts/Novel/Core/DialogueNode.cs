using System;
using System.Collections.Generic;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Узел диалога: реплика/описание + медиа-кью + варианты ответа.
    /// Анимаций и сюжета пока нет — поля уже заложены и заполняются из JSON.
    /// </summary>
    [Serializable]
    public sealed class DialogueNode
    {
        /// <summary>Уникальный id узла (на него ссылаются nextNodeId).</summary>
        public string id = string.Empty;

        /// <summary>Имя говорящего (для будущих портретов/подсветки).</summary>
        public string speaker = string.Empty;

        /// <summary>Текст реплики или описания сцены.</summary>
        public string text = string.Empty;

        /// <summary>
        /// Анимированная сценка, проигрываемая при входе в узел.
        /// По окончании персонажи возвращаются в Idle и ждут выбора игрока.
        /// Пусто — сценки нет.
        /// </summary>
        public string sceneAnimationId = string.Empty;

        /// <summary>Музыка (путь события FMOD, напр. "event:/Music/Theme"). Пусто — не менять текущую.</summary>
        public string musicId = string.Empty;

        /// <summary>Разовый звуковой эффект при входе в узел. Пусто — без звука.</summary>
        public string sfxId = string.Empty;

        /// <summary>Озвучка реплики — МЕСТО ЗАРЕЗЕРВИРОВАНО, записи добавим позже.</summary>
        public string voiceId = string.Empty;

        /// <summary>
        /// Куда ведёт «бездействие» (таймаут) для этого узла.
        /// Пусто — берётся узел по умолчанию из <see cref="NovelCoreConfig"/>.
        /// </summary>
        public string timeoutNextNodeId = string.Empty;

        /// <summary>Варианты ответа. Пусто — узел завершает сцену.</summary>
        public List<ChoiceOption> choices = new();
    }
}
