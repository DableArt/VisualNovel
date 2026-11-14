using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Боковое меню разделов настроек. Активный раздел не реагирует на hover
    /// (его подсветка — белая плашка, размечена в сцене). Пунктам НИЖЕ активного
    /// добавляется бордовый hover-фон; пункты выше активного остаются без hover.
    /// Клик по пункту — переход на раздел через <see cref="SettingsSectionSwitcher"/>
    /// (Fade In Up). Пункты без раздела (заглушки) кликабельны, но ничего не делают.
    /// </summary>
    public sealed class SettingsSidebarView : MonoBehaviour
    {
        [Serializable]
        private struct Tab
        {
            [Tooltip("Ключ раздела из SettingsSectionSwitcher. Пусто — заглушка")]
            public string sectionKey;
            public Button button;
            [Tooltip("Фон пункта: окрашивается в бордовый при hover")]
            public Image background;
        }

        [SerializeField] private SettingsSectionSwitcher switcher;

        [Tooltip("Первый — активный раздел этой панели")]
        [SerializeField] private Tab[] tabs = new Tab[0];

        [Header("Hover")]
        [SerializeField] private Color hoverColor = new(0.49f, 0.004f, 0.004f, 1f);

        private void Start()
        {
            for (int i = 1; i < tabs.Length; i++) // tabs[0] — активный раздел, hover не нужен
            {
                var tab = tabs[i];
                if (tab.button == null) continue;

                var trigger = tab.button.gameObject.AddComponent<EventTrigger>();
                AddHandler(trigger, EventTriggerType.PointerEnter, () => SetHover(tab.background, true));
                AddHandler(trigger, EventTriggerType.PointerExit, () => SetHover(tab.background, false));

                var key = tab.sectionKey; // захват для замыкания
                tab.button.onClick.AddListener(() => OnTabClicked(key));
            }
        }

        private void SetHover(Image background, bool hovered)
        {
            if (background == null) return;

            background.color = hovered
                ? hoverColor
                : new Color(hoverColor.r, hoverColor.g, hoverColor.b, 0f);
        }

        private void OnTabClicked(string sectionKey)
        {
            if (string.IsNullOrEmpty(sectionKey) || switcher == null) return;

            SetAllNormal();
            switcher.ShowSection(sectionKey);
        }

        private void SetAllNormal()
        {
            foreach (var tab in tabs)
                SetHover(tab.background, false);
        }

        private static void AddHandler(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
    }
}
