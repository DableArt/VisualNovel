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
    /// </summary>
    public sealed class MainMenuCore
    {
        private bool hasSaveGame;

        /// <summary>Текущее состояние меню. Стартовое — Intro (ввод заблокирован до конца интро-анимации).</summary>
        public MainMenuState CurrentState { get; private set; } = MainMenuState.Intro;

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

        /// <summary>Можно ли выполнить команду прямо сейчас.</summary>
        public bool CanExecute(MainMenuCommand command)
        {
            if (CurrentState != MainMenuState.Idle) return false;

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
