using System;
using R3;
using UnityEngine;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// MonoBehaviour-обёртка над ядром главного меню <see cref="MainMenuCore"/>:
    /// связывает кнопки сцены с командами, транслирует события ядра
    /// и содержит заглушки обработчиков (сцены, сохранения — позже).
    /// Состояние меню выставлено как R3 ReactiveProperty — на него подпишутся
    /// фоновая анимация и анимация появления кнопок.
    /// </summary>
    public sealed class MainMenuRunner : MonoBehaviour
    {
        [Serializable]
        private struct ButtonBinding
        {
            public MainMenuCommand command;
            public MainMenuButtonView button;
        }

        [Header("Кнопки")]
        [SerializeField] private ButtonBinding[] buttons = new ButtonBinding[0];

        [Header("Заглушки (пока нет систем)")]
        [Tooltip("Временно: есть ли сохранение. Потом значение придёт из системы сохранений")]
        [SerializeField] private bool hasSaveGame;

        private readonly ReactiveProperty<MainMenuState> state = new(MainMenuState.Intro);

        /// <summary>Состояние меню — реактивная подписка для фоновых анимаций и анимации появления кнопок.</summary>
        public ReadOnlyReactiveProperty<MainMenuState> State => state;

        /// <summary>Ядро меню (для внешних систем: сохранения, отладка).</summary>
        public MainMenuCore Core { get; private set; }

        private void Start()
        {
            Core = new MainMenuCore { HasSaveGame = hasSaveGame };
            Core.StateChanged += OnCoreStateChanged;
            Core.CommandInvoked += OnCommandInvoked;
            Core.SaveAvailabilityChanged += OnSaveAvailabilityChanged;

            foreach (var binding in buttons)
            {
                if (binding.button == null) continue;

                var command = binding.command; // захват для замыкания
                binding.button.Clicked += () => Core.InvokeCommand(command);
            }

            RefreshButtons();

            // Интро-анимации пока нет — меню сразу интерактивно.
            // Потом этот вызов переедет на конец фоновой анимации/анимации появления кнопок.
            Core.CompleteIntro();
        }

        private void OnCoreStateChanged(MainMenuState next)
        {
            state.Value = next;
            RefreshButtons();
        }

        private void OnSaveAvailabilityChanged(bool available) => RefreshButtons();

        private void RefreshButtons()
        {
            if (Core == null) return;

            foreach (var binding in buttons)
                if (binding.button != null)
                    binding.button.SetInteractable(Core.CanExecute(binding.command));
        }

        private void OnCommandInvoked(MainMenuCommand command)
        {
            switch (command)
            {
                case MainMenuCommand.Continue:
                    Debug.Log("[MainMenu] Продолжить (TODO: загрузка сохранения)", this);
                    break;
                case MainMenuCommand.NewGame:
                    Debug.Log("[MainMenu] Новая игра (TODO: загрузка игровой сцены)", this);
                    break;
                case MainMenuCommand.LoadGame:
                    Debug.Log("[MainMenu] Загрузить (TODO: экран загрузки)", this);
                    break;
                case MainMenuCommand.AdditionalMaterials:
                    Debug.Log("[MainMenu] Доп. материалы (TODO: подменю)", this);
                    break;
                case MainMenuCommand.Settings:
                    Debug.Log("[MainMenu] Настройки (TODO: подменю)", this);
                    break;
                case MainMenuCommand.Exit:
                    ExitGame();
                    break;
            }
        }

        private static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            state.Dispose();

            if (Core != null)
            {
                Core.StateChanged -= OnCoreStateChanged;
                Core.CommandInvoked -= OnCommandInvoked;
                Core.SaveAvailabilityChanged -= OnSaveAvailabilityChanged;
            }
        }
    }
}
