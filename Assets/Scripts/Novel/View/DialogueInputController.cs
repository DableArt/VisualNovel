using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Ввод для «колеса» выбора: WASD + стрелки (клавиатура),
    /// D-Pad и левый стик (геймпад — заложено на будущее, уже работает).
    /// Нажатие направления = выбор варианта в этой позиции.
    /// Новая Input System (com.unity.inputsystem).
    /// </summary>
    public sealed class DialogueInputController : MonoBehaviour
    {
        [Tooltip("Порог левого стика геймпада для срабатывания направления")]
        [Range(0.1f, 0.95f)] [SerializeField] private float stickDeadzone = 0.5f;

        /// <summary>Направление нажато (клавиатура или геймпад).</summary>
        public event Action<ChoiceDirection> DirectionPressed;

        /// <summary>Приём ввода включён (пока кнопки доступны игроку).</summary>
        public bool Enabled { get; set; }

        private ChoiceDirection? stickHeldDirection;

        private void Update()
        {
            if (!Enabled) return;

            if (TryReadKeyboard(out var direction) || TryReadGamepad(out direction))
                DirectionPressed?.Invoke(direction);
        }

        private static bool TryReadKeyboard(out ChoiceDirection direction)
        {
            direction = default;
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                direction = ChoiceDirection.Up;
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                direction = ChoiceDirection.Right;
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                direction = ChoiceDirection.Down;
            else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                direction = ChoiceDirection.Left;
            else
                return false;

            return true;
        }

        private bool TryReadGamepad(out ChoiceDirection direction)
        {
            direction = default;
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                stickHeldDirection = null;
                return false;
            }

            if (gamepad.dpad.up.wasPressedThisFrame) { direction = ChoiceDirection.Up; return true; }
            if (gamepad.dpad.right.wasPressedThisFrame) { direction = ChoiceDirection.Right; return true; }
            if (gamepad.dpad.down.wasPressedThisFrame) { direction = ChoiceDirection.Down; return true; }
            if (gamepad.dpad.left.wasPressedThisFrame) { direction = ChoiceDirection.Left; return true; }

            // Стик: срабатывает один раз при пересечении порога,
            // повторно — после возврата в мёртвую зону.
            var stick = gamepad.leftStick.ReadValue();
            ChoiceDirection? current = null;
            if (stick.magnitude >= stickDeadzone)
                current = Mathf.Abs(stick.x) >= Mathf.Abs(stick.y)
                    ? (stick.x > 0f ? ChoiceDirection.Right : ChoiceDirection.Left)
                    : (stick.y > 0f ? ChoiceDirection.Up : ChoiceDirection.Down);

            bool fresh = current.HasValue && current != stickHeldDirection;
            stickHeldDirection = current;

            if (!fresh) return false;
            direction = current.Value;
            return true;
        }
    }
}
