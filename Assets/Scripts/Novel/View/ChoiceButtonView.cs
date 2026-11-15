using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Кнопка варианта ответа. Состояния по ТЗ:
    /// обычная — БЕЗ фона, белый текст; hover — белый фон, тёмный текст;
    /// нажатие (Active) — чёрный фон, белый текст.
    /// Показом бонусов управляет ChoiceWheelView через события Hovered/Unhovered.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ChoiceButtonView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Ссылки (пусто — авто-поиск)")]
        [SerializeField] private Graphic background;   // Image самой кнопки
        [SerializeField] private TMP_Text label;       // дочерний "Label"
        [SerializeField] private Graphic icon;         // дочерний "Ico" (может отсутствовать)

        [Header("Цвета")]
        [Tooltip("Обычное состояние: фона нет совсем")]
        [SerializeField] private Color normalBackground = new(1f, 1f, 1f, 0f);
        [Tooltip("Hover: белый фон, тёмный текст")]
        [SerializeField] private Color hoverBackground = Color.white;
        [SerializeField] private Color normalContent = Color.white;
        [SerializeField] private Color hoverContent = new(0.18f, 0.26f, 0.40f, 1f);
        [Tooltip("Нажатие (Active): чёрный фон, белый текст")]
        [SerializeField] private Color pressedBackground = Color.black;
        [SerializeField] private Color pressedContent = Color.white;

        [Header("Подсветка при выборе с клавиатуры/геймпада")]
        [Min(0.01f)] [SerializeField] private float pressFlashDuration = 0.15f;

        /// <summary>Кнопка в hover (мышь или выбор с клавиатуры/геймпада).</summary>
        public event Action Hovered;

        /// <summary>Кнопка покинула hover.</summary>
        public event Action Unhovered;

        private Button button;
        private bool hovered;
        private bool pressed;
        private bool initialized;

        private void Awake() => EnsureInitialized();

        /// <summary>Unity Button этой кнопки (для подписки на onClick).</summary>
        public Button Button
        {
            get
            {
                EnsureInitialized();
                return button;
            }
        }

        /// <summary>Текст варианта на кнопке.</summary>
        public void SetText(string text)
        {
            EnsureInitialized();
            if (label != null) label.text = text ?? string.Empty;
        }

        /// <summary>Включает/выключает нажатия. При выключении сбрасывает визуал.</summary>
        public void SetInteractable(bool interactable)
        {
            EnsureInitialized();
            button.interactable = interactable;
            if (!interactable) ResetVisual();
        }

        /// <summary>Программный hover (выбор с клавиатуры/геймпада до подтверждения).</summary>
        public void ShowHovered()
        {
            EnsureInitialized();
            if (hovered) return;
            hovered = true;
            RefreshColors();
            Hovered?.Invoke();
        }

        /// <summary>Снять программный hover (курсор направления ушёл на другую кнопку).</summary>
        public void ClearHover()
        {
            if (!hovered) return;
            hovered = false;
            RefreshColors();
            Unhovered?.Invoke();
        }

        /// <summary>Подтверждение с клавиатуры/геймпада: вспышка нажатия + клик.</summary>
        public void SimulatePress()
        {
            EnsureInitialized();
            if (!button.interactable || !gameObject.activeInHierarchy) return;
            StartCoroutine(FlashPressed());
            button.onClick.Invoke();
        }

        /// <summary>Исходное состояние: без фона, белый контент.</summary>
        public void ResetVisual()
        {
            hovered = false;
            pressed = false;
            ApplyColors(normalBackground, normalContent);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureInitialized();
            if (!button.interactable) return;
            hovered = true;
            RefreshColors();
            Hovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
            RefreshColors();
            Unhovered?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            EnsureInitialized();
            if (!button.interactable) return;
            pressed = true;
            RefreshColors();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            RefreshColors();
        }

        private void RefreshColors()
        {
            if (pressed) ApplyColors(pressedBackground, pressedContent); // Active: чёрный фон
            else if (hovered) ApplyColors(hoverBackground, hoverContent); // hover: белый фон
            else ApplyColors(normalBackground, normalContent); // обычная: без фона
        }

        private void ApplyColors(Color backgroundColor, Color contentColor)
        {
            if (background != null) background.color = backgroundColor;
            if (label != null) label.color = contentColor;
            if (icon != null) icon.color = contentColor;
        }

        private IEnumerator FlashPressed()
        {
            pressed = true;
            RefreshColors();
            yield return new WaitForSeconds(pressFlashDuration);
            pressed = false;
            RefreshColors();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            button = GetComponent<Button>();
            button.transition = Selectable.Transition.None; // цветами управляем сами

            if (background == null) background = GetComponent<Graphic>();
            if (label == null) label = FindChild<TMP_Text>("Label");
            if (icon == null) icon = FindChild<Graphic>("Ico");
            if (label == null)
                Debug.LogWarning($"[Novel] ChoiceButtonView: у '{name}' не найден дочерний Label.", this);

            ResetVisual();
        }

        private T FindChild<T>(string childName) where T : Component
        {
            foreach (var child in GetComponentsInChildren<T>(true))
                if (child.name == childName) return child;
            return null;
        }
    }
}
