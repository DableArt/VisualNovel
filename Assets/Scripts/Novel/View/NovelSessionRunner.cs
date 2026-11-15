using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// MonoBehaviour-обёртка над ядром <see cref="NovelSession"/>: сборка сессии
    /// из JSON + конфига, Tick таймера, трансляция событий ядра в UI, актёров и аудио.
    ///
    /// Секвенс появления диалога (основа Core Loop):
    /// 1) текстовое окно с именем персонажа — FadeInDown (сверху вниз);
    /// 2) «колесо» вариантов — FadeInUp (снизу вверх), вместе с ним показывается эллипс;
    /// 3) только после появления стартует обратный отсчёт (<see cref="NovelSession.BeginChoiceTimer"/>).
    ///
    /// Таймаут: сообщение в консоль + выход из игры (временное решение, quitOnTimeout).
    /// </summary>
    public sealed class NovelSessionRunner : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private NovelCoreConfig config;
        [SerializeField] private TextAsset dialogueJson;
        [Tooltip("Пусто — стартовый узел берётся из JSON (startNodeId)")]
        [SerializeField] private string startNodeId = string.Empty;

        [Header("Диалоговый UI (сцена DialogSystem)")]
        [SerializeField] private DialogueWindowView dialogueWindow;
        [SerializeField] private ChoiceWheelView choiceWheel;
        [SerializeField] private ChoiceTimerView choiceTimer;
        [SerializeField] private DialogueInputController input;

        [Header("Простая панель выбора (опционально)")]
        [SerializeField] private ChoicePanelView choicePanel;

        [Header("Сцена")]
        [Tooltip("Актёры, которым проигрываются анимированные сценки (после — возврат в Idle)")]
        [SerializeField] private CharacterActorView[] actors = new CharacterActorView[0];
        [Tooltip("Компонент, реализующий IAudioService (например FmodAudioService). Пусто — тишина")]
        [SerializeField] private MonoBehaviour audioService;

        [Header("Таймаут (временное решение)")]
        [Tooltip("По истечении времени выбора: сообщение в консоль и выход из игры")]
        [SerializeField] private bool quitOnTimeout = true;

        private readonly ReactiveProperty<float> remainingChoiceTime = new(0f);
        private Coroutine presentSequence;

        /// <summary>Остаток времени на выбор — подписка для UI (эллипс-таймер).</summary>
        public ReadOnlyReactiveProperty<float> RemainingChoiceTime => remainingChoiceTime;

        /// <summary>Ядро сессии (для внешних систем: сохранения, отладка).</summary>
        public NovelSession Session { get; private set; }

        private void Awake()
        {
            if (config == null || dialogueJson == null)
            {
                Debug.LogError("[Novel] Заполни Config и Dialogue Json.", this);
                enabled = false;
                return;
            }

            var provider = new JsonDialogueProvider(dialogueJson.text);
            var audio = audioService as IAudioService ?? NullAudioService.Instance;

            Session = new NovelSession(config, provider, audio);
            Session.Error += msg => Debug.LogWarning(msg, this);
            Session.NodeEntered += OnNodeEntered;
            Session.ChoicesPresented += OnChoicesPresented;
            Session.ChoiceResolved += OnChoiceResolved;
            Session.SessionFinished += OnSessionFinished;

            if (choicePanel != null) choicePanel.Bind(Session, RemainingChoiceTime);
            if (choiceWheel != null) choiceWheel.Bind(Session);
            if (choiceTimer != null) choiceTimer.Bind(RemainingChoiceTime);
            if (input != null)
            {
                input.DirectionPressed += OnDirectionPressed;
                input.Enabled = false;
            }

            if (dialogueWindow != null) dialogueWindow.HideInstant();
            if (choiceTimer != null) choiceTimer.Hide();
        }

        private void Start()
        {
            if (Session == null) return;

            if (string.IsNullOrEmpty(startNodeId)) Session.Start();
            else Session.Start(startNodeId);
        }

        private void Update()
        {
            if (Session == null) return;

            Session.Tick(Time.deltaTime);
            remainingChoiceTime.Value = Session.RemainingChoiceTime;
        }

        private void OnNodeEntered(DialogueNode node)
        {
            if (string.IsNullOrEmpty(node.sceneAnimationId)) return;

            foreach (var actor in actors)
                if (actor != null) actor.PlayScene(node.sceneAnimationId);
        }

        private void OnChoicesPresented(IReadOnlyList<ChoiceOption> options, float timeLimit)
        {
            if (choiceWheel != null) choiceWheel.Prepare(options);

            RestartPresentSequence(PresentSequence(timeLimit));
        }

        private void OnChoiceResolved(ChoiceOption option, bool byTimeout)
        {
            StopPresentSequence();
            if (choiceWheel != null) choiceWheel.Hide();
            if (choiceTimer != null) choiceTimer.Hide();
            if (input != null) input.Enabled = false;

            if (!byTimeout) return;

            Debug.Log("[Novel] Время закончилось — выбор не сделан.", this);
            if (quitOnTimeout) QuitGame();
        }

        private void OnSessionFinished()
        {
            if (choiceWheel != null) choiceWheel.Hide();
            if (choiceTimer != null) choiceTimer.Hide();
            if (input != null) input.Enabled = false;

            // Узел без вариантов: финальная реплика всё равно показывается.
            var node = Session.CurrentNode;
            if (node != null && presentSequence == null && dialogueWindow != null)
            {
                dialogueWindow.SetLine(node.speaker, node.text);
                presentSequence = StartCoroutine(FinalLineSequence());
            }
        }

        private void OnDirectionPressed(ChoiceDirection direction)
        {
            if (choiceWheel != null) choiceWheel.SelectFromInput(direction);
        }

        /// <summary>
        /// Секвенс появления: окно (FadeInDown) → колесо (FadeInUp) → старт отсчёта.
        /// </summary>
        private IEnumerator PresentSequence(float timeLimit)
        {
            var node = Session.CurrentNode;

            if (dialogueWindow != null)
            {
                dialogueWindow.SetLine(node.speaker, node.text);
                yield return dialogueWindow.PlayFadeIn();
            }

            if (choiceTimer != null) choiceTimer.Prepare(timeLimit);
            if (choiceWheel != null) yield return choiceWheel.PlayFadeIn();

            if (input != null) input.Enabled = true;
            Session.BeginChoiceTimer();
            presentSequence = null;
        }

        private IEnumerator FinalLineSequence()
        {
            yield return dialogueWindow.PlayFadeIn();
            presentSequence = null;
        }

        private void RestartPresentSequence(IEnumerator sequence)
        {
            StopPresentSequence();
            presentSequence = StartCoroutine(sequence);
        }

        private void StopPresentSequence()
        {
            if (presentSequence == null) return;
            StopCoroutine(presentSequence);
            presentSequence = null;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            remainingChoiceTime.Dispose();
        }
    }
}
