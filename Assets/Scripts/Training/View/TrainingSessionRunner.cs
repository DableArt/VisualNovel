using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        private readonly Dictionary<int, TrainingNoteView> activeNotes = new();
        private readonly List<int> expiredNoteIndices = new();

        private void Start()
        {
            StartTraining(config);
        }

        /// <summary>Запуск тренировки извне (например, из диалога визуальной новеллы).</summary>
        public void StartTraining(TrainingSessionConfig sessionConfig)
        {
            config = sessionConfig;

            if (config == null || notePrefab == null || notesContainer == null ||
                spawnPoint == null || hitZone == null)
            {
                Debug.LogError("[Training] Заполни Config, Note Prefab, Notes Container, Spawn Point и Hit Zone.", this);
                enabled = false;
                return;
            }

            if (notePrefab.GetComponent<Image>() == null || notePrefab.GetComponent<Canvas>() != null)
            {
                Debug.LogError("[Training] Note Prefab должен быть отдельным объектом UI → Image " +
                    "с TrainingNoteView, а не Canvas или целой сценой. Создай новый prefab для одной стрелки.", notePrefab);
                enabled = false;
                return;
            }

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
            expiredNoteIndices.Clear();
            foreach (var pair in activeNotes)
            {
                if (pair.Value == null || !pair.Value.UpdatePosition(session.Elapsed))
                    expiredNoteIndices.Add(pair.Key);
            }

            foreach (int index in expiredNoteIndices)
                RemoveNoteView(index);
        }

        private void OnNoteVisible(int index, TrainingNote note)
        {
            RemoveNoteView(index);
            var view = Instantiate(notePrefab, notesContainer);
            view.Init(note, LocalPoint(spawnPoint), LocalPoint(hitZone),
                config.noteTravelTime, session.Elapsed);
            activeNotes.Add(index, view);
        }

        // Переводит позицию точки в координаты контейнера нот —
        // работает корректно при любых якорях у SpawnPoint/HitZone.
        private Vector2 LocalPoint(RectTransform point) =>
            notesContainer.InverseTransformPoint(point.position);

        private void OnNoteHit(int index, TrainingNote note, TrainingHitResult result, int points)
        {
            RemoveNoteView(index);
            heroStats?.Add(config.stat, points);
            feedback?.ShowHit(PickRandom(config.successPhrases));
        }

        private void OnNoteMissed(int index, TrainingNote note)
        {
            RemoveNoteView(index);
            feedback?.ShowMiss(PickRandom(config.missPhrases));
        }

        private void OnSessionFinished(bool completed)
        {
            ClearNoteViews();
            feedback?.ShowPhrase(completed
                ? $"Тренировка завершена! +{session.EarnedStatPoints} к {StatName(config.stat)}"
                : "Тренировка провалена. Попробуй ещё раз!");
        }

        private void RemoveNoteView(int index)
        {
            if (!activeNotes.TryGetValue(index, out var view))
                return;

            activeNotes.Remove(index);
            if (view != null)
                Destroy(view.gameObject);
        }

        private void ClearNoteViews()
        {
            foreach (var view in activeNotes.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            activeNotes.Clear();
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
