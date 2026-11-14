using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Вью раздела настроек «Дисплей». Заполняет реальными данными системы:
    /// мониторы (Display.displays), разрешения (Screen.resolutions, дедупликация),
    /// лимиты кадров (по частоте обновления монитора). Яркость и контрастность
    /// сразу применяются к превью-изображению через шейдер
    /// VisualNovel/UI/BrightnessContrast — изменения видны до нажатия «Принять».
    /// Кнопка «Принять настройки» появляется при неприменённых изменениях
    /// (пока без функции), «Сбросить настройки» возвращает дефолты.
    /// Реальное применение (Screen.SetResolution, Screen.MoveMainWindowTo,
    /// Application.targetFrameRate) будет добавлено вместе с «Принять».
    /// </summary>
    public sealed class DisplaySettingsView : MonoBehaviour
    {
        [Header("Дропдауны")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown monitorDropdown;
        [SerializeField] private TMP_Dropdown screenModeDropdown;
        [SerializeField] private TMP_Dropdown fpsDropdown;

        [Header("Прочее")]
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider contrastSlider;

        [Header("Превью")]
        [SerializeField] private Image previewImage;
        [Tooltip("Шейдер VisualNovel/UI/BrightnessContrast")]
        [SerializeField] private Shader brightnessContrastShader;

        [Header("Кнопки")]
        [Tooltip("Появляется при неприменённых изменениях. Пока без функции")]
        [SerializeField] private GameObject acceptButton;
        [SerializeField] private Button resetButton;

        [Header("Диапазоны эффекта (слайдеры 0..1)")]
        [SerializeField] private float minBrightness = 0.5f;
        [SerializeField] private float maxBrightness = 1.5f;
        [SerializeField] private float minContrast = 0.5f;
        [SerializeField] private float maxContrast = 1.5f;

        private SettingsDraftModel model;
        private Material previewMaterial;

        /// <summary>Подключает раздел к черновику настроек. Вызывается из MainMenuRunner.</summary>
        public void Bind(SettingsDraftModel settingsModel)
        {
            model = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

            // Инициализация здесь, а не в Awake: панель выключена при старте,
            // и Awake выполнился бы позже Bind (дефолты регистрируются после заполнения).
            PopulateSystemDropdowns();

            if (previewImage != null && brightnessContrastShader != null)
            {
                previewMaterial = new Material(brightnessContrastShader);
                previewImage.material = previewMaterial;
                ApplyPreview(brightnessSlider != null ? brightnessSlider.value : 0.5f,
                             contrastSlider != null ? contrastSlider.value : 0.5f);
            }

            BindDropdown("resolution", resolutionDropdown);
            BindDropdown("monitor", monitorDropdown);
            BindDropdown("screenMode", screenModeDropdown);
            BindDropdown("fps", fpsDropdown);

            if (vsyncToggle != null)
            {
                model.Register("vsync", vsyncToggle.isOn ? 1 : 0);
                vsyncToggle.onValueChanged.AddListener(on => model.SetValue("vsync", on ? 1 : 0));
            }

            BindSlider("brightness", brightnessSlider);
            BindSlider("contrast", contrastSlider);

            model.ValueChanged += OnModelValueChanged;
            model.FloatValueChanged += OnModelFloatChanged;
            model.DirtyChanged += OnDirtyChanged;

            if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
            if (acceptButton != null) acceptButton.SetActive(model.IsDirty);
        }

        private void PopulateSystemDropdowns()
        {
            if (monitorDropdown != null)
            {
                monitorDropdown.ClearOptions();
                monitorDropdown.AddOptions(new List<string>(
                    DisplayOptionsBuilder.BuildMonitorNames(Display.displays.Length)));
            }

            if (resolutionDropdown != null)
            {
                var raw = new List<(int, int)>();
                foreach (var r in Screen.resolutions)
                    raw.Add((r.width, r.height));

                var options = DisplayOptionsBuilder.DeduplicateResolutions(raw);
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(options);

                string current = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
                int index = options.IndexOf(current);
                resolutionDropdown.SetValueWithoutNotify(index >= 0 ? index : 0);
                resolutionDropdown.RefreshShownValue();
            }

            if (fpsDropdown != null)
            {
                var caps = DisplayOptionsBuilder.BuildFpsCaps(Screen.currentResolution.refreshRateRatio.value);
                var options = new List<string>();
                int refresh = (int)Math.Round(Screen.currentResolution.refreshRateRatio.value);
                int defaultIndex = 0;
                for (int i = 0; i < caps.Count; i++)
                {
                    options.Add(DisplayOptionsBuilder.FormatFpsCap(caps[i]));
                    if (caps[i] == refresh) defaultIndex = i;
                }

                fpsDropdown.ClearOptions();
                fpsDropdown.AddOptions(options);
                fpsDropdown.SetValueWithoutNotify(defaultIndex);
                fpsDropdown.RefreshShownValue();
            }
        }

        private void BindDropdown(string key, TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;

            model.Register(key, dropdown.value);
            dropdown.onValueChanged.AddListener(value => model.SetValue(key, value));
        }

        private void BindSlider(string key, Slider slider)
        {
            if (slider == null) return;

            // Страховка: минимум слева, максимум справа — независимо от сцены.
            slider.direction = Slider.Direction.LeftToRight;

            model.RegisterFloat(key, slider.value);
            slider.onValueChanged.AddListener(value =>
            {
                model.SetFloatValue(key, value);
                ApplyPreview(
                    brightnessSlider != null ? brightnessSlider.value : 0.5f,
                    contrastSlider != null ? contrastSlider.value : 0.5f);
            });
        }

        private void OnModelValueChanged(string key, int value)
        {
            switch (key)
            {
                case "resolution": SetDropdown(resolutionDropdown, value); break;
                case "monitor": SetDropdown(monitorDropdown, value); break;
                case "screenMode": SetDropdown(screenModeDropdown, value); break;
                case "fps": SetDropdown(fpsDropdown, value); break;
                case "vsync":
                    if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(value != 0);
                    break;
            }
        }

        private void OnModelFloatChanged(string key, float value)
        {
            if (key == "brightness" && brightnessSlider != null)
                brightnessSlider.SetValueWithoutNotify(value);
            else if (key == "contrast" && contrastSlider != null)
                contrastSlider.SetValueWithoutNotify(value);

            ApplyPreview(model.GetFloatValue("brightness"), model.GetFloatValue("contrast"));
        }

        private static void SetDropdown(TMP_Dropdown dropdown, int value)
        {
            if (dropdown == null || dropdown.value == value) return;

            dropdown.SetValueWithoutNotify(value);
            dropdown.RefreshShownValue();
        }

        private void ApplyPreview(float brightness, float contrast)
        {
            if (previewMaterial == null) return;

            previewMaterial.SetFloat("_Brightness", Mathf.Lerp(minBrightness, maxBrightness, brightness));
            previewMaterial.SetFloat("_Contrast", Mathf.Lerp(minContrast, maxContrast, contrast));
        }

        private void OnDirtyChanged(bool isDirty)
        {
            if (acceptButton != null) acceptButton.SetActive(isDirty);
        }

        private void OnResetClicked()
        {
            // Сбрасываем только видимый раздел (шапка с кнопкой общая).
            if (!gameObject.activeInHierarchy) return;

            model.ResetToDefaults();
        }

        private void OnDestroy()
        {
            if (model != null)
            {
                model.ValueChanged -= OnModelValueChanged;
                model.FloatValueChanged -= OnModelFloatChanged;
                model.DirtyChanged -= OnDirtyChanged;
            }

            if (resetButton != null) resetButton.onClick.RemoveListener(OnResetClicked);
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }
}
