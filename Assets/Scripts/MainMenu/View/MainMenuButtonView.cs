using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Вью кнопки главного меню: рисует состояния модели <see cref="MenuButtonModel"/>.
    /// NotActive — без фона; Hovered — белый квадратный фон; Pressed — красный текст.
    /// Вся логика переходов — в модели, здесь только отображение.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuButtonView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Ссылки")]
        [Tooltip("Белый квадратный фон кнопки. Прячется прозрачностью, компонент не выключается — " +
                 "иначе кнопка потеряет raycast-цель и перестанет реагировать на мышь")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        [Header("Цвета состояний")]
        [SerializeField] private Color normalTextColor = new(0.196f, 0.196f, 0.196f, 1f);
        [SerializeField] private Color pressedTextColor = new(0.85f, 0.12f, 0.16f, 1f);
        [SerializeField] private Color disabledTextColor = new(0.196f, 0.196f, 0.196f, 0.4f);
        [SerializeField] private Color hoverBackgroundColor = Color.white;

        private Button button;
        private MenuButtonModel model;

        /// <summary>Подтверждённый клик по кнопке (мышь или Submit с клавиатуры/геймпада).</summary>
        public event Action Clicked;

        private void Awake()
        {
            button = GetComponent<Button>();
            // Состояния рисует модель — стандартный тинт uGUI Button отключаем.
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnButtonClick);

            model = new MenuButtonModel();
            model.StateChanged += ApplyState;
            ApplyState(model.CurrentState);
        }

        /// <summary>Включает/выключает кнопку (например «Продолжить» без сохранения).</summary>
        public void SetInteractable(bool interactable)
        {
            model.SetEnabled(interactable);
            button.interactable = interactable;
            ApplyState(model.CurrentState); // обновить disabled-цвет текста
        }

        public void OnPointerEnter(PointerEventData eventData) => model.PointerEnter();

        public void OnPointerExit(PointerEventData eventData) => model.PointerExit();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                model.PointerDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                model.PointerUp();
        }

        // Навигация с клавиатуры/геймпада: выбранная кнопка выглядит как под курсором.
        public void OnSelect(BaseEventData eventData) => model.PointerEnter();

        public void OnDeselect(BaseEventData eventData) => model.PointerExit();

        private void ApplyState(MenuButtonVisualState state)
        {
            if (background != null)
            {
                bool hovered = model.IsEnabled && state == MenuButtonVisualState.Hovered;
                background.color = hovered
                    ? hoverBackgroundColor
                    : new Color(hoverBackgroundColor.r, hoverBackgroundColor.g, hoverBackgroundColor.b, 0f);
            }

            if (label != null)
                label.color = !model.IsEnabled ? disabledTextColor
                    : state == MenuButtonVisualState.Pressed ? pressedTextColor
                    : normalTextColor;
        }

        private void OnButtonClick() => Clicked?.Invoke();

        private void OnDestroy()
        {
            if (model != null) model.StateChanged -= ApplyState;
            if (button != null) button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
