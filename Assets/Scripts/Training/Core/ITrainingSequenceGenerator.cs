using System.Collections.Generic;

namespace VisualNovel.Training
{
    /// <summary>
    /// Стратегия генерации последовательности нот.
    /// Позволяет заменять «случайный ритм» на фиксированные паттерны
    /// или паттерны под музыку, не трогая ядро сессии.
    /// </summary>
    public interface ITrainingSequenceGenerator
    {
        IReadOnlyList<TrainingNote> Generate(TrainingSessionConfig config);
    }
}
