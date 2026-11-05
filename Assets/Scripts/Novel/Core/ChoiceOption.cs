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

        public ChoiceOption() { }

        public ChoiceOption(string text, string nextNodeId)
        {
            this.text = text;
            this.nextNodeId = nextNodeId;
        }
    }
}
