using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Индикатор времени выбора («Элипс»): радиальная заливка, убывающая
    /// по часовой стрелке, + текст с секундами обратного отсчёта.
    /// Сам время не считает — отображает остаток из ядра (R3-подписка).
    /// </summary>
    public sealed class ChoiceTimerView : MonoBehaviour
    {
        [Header("Ссылки (пусто — поиск по имени среди детей)")]
        [SerializeField] private Image indicator;   // "TimeIndicator" — эллипс
        [SerializeField] private TMP_Text timeText; // "TimeText"

        private float limit;
        private bool initialized;
        private IDisposable subscription;

        private void Awake() => EnsureInitialized();

        /// <summary>Подписка на остаток времени. Вызывается из NovelSessionRunner.</summary>
        public void Bind(ReadOnlyReactiveProperty<float> remainingTime)
        {
            subscription?.Dispose();
            if (remainingTime == null) return;
            subscription = remainingTime.Subscribe(UpdateDisplay);
        }

        /// <summary>Настраивает индикатор под лимит времени узла и показывает его.</summary>
        public void Prepare(float timeLimit)
        {
            EnsureInitialized();
            limit = timeLimit;

            bool timed = timeLimit > 0f;
            gameObject.SetActive(timed);
            if (!timed) return;

            if (indicator != null)
            {
                indicator.type = Image.Type.Filled;
                indicator.fillMethod = Image.FillMethod.Radial360;
                indicator.fillOrigin = (int)Image.Origin360.Top;
                indicator.fillClockwise = true; // убывание по часовой стрелке
                indicator.fillAmount = 1f;
            }

            if (timeText != null)
                timeText.text = Mathf.CeilToInt(timeLimit).ToString();
        }

        /// <summary>Мгновенно скрыть индикатор.</summary>
        public void Hide() => gameObject.SetActive(false);

        private void UpdateDisplay(float remaining)
        {
            if (!initialized || !gameObject.activeSelf || limit <= 0f) return;

            if (indicator != null)
                indicator.fillAmount = Mathf.Clamp01(remaining / limit);
            if (timeText != null)
                timeText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            if (indicator == null) indicator = FindChild<Image>("TimeIndicator");
            if (timeText == null) timeText = FindChild<TMP_Text>("TimeText");
            if (indicator == null)
                Debug.LogWarning("[Novel] ChoiceTimerView: не найден дочерний TimeIndicator.", this);
        }

        private T FindChild<T>(string childName) where T : Component
        {
            foreach (var child in GetComponentsInChildren<T>(true))
                if (child.name == childName) return child;
            return null;
        }

        private void OnDestroy() => subscription?.Dispose();
    }
}
