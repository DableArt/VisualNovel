using UnityEngine;

namespace VisualNovel.Training
{
    /// <summary>
    /// Настройки одного вида тренировки. Создаётся через
    /// Assets → Create → VisualNovel → Training → Session Config.
    /// Разные виды тренировок = разные экземпляры этого ассета.
    /// </summary>
    [CreateAssetMenu(menuName = "VisualNovel/Training/Session Config", fileName = "TrainingSessionConfig")]
    public sealed class TrainingSessionConfig : ScriptableObject
    {
        [Header("Прогрессия")]
        [Tooltip("Какая характеристика качается в этой тренировке")]
        public TrainingStat stat = TrainingStat.Strength;

        [Tooltip("Очков характеристики за идеальное попадание (Good даёт половину)")]
        public int statPerPerfectHit = 2;

        [Header("Правила")]
        [Tooltip("Сколько промахов завершает тренировку (по ТЗ — 3)")]
        [Min(1)] public int maxMisses = 3;

        [Tooltip("Длина последовательности нот в одной тренировке")]
        [Min(1)] public int sequenceLength = 12;

        [Header("Тайминги (секунды)")]
        [Tooltip("Окно идеального попадания (± от момента удара)")]
        public float perfectWindow = 0.12f;

        [Tooltip("Окно хорошего попадания (± от момента удара)")]
        public float goodWindow = 0.3f;

        [Tooltip("За сколько секунд нота появляется на экране до момента удара")]
        public float noteTravelTime = 1.5f;

        [Tooltip("Пауза между нотами")]
        public float noteInterval = 0.9f;

        [Header("Фразы при успехе")]
        [TextArea] public string[] successPhrases =
        {
            "Так держать!",
            "Успех!",
            "Отлично!",
            "Ещё раз!"
        };

        [Header("Фразы при промахе")]
        [TextArea] public string[] missPhrases =
        {
            "Мимо!",
            "Соберись!",
            "Не сдавайся!"
        };
    }
}
