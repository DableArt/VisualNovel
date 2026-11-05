using System;
using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Панель выбора ответа: до 6 кнопок (жёсткий максимум) + полоса таймера.
    /// Никакой логики сюжета — только отображает то, что дало ядро,
    /// и шлёт обратно индекс выбранного варианта.
    /// </summary>
    public sealed class ChoicePanelView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [Tooltip("Ровно 6 кнопок — жёсткий максимум вариантов. Лишние прячутся")]
        [SerializeField] private Button[] choiceButtons = new Button[NovelCoreConfig.HardMaxChoices];
        [Tooltip("Полоса оставшегося времени. Пусто — без индикации")]
        [SerializeField] private Slider timerBar;

        private NovelSession session;
        private IDisposable timerSubscription;

        /// <summary>Подключает панель к ядру. Вызывается из NovelSessionRunner.</summary>
        public void Bind(NovelSession novelSession, ReadOnlyReactiveProperty<float> remainingTime)
        {
            session = novelSession ?? throw new ArgumentNullException(nameof(novelSession));

            session.ChoicesPresented += OnChoicesPresented;
            session.ChoiceResolved += OnChoiceResolved;
            session.SessionFinished += Hide;

            if (remainingTime != null)
                timerSubscription = remainingTime.Subscribe(UpdateTimerBar);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i; // захват для замыкания
                var button = choiceButtons[i];
                if (button == null) continue;
                button.onClick.AddListener(() => session.SelectChoice(index));
            }

            Hide();
        }

        private void OnChoicesPresented(IReadOnlyList<ChoiceOption> options, float timeLimit)
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                var button = choiceButtons[i];
                if (button == null) continue;

                bool visible = i < options.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = options[i].text;
            }

            if (timerBar != null)
            {
                bool timed = timeLimit > 0f;
                timerBar.gameObject.SetActive(timed);
                if (timed)
                {
                    timerBar.maxValue = timeLimit;
                    timerBar.value = timeLimit;
                }
            }
        }

        private void OnChoiceResolved(ChoiceOption option, bool byTimeout) => Hide();

        private void UpdateTimerBar(float remaining)
        {
            if (timerBar != null && timerBar.gameObject.activeSelf)
                timerBar.value = remaining;
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            timerSubscription?.Dispose();

            if (session != null)
            {
                session.ChoicesPresented -= OnChoicesPresented;
                session.ChoiceResolved -= OnChoiceResolved;
                session.SessionFinished -= Hide;
            }
        }
    }
}
