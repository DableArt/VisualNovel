using System;
using System.Collections.Generic;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Черновик настроек. Чистый C#-класс без зависимостей на визуал:
    /// хранит значения по ключам и их дефолты (дефолты регистрирует вью
    /// из сцены при биндинге). Есть неприменённые изменения — <see cref="IsDirty"/>.
    /// Пока кнопка «Принять настройки» не работает, dirty считается
    /// относительно дефолтов; когда появится применение — здесь добавится
    /// коммит черновика в постоянные настройки.
    /// </summary>
    public sealed class SettingsDraftModel
    {
        private readonly Dictionary<string, int> defaults = new();
        private readonly Dictionary<string, int> values = new();
        private readonly Dictionary<string, float> floatDefaults = new();
        private readonly Dictionary<string, float> floatValues = new();

        /// <summary>Есть ли значения, отличающиеся от дефолтных.</summary>
        public bool IsDirty { get; private set; }

        /// <summary>Значение по ключу изменилось (в т.ч. при сбросе — чтобы вью обновило контролы).</summary>
        public event Action<string, int> ValueChanged;

        /// <summary>Float-значение по ключу изменилось (слайдеры).</summary>
        public event Action<string, float> FloatValueChanged;

        /// <summary>Флаг <see cref="IsDirty"/> изменился — показать/скрыть кнопку «Принять настройки».</summary>
        public event Action<bool> DirtyChanged;

        /// <summary>Регистрирует настройку с дефолтным значением. Вызывается вью при биндинге.</summary>
        public void Register(string key, int defaultValue)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Ключ настройки пустой.", nameof(key));

            defaults[key] = defaultValue;
            values[key] = defaultValue;
        }

        /// <summary>Регистрирует float-настройку с дефолтным значением (слайдеры).</summary>
        public void RegisterFloat(string key, float defaultValue)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Ключ настройки пустой.", nameof(key));

            floatDefaults[key] = defaultValue;
            floatValues[key] = defaultValue;
        }

        /// <summary>Текущее значение настройки (0, если ключ не зарегистрирован).</summary>
        public int GetValue(string key) => values.TryGetValue(key, out var value) ? value : 0;

        /// <summary>Текущее float-значение настройки (0, если ключ не зарегистрирован).</summary>
        public float GetFloatValue(string key) => floatValues.TryGetValue(key, out var value) ? value : 0f;

        /// <summary>Меняет значение. Одинаковое значение игнорируется.</summary>
        public void SetValue(string key, int value)
        {
            if (!values.ContainsKey(key))
                throw new InvalidOperationException($"[Settings] Ключ '{key}' не зарегистрирован.");
            if (values[key] == value) return;

            values[key] = value;
            ValueChanged?.Invoke(key, value);
            UpdateDirty();
        }

        /// <summary>Меняет float-значение. Одинаковое значение игнорируется.</summary>
        public void SetFloatValue(string key, float value)
        {
            if (!floatValues.ContainsKey(key))
                throw new InvalidOperationException($"[Settings] Ключ '{key}' не зарегистрирован.");
            if (Math.Abs(floatValues[key] - value) < 0.0001f) return;

            floatValues[key] = value;
            FloatValueChanged?.Invoke(key, value);
            UpdateDirty();
        }

        /// <summary>Сбрасывает все значения на дефолты.</summary>
        public void ResetToDefaults()
        {
            foreach (var pair in defaults)
            {
                if (values[pair.Key] == pair.Value) continue;

                values[pair.Key] = pair.Value;
                ValueChanged?.Invoke(pair.Key, pair.Value);
            }

            foreach (var pair in floatDefaults)
            {
                if (Math.Abs(floatValues[pair.Key] - pair.Value) < 0.0001f) continue;

                floatValues[pair.Key] = pair.Value;
                FloatValueChanged?.Invoke(pair.Key, pair.Value);
            }

            UpdateDirty();
        }

        private void UpdateDirty()
        {
            bool dirty = false;
            foreach (var pair in values)
            {
                if (defaults.TryGetValue(pair.Key, out var defaultValue) && defaultValue != pair.Value)
                {
                    dirty = true;
                    break;
                }
            }

            if (!dirty)
                foreach (var pair in floatValues)
                {
                    if (floatDefaults.TryGetValue(pair.Key, out var defaultValue)
                        && Math.Abs(defaultValue - pair.Value) >= 0.0001f)
                    {
                        dirty = true;
                        break;
                    }
                }

            if (dirty == IsDirty) return;

            IsDirty = dirty;
            DirtyChanged?.Invoke(dirty);
        }
    }
}
