using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace VisualNovel.Training
{
    /// <summary>
    /// Ядро механики тренировки. Чистый C#-класс без Unity-зависимостей на визуал:
    /// считает тайминги, попадания, промахи и награды, сообщая обо всём через события.
    /// Одинаков для всех видов тренировок — меняется только конфиг и визуальная обвязка.
    /// </summary>
    public sealed class TrainingSession
    {
        public enum State { Idle, Running, Finished }

        private readonly TrainingSessionConfig config;
        private readonly IReadOnlyList<TrainingNote> notes;

        private float elapsed;
        private int currentIndex;

        public State CurrentState { get; private set; } = State.Idle;
        public TrainingSessionConfig Config => config;
        public IReadOnlyList<TrainingNote> Notes => notes;

        /// <summary>Индекс активной ноты (-1, если сессия не идёт).</summary>
        public int CurrentIndex => currentIndex;

        /// <summary>Сколько промахов допущено.</summary>
        public int MissCount { get; private set; }

        /// <summary>Итоговая награда в очках характеристики.</summary>
        public int EarnedStatPoints { get; private set; }

        /// <summary>Текущее время сессии (секунды).</summary>
        public float Elapsed => elapsed;

        /// <summary>Нота появилась на экране (пора показывать стрелку).</summary>
        public event Action<int, TrainingNote> NoteBecameVisible;

        /// <summary>Игрок попал по ноте.</summary>
        public event Action<int, TrainingNote, TrainingHitResult, int> NoteHit;

        /// <summary>Промах по ноте (таймаут или неверная клавиша).</summary>
        public event Action<int, TrainingNote> NoteMissed;

        /// <summary>Тренировка завершена. Параметр — true, если лимит промахов НЕ исчерпан.</summary>
        public event Action<bool> SessionFinished;

        public TrainingSession(TrainingSessionConfig config, ITrainingSequenceGenerator generator)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            notes = generator.Generate(config);
        }

        public void Start()
        {
            if (CurrentState != State.Idle) return;
            CurrentState = State.Running;
            elapsed = 0f;
        }

        /// <summary>Вызывается каждый кадр из MonoBehaviour-обёртки.</summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState != State.Running) return;

            elapsed += deltaTime;
            RevealVisibleNotes();
            FailOverdueNote();
            FinishWhenOutOfNotes();
        }

        /// <summary>Обработка нажатия клавиши. Возвращает true, если нажатие разыграно.</summary>
        public bool HandleKeyPress(Key key)
        {
            if (CurrentState != State.Running) return false;

            var note = CurrentNote;
            if (note == null) return false;

            if (key == note.Key && Mathf_Abs(elapsed - note.HitTime) <= config.goodWindow)
            {
                ResolveHit(note);
                return true;
            }

            // Любое неверное нажатие, пока активна нота, — промах.
            ResolveMiss(note);
            return true;
        }

        private TrainingNote CurrentNote
        {
            get
            {
                while (currentIndex < notes.Count && notes[currentIndex].IsResolved)
                    currentIndex++;

                return currentIndex < notes.Count ? notes[currentIndex] : null;
            }
        }

        private void ResolveHit(TrainingNote note)
        {
            bool perfect = Mathf_Abs(elapsed - note.HitTime) <= config.perfectWindow;
            var result = perfect ? TrainingHitResult.Perfect : TrainingHitResult.Good;

            int points = perfect
                ? config.statPerPerfectHit
                : Math.Max(1, config.statPerPerfectHit / 2);

            EarnedStatPoints += points;
            note.Resolve();
            NoteHit?.Invoke(currentIndex, note, result, points);
        }

        private void ResolveMiss(TrainingNote note)
        {
            note.Resolve();
            MissCount++;
            NoteMissed?.Invoke(currentIndex, note);

            if (MissCount >= config.maxMisses)
                Finish(completed: false);
        }

        private void RevealVisibleNotes()
        {
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (note.IsResolved || note.IsVisible) continue;
                if (elapsed >= note.HitTime - config.noteTravelTime)
                {
                    note.MarkVisible();
                    NoteBecameVisible?.Invoke(i, note);
                }
            }
        }

        private void FailOverdueNote()
        {
            var note = CurrentNote;
            if (note != null && elapsed > note.HitTime + config.goodWindow)
                ResolveMiss(note);
        }

        private void FinishWhenOutOfNotes()
        {
            if (CurrentState == State.Running && CurrentNote == null)
                Finish(completed: true);
        }

        private void Finish(bool completed)
        {
            CurrentState = State.Finished;
            SessionFinished?.Invoke(completed);
        }

        // Избегаем зависимости от UnityEngine в арифметике — ядро тестируемо вне Unity.
        private static float Mathf_Abs(float v) => v < 0f ? -v : v;
    }
}
