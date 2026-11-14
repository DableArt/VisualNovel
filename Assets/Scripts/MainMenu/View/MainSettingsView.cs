using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Вью раздела настроек «Основные»: привязывает дропдауны к черновику
    /// <see cref="SettingsDraftModel"/>, показывает кнопку «Принять настройки»,
    /// когда есть неприменённые изменения (сама кнопка пока без функции),
    /// «Сбросить настройки» возвращает дефолты, «Назад» — в главное меню.
    /// Дефолты берутся из значений дропдаунов, выставленных в сцене.
    /// </summary>
    public sealed class MainSettingsView : MonoBehaviour
    {
        [Serializable]
        private struct DropdownBinding
        {
            public string key;
            public TMP_Dropdown dropdown;
        }

        [Header("Настройки раздела")]
        [SerializeField] private DropdownBinding[] dropdowns = new DropdownBinding[0];

        [Header("Кнопки")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;
        [Tooltip("Появляется при неприменённых изменениях. Пока без функции")]
        [SerializeField] private GameObject acceptButton;

        private SettingsDraftModel model;
        private MainMenuCore core;

        /// <summary>Подключает раздел к моделям. Вызывается из MainMenuRunner.</summary>
        public void Bind(SettingsDraftModel settingsModel, MainMenuCore menuCore)
        {
            model = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
            core = menuCore ?? throw new ArgumentNullException(nameof(menuCore));

            foreach (var binding in dropdowns)
            {
                if (binding.dropdown == null || string.IsNullOrEmpty(binding.key)) continue;

                model.Register(binding.key, binding.dropdown.value);

                var key = binding.key; // захват для замыкания
                binding.dropdown.onValueChanged.AddListener(value => model.SetValue(key, value));
            }

            model.ValueChanged += OnModelValueChanged;
            model.DirtyChanged += OnDirtyChanged;

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
            if (acceptButton != null) acceptButton.SetActive(model.IsDirty);
        }

        private void OnModelValueChanged(string key, int value)
        {
            // Синхронизация контролов при сбросе — без повторного события в модель.
            foreach (var binding in dropdowns)
                if (binding.key == key && binding.dropdown != null && binding.dropdown.value != value)
                    binding.dropdown.SetValueWithoutNotify(value);
        }

        private void OnDirtyChanged(bool isDirty)
        {
            if (acceptButton != null) acceptButton.SetActive(isDirty);
        }

        private void OnBackClicked() => core.BackToMain();

        private void OnResetClicked()
        {
            // Кнопка сброса общая на все разделы — сбрасываем только видимый.
            if (!gameObject.activeInHierarchy) return;

            model.ResetToDefaults();
        }

        private void OnDestroy()
        {
            if (model != null)
            {
                model.ValueChanged -= OnModelValueChanged;
                model.DirtyChanged -= OnDirtyChanged;
            }

            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (resetButton != null) resetButton.onClick.RemoveListener(OnResetClicked);
        }
    }
}
