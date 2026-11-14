using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Переключатель экранов меню (главное ↔ настройки). Подписывается на
    /// <see cref="MainMenuCore.TransitionStarted"/> и проигрывает переход:
    /// главное → настройки — покадровая анимация (<see cref="TransitionFramePlayerView"/>),
    /// панели переключаются в середине кадров; настройки → главное — мгновенно.
    /// Контент целевого экрана появляется фейдом (CanvasGroup), ввод на нём
    /// заблокирован до конца фейда. По завершении вызывает
    /// <see cref="MainMenuCore.CompleteTransition"/> — ядро разблокирует команды.
    /// </summary>
    public sealed class MainMenuScreensView : MonoBehaviour
    {
        [Header("Панели экранов")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Переход")]
        [Tooltip("Покадровая анимация перехода в настройки. Пусто/без кадров — мгновенное переключение")]
        [SerializeField] private TransitionFramePlayerView framePlayer;
        [Tooltip("Группы контента для фейд-появления (CanvasGroup на панелях)")]
        [SerializeField] private CanvasGroup mainPanelGroup;
        [SerializeField] private CanvasGroup settingsPanelGroup;
        [Tooltip("Длительность фейд-появления контента, сек. 0 — мгновенно")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.3f;

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
            // Снимаем выделение до скрытия панели: иначе EventSystem держит
            // выключенный объект выбранным — навигация и нажатия ломаются.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            if (switching != null) StopCoroutine(switching);

            switching = StartCoroutine(SwitchRoutine(from, to));
        }

        private IEnumerator SwitchRoutine(MainMenuScreen from, MainMenuScreen to)
        {
            var fromPanel = PanelFor(from);
            var toPanel = PanelFor(to);
            var targetGroup = GroupFor(to);

            // Целевая панель включается невидимой — появится фейдом.
            if (targetGroup != null)
            {
                targetGroup.alpha = 0f;
                targetGroup.interactable = false;
                targetGroup.blocksRaycasts = false;
            }

            // Покадровая анимация — только при входе в настройки.
            // Обратный переход мгновенный; кадры для него можно добавить позже.
            bool withFrames = framePlayer != null && framePlayer.HasFrames
                && from == MainMenuScreen.Main && to == MainMenuScreen.Settings;

            if (withFrames)
            {
                yield return framePlayer.Play(() =>
                {
                    if (fromPanel != null) fromPanel.SetActive(false);
                    if (toPanel != null) toPanel.SetActive(true);
                });
            }
            else
            {
                if (fromPanel != null) fromPanel.SetActive(false);
                if (toPanel != null) toPanel.SetActive(true);
            }

            if (targetGroup != null)
            {
                yield return FadeIn(targetGroup);
                targetGroup.interactable = true;
                targetGroup.blocksRaycasts = true;
            }

            switching = null;
            core.CompleteTransition();
        }

        private IEnumerator FadeIn(CanvasGroup group)
        {
            if (fadeDuration <= 0f)
            {
                group.alpha = 1f;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            group.alpha = 1f;
        }

        private void ShowPanel(MainMenuScreen screen)
        {
            if (mainPanel != null) mainPanel.SetActive(screen == MainMenuScreen.Main);
            if (settingsPanel != null) settingsPanel.SetActive(screen == MainMenuScreen.Settings);

            // Начальное состояние без анимации: активный экран сразу виден и интерактивен.
            var active = GroupFor(screen);
            if (active != null)
            {
                active.alpha = 1f;
                active.interactable = true;
                active.blocksRaycasts = true;
            }
        }

        private GameObject PanelFor(MainMenuScreen screen) => screen switch
        {
            MainMenuScreen.Main => mainPanel,
            MainMenuScreen.Settings => settingsPanel,
            _ => null
        };

        private CanvasGroup GroupFor(MainMenuScreen screen) => screen switch
        {
            MainMenuScreen.Main => mainPanelGroup,
            MainMenuScreen.Settings => settingsPanelGroup,
            _ => null
        };

        private void OnDestroy()
        {
            if (core != null) core.TransitionStarted -= OnTransitionStarted;
        }
    }
}
