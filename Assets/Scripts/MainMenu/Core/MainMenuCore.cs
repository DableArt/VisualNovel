using System;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Ядро главного меню. Чистый C#-класс без зависимостей на визуал:
    /// ведёт состояние меню (Intro → Idle → Leaving), решает, какие команды
    /// доступны (например «Продолжить»/«Загрузить» — только при наличии
    /// сохранения), и рассылает команды событием. Команды подменю
    /// (настройки, доп. материалы) не переводят меню в Leaving.
    /// Фоновые анимации и анимация появления кнопок — снаружи: они подписываются
    /// на StateChanged и по окончании интро вызывают <see cref="CompleteIntro"/>.
    /// Переходы между экранами (главное ↔ настройки) тоже идут через внешнюю
    /// анимацию: ядро шлёт TransitionStarted и ждёт <see cref="CompleteTransition"/>,
    /// пока переход идёт — ввод заблокирован.
    /// </summary>
    public sealed class MainMenuCore
    {
        private bool hasSaveGame;
        private MainMenuScreen pendingScreen;

        /// <summary>Текущее состояние меню. Стартовое — Intro (ввод заблокирован до конца интро-анимации).</summary>
        public MainMenuState CurrentState { get; private set; } = MainMenuState.Intro;

        /// <summary>Активный экран меню.</summary>
        public MainMenuScreen CurrentScreen { get; private set; } = MainMenuScreen.Main;

        /// <summary>Идёт ли анимация перехода между экранами (ввод заблокирован).</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>Есть ли сохранение. Пока выставляется заглушкой, потом — системой сохранений.</summary>
        public bool HasSaveGame
        {
            get => hasSaveGame;
            set
            {
                if (hasSaveGame == value) return;

                hasSaveGame = value;
                SaveAvailabilityChanged?.Invoke(value);
            }
        }

        /// <summary>Состояние меню сменилось (для фоновых анимаций и анимации кнопок).</summary>
        public event Action<MainMenuState> StateChanged;

        /// <summary>Игрок подтвердил команду. Обработчики (загрузка сцен, выход) — снаружи ядра.</summary>
        public event Action<MainMenuCommand> CommandInvoked;

        /// <summary>Доступность «Продолжить»/«Загрузить» изменилась — обновить интерактивность кнопок.</summary>
        public event Action<bool> SaveAvailabilityChanged;

        /// <summary>Начался переход между экранами (откуда, куда) — вью запускает анимацию.</summary>
        public event Action<MainMenuScreen, MainMenuScreen> TransitionStarted;

        /// <summary>Переход закончился, экран активен — вью вызвало <see cref="CompleteTransition"/>.</summary>
        public event Action<MainMenuScreen> TransitionFinished;

        /// <summary>Можно ли выполнить команду прямо сейчас.</summary>
        public bool CanExecute(MainMenuCommand command)
        {
            if (CurrentState != MainMenuState.Idle || IsTransitioning) return false;

            if (command is MainMenuCommand.Continue or MainMenuCommand.LoadGame)
                return hasSaveGame;

            return true;
        }

        /// <summary>Интро-анимация (фон, появление кнопок) закончена — меню становится интерактивным.</summary>
        public void CompleteIntro()
        {
            if (CurrentState != MainMenuState.Intro) return;

            SetState(MainMenuState.Idle);
        }

        /// <summary>Открыть экран настроек (через анимацию перехода).</summary>
        public bool OpenSettings() => BeginTransition(MainMenuScreen.Settings);

        /// <summary>Вернуться с подэкрана в главное меню (через анимацию перехода).</summary>
        public bool BackToMain() => BeginTransition(MainMenuScreen.Main);

        /// <summary>
        /// Вью сообщает, что анимация перехода закончилась. Пока анимации нет —
        /// вызывается сразу после TransitionStarted.
        /// </summary>
        public void CompleteTransition()
        {
            if (!IsTransitioning) return;

            IsTransitioning = false;
            CurrentScreen = pendingScreen;
            TransitionFinished?.Invoke(CurrentScreen);
        }

        private bool BeginTransition(MainMenuScreen target)
        {
            if (CurrentState != MainMenuState.Idle || IsTransitioning) return false;
            if (target == CurrentScreen) return false;

            IsTransitioning = true;
            pendingScreen = target;
            TransitionStarted?.Invoke(CurrentScreen, target);
            return true;
        }

        /// <summary>
        /// Игрок нажал кнопку меню. Возвращает true, если команда принята.
        /// Команды перехода (продолжить/новая игра/загрузить/выйти) переводят меню в Leaving.
        /// </summary>
        public bool InvokeCommand(MainMenuCommand command)
        {
            if (!CanExecute(command)) return false;

            CommandInvoked?.Invoke(command);

            if (command is MainMenuCommand.Continue or MainMenuCommand.NewGame
                or MainMenuCommand.LoadGame or MainMenuCommand.Exit)
                SetState(MainMenuState.Leaving);

            return true;
        }

        /// <summary>Возврат в меню (повторный вход в сцену): интро проигрывается заново.</summary>
        public void ReturnToMenu()
        {
            if (CurrentState == MainMenuState.Intro) return;

            SetState(MainMenuState.Intro);
        }

        private void SetState(MainMenuState next)
        {
            if (CurrentState == next) return;

            CurrentState = next;
            StateChanged?.Invoke(next);
        }
    }
}
