using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualNovel.Training
{
    /// <summary>
    /// MonoBehaviour-обёртка над ядром <see cref="TrainingSession"/>:
    /// двигает время, читает ввод (новая Input System), спавнит стрелки
    /// и транслирует события ядра в визуал и характеристики героя.
    /// </summary>
    public sealed class TrainingSessionRunner : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private TrainingSessionConfig config;
        [SerializeField] private HeroStats heroStats;

        [Header("Сцена")]
        [SerializeField] private TrainingNoteView notePrefab;
        [SerializeField] private RectTransform notesContainer;
        [SerializeField] private RectTransform spawnPoint;
        [SerializeField] private RectTransform hitZone;
        [SerializeField] private TrainingFeedbackView feedback;

        private static readonly Key[] TrackedKeys =
        {
            Key.LeftArrow, Key.DownArrow, Key.UpArrow, Key.RightArrow
        };

        private TrainingSession session;
        private readonly List<TrainingNoteView> activeNotes = new();

        private void Start()
        {
            StartTraining(config);
        }

        /// <summary>Запуск тренировки извне (например, из диалога визуальной новеллы).</summary>
        public void StartTraining(TrainingSessionConfig sessionConfig)
        {
            config = sessionConfig;
            session = new TrainingSession(config, new RandomTrainingSequenceGenerator());
            session.NoteBecameVisible += OnNoteVisible;
            session.NoteHit += OnNoteHit;
            session.NoteMissed += OnNoteMissed;
            session.SessionFinished += OnSessionFinished;
            session.Start();
        }

        private void Update()
        {
            if (session == null) return;

            session.Tick(Time.deltaTime);
            PollInput();
            UpdateNoteViews();
        }

        private void PollInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            foreach (var key in TrackedKeys)
            {
                if (keyboard[key].wasPressedThisFrame)
                    session.HandleKeyPress(key);
            }
        }

        private void UpdateNoteViews()
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                if (activeNotes[i] == null ||
                    !activeNotes[i].UpdatePosition(session.Elapsed))
                {
                    if (activeNotes[i] != null)
                        Destroy(activeNotes[i].gameObject);
                    activeNotes.RemoveAt(i);
                }
            }
        }

        private void OnNoteVisible(int index, TrainingNote note)
        {
            var view = Instantiate(notePrefab, notesContainer);
            view.Init(note, spawnPoint.anchoredPosition, hitZone.anchoredPosition,
                config.noteTravelTime, session.Elapsed);
            activeNotes.Add(view);
        }

        private void OnNoteHit(int index, TrainingNote note, TrainingHitResult result, int points)
        {
            heroStats?.Add(config.stat, points);
            feedback?.ShowHit(PickRandom(config.successPhrases));
        }

        private void OnNoteMissed(int index, TrainingNote note)
        {
            feedback?.ShowMiss(PickRandom(config.missPhrases));
        }

        private void OnSessionFinished(bool completed)
        {
            feedback?.ShowPhrase(completed
                ? $"Тренировка завершена! +{session.EarnedStatPoints} к {StatName(config.stat)}"
                : "Тренировка провалена. Попробуй ещё раз!");
        }

        private static string PickRandom(string[] phrases) =>
            phrases != null && phrases.Length > 0
                ? phrases[Random.Range(0, phrases.Length)]
                : string.Empty;

        private static string StatName(TrainingStat stat) => stat switch
        {
            TrainingStat.Strength => "Силе",
            TrainingStat.Agility => "Ловкости",
            TrainingStat.Intelligence => "Интеллекту",
            _ => stat.ToString()
        };

        private void OnDestroy()
        {
            if (session == null) return;
            session.NoteBecameVisible -= OnNoteVisible;
            session.NoteHit -= OnNoteHit;
            session.NoteMissed -= OnNoteMissed;
            session.SessionFinished -= OnSessionFinished;
        }
    }
}
