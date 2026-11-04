using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualNovel.Training
{
    /// <summary>
    /// Стандартная стратегия: случайные стрелки с равным интервалом.
    /// </summary>
    public sealed class RandomTrainingSequenceGenerator : ITrainingSequenceGenerator
    {
        private static readonly Key[] ArrowKeys =
        {
            Key.LeftArrow,
            Key.DownArrow,
            Key.UpArrow,
            Key.RightArrow
        };

        public IReadOnlyList<TrainingNote> Generate(TrainingSessionConfig config)
        {
            var notes = new List<TrainingNote>(config.sequenceLength);
            // Первая нота появляется после короткой подготовки игрока.
            float hitTime = config.noteTravelTime + 0.5f;

            for (int i = 0; i < config.sequenceLength; i++)
            {
                var key = ArrowKeys[Random.Range(0, ArrowKeys.Length)];
                notes.Add(new TrainingNote(key, hitTime));
                hitTime += config.noteInterval;
            }

            return notes;
        }
    }
}
