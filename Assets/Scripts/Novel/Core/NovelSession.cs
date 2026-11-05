using System;
using System.Collections.Generic;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Ядро визуальной новеллы. Чистый C#-класс без зависимостей на визуал:
    /// ведёт граф диалога, показывает варианты ответа (не больше
    /// <see cref="NovelCoreConfig.HardMaxChoices"/>), считает таймер выбора
    /// и по таймауту разыгрывает «бездействие». Обо всём сообщает событиями.
    /// Анимированные сценки и аудио — снаружи, через события и IAudioService.
    /// </summary>
    public sealed class NovelSession
    {
        public enum State { Idle, AwaitingChoice, Finished }

        private readonly NovelCoreConfig config;
        private readonly DialogueGraph graph;
        private readonly IAudioService audio;

        private List<ChoiceOption> presented = new();

        public State CurrentState { get; private set; } = State.Idle;

        /// <summary>Текущий узел диалога (null до старта).</summary>
        public DialogueNode CurrentNode { get; private set; }

        /// <summary>Варианты, реально показанные игроку (с учётом maxChoices).</summary>
        public IReadOnlyList<ChoiceOption> PresentedChoices => presented;

        /// <summary>Остаток времени на выбор (секунды). Актуально в состоянии AwaitingChoice.</summary>
        public float RemainingChoiceTime { get; private set; }

        /// <summary>Вошли в узел: можно играть сценку, менять фон, показывать текст.</summary>
        public event Action<DialogueNode> NodeEntered;

        /// <summary>Варианты показаны игроку. Второй параметр — лимит времени (0 = без таймера).</summary>
        public event Action<IReadOnlyList<ChoiceOption>, float> ChoicesPresented;

        /// <summary>Выбор разрешён. byTimeout = true, если это «бездействие» по таймауту.</summary>
        public event Action<ChoiceOption, bool> ChoiceResolved;

        /// <summary>Сцена завершена (узел без выборов, бездействие без цели или конец графа).</summary>
        public event Action SessionFinished;

        /// <summary>Нефатальная ошибка данных (например, узел не найден).</summary>
        public event Action<string> Error;

        public NovelSession(NovelCoreConfig config, IDialogueProvider provider, IAudioService audio = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            graph = provider.Load() ?? throw new InvalidOperationException("[Novel] Провайдер вернул пустой граф.");
            this.audio = audio ?? NullAudioService.Instance;
        }

        /// <summary>Старт с узла startNodeId из данных.</summary>
        public void Start() => Start(graph.startNodeId);

        /// <summary>Старт с конкретного узла.</summary>
        public void Start(string startNodeId)
        {
            if (CurrentState == State.AwaitingChoice) return;

            if (string.IsNullOrEmpty(startNodeId))
            {
                Error?.Invoke("[Novel] Не задан стартовый узел.");
                Finish();
                return;
            }

            EnterNode(startNodeId);
        }

        /// <summary>Вызывается каждый кадр из MonoBehaviour-обёртки (таймер выбора).</summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState != State.AwaitingChoice || config.choiceTimeLimit <= 0f) return;

            RemainingChoiceTime -= deltaTime;
            if (RemainingChoiceTime <= 0f)
            {
                RemainingChoiceTime = 0f;
                ResolveTimeout();
            }
        }

        /// <summary>Игрок выбрал вариант по индексу из <see cref="PresentedChoices"/>.</summary>
        public bool SelectChoice(int index)
        {
            if (CurrentState != State.AwaitingChoice) return false;
            if (index < 0 || index >= presented.Count) return false;

            var option = presented[index];
            CurrentState = State.Idle; // останавливаем таймер на время перехода
            ChoiceResolved?.Invoke(option, false);
            Navigate(option.nextNodeId);
            return true;
        }

        /// <summary>Таймаут: разыгрывается дополнительный вариант «бездействие».</summary>
        private void ResolveTimeout()
        {
            var node = CurrentNode;
            string target = !string.IsNullOrEmpty(node?.timeoutNextNodeId)
                ? node.timeoutNextNodeId
                : config.idleNextNodeId;

            var idle = new ChoiceOption(config.idleChoiceText, target);
            CurrentState = State.Idle;
            ChoiceResolved?.Invoke(idle, true);
            Navigate(target);
        }

        private void Navigate(string nextNodeId)
        {
            if (string.IsNullOrEmpty(nextNodeId))
            {
                Finish();
                return;
            }

            EnterNode(nextNodeId);
        }

        private void EnterNode(string nodeId)
        {
            if (!graph.TryGetNode(nodeId, out var node))
            {
                Error?.Invoke($"[Novel] Узел '{nodeId}' не найден — сцена завершена.");
                Finish();
                return;
            }

            CurrentNode = node;
            PlayAudioCues(node);
            NodeEntered?.Invoke(node);

            if (node.choices == null || node.choices.Count == 0)
            {
                Finish();
                return;
            }

            int count = Math.Min(config.maxChoices, node.choices.Count);
            presented = node.choices.GetRange(0, count);
            RemainingChoiceTime = config.choiceTimeLimit;
            CurrentState = State.AwaitingChoice;
            ChoicesPresented?.Invoke(presented, config.choiceTimeLimit);
        }

        private void PlayAudioCues(DialogueNode node)
        {
            if (!string.IsNullOrEmpty(node.musicId)) audio.PlayMusic(node.musicId);
            if (!string.IsNullOrEmpty(node.sfxId)) audio.PlaySfx(node.sfxId);
            if (!string.IsNullOrEmpty(node.voiceId)) audio.PlayVoice(node.voiceId); // зарезервировано
        }

        private void Finish()
        {
            if (CurrentState == State.Finished) return;

            CurrentState = State.Finished;
            presented = new List<ChoiceOption>();
            SessionFinished?.Invoke();
        }
    }
}
