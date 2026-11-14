using System;
using System.Collections;
using UnityEngine;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Переключатель разделов настроек (Основные ↔ Дисплей ↔ ...) с анимацией
    /// Fade In Up: новый раздел проявляется снизу вверх, старый гаснет мгновенно.
    /// Живёт на SettingsPanel — шапка (назад/заголовок/сброс) лежит снаружи
    /// разделов и анимацией не затрагивается. Во время анимации ввод на
    /// разделе заблокирован, повторные клики игнорируются.
    /// </summary>
    public sealed class SettingsSectionSwitcher : MonoBehaviour
    {
        [Serializable]
        public sealed class Section
        {
            public string key;
            public GameObject panel;
            public CanvasGroup group;
        }

        [SerializeField] private Section[] sections = new Section[0];

        [Tooltip("Раздел по умолчанию при открытии настроек")]
        [SerializeField] private string defaultSectionKey = "main";

        [Header("Fade In Up")]
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        [SerializeField, Min(0f)] private float yOffset = 40f;

        private bool animating;

        /// <summary>Ключ активного раздела.</summary>
        public string CurrentKey { get; private set; }

        /// <summary>Активирует раздел по умолчанию, гасит остальные. Вызывается при открытии настроек.</summary>
        public void ResetToDefault() => ShowImmediate(defaultSectionKey);

        /// <summary>Переключает раздел с анимацией Fade In Up. Неизвестный/текущий ключ игнорируется.</summary>
        public void ShowSection(string key)
        {
            if (animating || key == CurrentKey) return;

            var target = Find(key);
            if (target == null) return;

            StartCoroutine(SwitchRoutine(target));
        }

        private void ShowImmediate(string key)
        {
            foreach (var section in sections)
            {
                bool active = section.key == key;
                if (section.panel != null) section.panel.SetActive(active);
                if (section.group != null)
                {
                    section.group.alpha = 1f;
                    section.group.interactable = active;
                    section.group.blocksRaycasts = active;
                }
            }

            CurrentKey = key;
        }

        private IEnumerator SwitchRoutine(Section target)
        {
            animating = true;

            var current = Find(CurrentKey);
            if (current?.panel != null) current.panel.SetActive(false);
            if (current?.group != null)
            {
                current.group.interactable = false;
                current.group.blocksRaycasts = false;
            }

            var rect = target.panel != null ? target.panel.transform as RectTransform : null;
            Vector2 basePos = rect != null ? rect.anchoredPosition : Vector2.zero;

            if (target.panel != null) target.panel.SetActive(true);
            if (target.group != null)
            {
                target.group.alpha = 0f;
                target.group.interactable = false;
                target.group.blocksRaycasts = false;
            }
            if (rect != null) rect.anchoredPosition = basePos + Vector2.down * yOffset;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out

                if (target.group != null) target.group.alpha = eased;
                if (rect != null) rect.anchoredPosition = Vector2.Lerp(basePos + Vector2.down * yOffset, basePos, eased);

                yield return null;
            }

            if (target.group != null)
            {
                target.group.alpha = 1f;
                target.group.interactable = true;
                target.group.blocksRaycasts = true;
            }
            if (rect != null) rect.anchoredPosition = basePos;

            CurrentKey = target.key;
            animating = false;
        }

        private Section Find(string key)
        {
            foreach (var section in sections)
                if (section.key == key) return section;
            return null;
        }
    }
}
