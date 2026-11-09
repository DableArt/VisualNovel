using System;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Чистый C#-автомат состояний кнопки меню (без зависимостей на Unity UI):
    /// принимает события указателя/навигации и переключает
    /// <see cref="MenuButtonVisualState"/>. Визуал применяет вью по событию StateChanged.
    /// Выключенная кнопка игнорирует ввод и всегда в состоянии NotActive.
    /// </summary>
    public sealed class MenuButtonModel
    {
        /// <summary>Кнопка доступна для взаимодействия.</summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>Указатель сейчас над кнопкой (или она выбрана навигацией).</summary>
        public bool IsPointerInside { get; private set; }

        /// <summary>Текущее визуальное состояние.</summary>
        public MenuButtonVisualState CurrentState { get; private set; } = MenuButtonVisualState.NotActive;

        /// <summary>Состояние сменилось — вью должно перерисовать кнопку.</summary>
        public event Action<MenuButtonVisualState> StateChanged;

        /// <summary>Включает/выключает кнопку. При выключении сбрасывает состояние в NotActive.</summary>
        public void SetEnabled(bool enabled)
        {
            if (IsEnabled == enabled) return;

            IsEnabled = enabled;
            if (!enabled)
            {
                IsPointerInside = false;
                SetState(MenuButtonVisualState.NotActive);
            }
        }

        /// <summary>Указатель вошёл / кнопка выбрана навигацией (клавиатура, геймпад).</summary>
        public void PointerEnter()
        {
            if (!IsEnabled) return;

            IsPointerInside = true;
            if (CurrentState != MenuButtonVisualState.Pressed)
                SetState(MenuButtonVisualState.Hovered);
        }

        /// <summary>Указатель вышел / кнопка сброшена навигацией.</summary>
        public void PointerExit()
        {
            IsPointerInside = false;
            if (!IsEnabled) return;

            SetState(MenuButtonVisualState.NotActive);
        }

        /// <summary>Кнопка нажата (кнопка мыши зажата над ней).</summary>
        public void PointerDown()
        {
            if (!IsEnabled || !IsPointerInside) return;

            SetState(MenuButtonVisualState.Pressed);
        }

        /// <summary>Кнопка отпущена: возврат в Hovered, если указатель ещё над ней, иначе в NotActive.</summary>
        public void PointerUp()
        {
            if (!IsEnabled) return;

            SetState(IsPointerInside ? MenuButtonVisualState.Hovered : MenuButtonVisualState.NotActive);
        }

        private void SetState(MenuButtonVisualState next)
        {
            if (CurrentState == next) return;

            CurrentState = next;
            StateChanged?.Invoke(next);
        }
    }
}
