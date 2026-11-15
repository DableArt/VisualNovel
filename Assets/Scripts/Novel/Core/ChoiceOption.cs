using System;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Один вариант ответа в узле диалога.
    /// Сериализуется из JSON и редактируется в инспекторе.
    /// </summary>
    [Serializable]
    public sealed class ChoiceOption
    {
        /// <summary>Текст варианта на кнопке.</summary>
        public string text = string.Empty;

        /// <summary>Id узла, в который ведёт выбор. Пусто — сцена завершается.</summary>
        public string nextNodeId = string.Empty;

        /// <summary>Описание бонуса/последствия ("+10 к мастерству"). Показывается при наведении. Пусто — без описания.</summary>
        public string bonusText = string.Empty;

        /// <summary>Шанс срабатывания варианта (0..1). Отрицательное значение — шанс не показывается.</summary>
        public float successChance = -1f;

        public ChoiceOption() { }

        public ChoiceOption(string text, string nextNodeId)
        {
            this.text = text;
            this.nextNodeId = nextNodeId;
        }
    }
}
