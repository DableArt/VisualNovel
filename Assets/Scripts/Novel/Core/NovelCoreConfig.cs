using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Настройки ядра визуальной новеллы. Создаётся через
    /// Assets → Create → VisualNovel → Novel → Core Config.
    /// </summary>
    [CreateAssetMenu(menuName = "VisualNovel/Novel/Core Config", fileName = "NovelCoreConfig")]
    public sealed class NovelCoreConfig : ScriptableObject
    {
        /// <summary>Жёсткий потолок вариантов ответа по ТЗ.</summary>
        public const int HardMaxChoices = 6;

        [Header("Выборы")]
        [Tooltip("Сколько вариантов ответа максимум показывается игроку (потолок — 6)")]
        [Range(1, HardMaxChoices)] public int maxChoices = HardMaxChoices;

        [Header("Таймер выбора")]
        [Tooltip("Секунд на выбор ответа. 0 — таймер выключен")]
        [Min(0f)] public float choiceTimeLimit = 10f;

        [Tooltip("Текст варианта «бездействие», который ядро выбирает по таймауту")]
        public string idleChoiceText = "(Промолчать)";

        [Tooltip("Узел по умолчанию для бездействия, если у текущего узла не задан timeoutNextNodeId. " +
                 "Пусто — по таймауту сцена завершается")]
        public string idleNextNodeId = string.Empty;
    }
}
