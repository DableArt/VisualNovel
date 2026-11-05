using R3;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// MonoBehaviour-обёртка над ядром <see cref="NovelSession"/>:
    /// двигает таймер (Tick), собирает сессию из JSON + конфига,
    /// транслирует события ядра в UI, актёров и аудио.
    /// Остаток времени выставлен как R3 ReactiveProperty — UI подписывается реактивно.
    /// </summary>
    public sealed class NovelSessionRunner : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private NovelCoreConfig config;
        [SerializeField] private TextAsset dialogueJson;
        [Tooltip("Пусто — стартовый узел берётся из JSON (startNodeId)")]
        [SerializeField] private string startNodeId = string.Empty;

        [Header("Сцена")]
        [SerializeField] private ChoicePanelView choicePanel;
        [Tooltip("Актёры, которым проигрываются анимированные сценки (после — возврат в Idle)")]
        [SerializeField] private CharacterActorView[] actors = new CharacterActorView[0];
        [Tooltip("Компонент, реализующий IAudioService (например FmodAudioService). Пусто — тишина")]
        [SerializeField] private MonoBehaviour audioService;

        private readonly ReactiveProperty<float> remainingChoiceTime = new(0f);

        /// <summary>Остаток времени на выбор — подписка для UI (таймер-бар).</summary>
        public ReadOnlyReactiveProperty<float> RemainingChoiceTime => remainingChoiceTime;

        /// <summary>Ядро сессии (для внешних систем: сохранения, отладка).</summary>
        public NovelSession Session { get; private set; }

        private void Start()
        {
            if (config == null || dialogueJson == null || choicePanel == null)
            {
                Debug.LogError("[Novel] Заполни Config, Dialogue Json и Choice Panel.", this);
                enabled = false;
                return;
            }

            var provider = new JsonDialogueProvider(dialogueJson.text);
            var audio = audioService as IAudioService ?? NullAudioService.Instance;

            Session = new NovelSession(config, provider, audio);
            Session.Error += msg => Debug.LogWarning(msg, this);
            Session.NodeEntered += OnNodeEntered;

            choicePanel.Bind(Session, RemainingChoiceTime);

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

        private void OnDestroy()
        {
            remainingChoiceTime.Dispose();
        }
    }
}
