using System;
using System.Collections;
using UnityEngine;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Переключатель экранов меню (главное ↔ настройки). Подписывается на
    /// <see cref="MainMenuCore.TransitionStarted"/> и проигрывает переход.
    /// Анимации пока нет — переключение мгновенное, но точка расширения готова:
    /// анимацию ухода/появления панелей вставить в <see cref="SwitchRoutine"/>,
    /// а <see cref="MainMenuCore.CompleteTransition"/> вызывать по её концу.
    /// </summary>
    public sealed class MainMenuScreensView : MonoBehaviour
    {
        [Header("Панели экранов")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Переход (заглушка под анимацию)")]
        [Tooltip("Задержка переключения, сек. Пока анимации нет — 0: мгновенно")]
        [SerializeField, Min(0f)] private float transitionDuration;

        private MainMenuCore core;
        private Coroutine switching;

        /// <summary>Подключает переключатель к ядру. Вызывается из MainMenuRunner.</summary>
        public void Bind(MainMenuCore menuCore)
        {
            core = menuCore ?? throw new ArgumentNullException(nameof(menuCore));
            core.TransitionStarted += OnTransitionStarted;

            ShowPanel(core.CurrentScreen);
        }

        private void OnTransitionStarted(MainMenuScreen from, MainMenuScreen to)
        {
            if (switching != null) StopCoroutine(switching);

            switching = StartCoroutine(SwitchRoutine(from, to));
        }

        private IEnumerator SwitchRoutine(MainMenuScreen from, MainMenuScreen to)
        {
            // TODO: анимация ухода экрана from и появления экрана to.
            // Пока — мгновенное переключение панелей.
            var fromPanel = PanelFor(from);
            if (fromPanel != null) fromPanel.SetActive(false);

            ShowPanel(to);

            if (transitionDuration > 0f)
                yield return new WaitForSeconds(transitionDuration);
            else
                yield return null;

            switching = null;
            core.CompleteTransition();
        }

        private void ShowPanel(MainMenuScreen screen)
        {
            if (mainPanel != null) mainPanel.SetActive(screen == MainMenuScreen.Main);
            if (settingsPanel != null) settingsPanel.SetActive(screen == MainMenuScreen.Settings);
        }

        private GameObject PanelFor(MainMenuScreen screen) => screen switch
        {
            MainMenuScreen.Main => mainPanel,
            MainMenuScreen.Settings => settingsPanel,
            _ => null
        };

        private void OnDestroy()
        {
            if (core != null) core.TransitionStarted -= OnTransitionStarted;
        }
    }
}
