using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// «Колесо» вариантов ответа: 4 кнопки по сторонам света (Up/Right/Down/Left)
    /// + подписи бонусов и % срабатывания при наведении (по макету).
    /// Появляется снизу вверх (FadeInUp); после появления кнопки становятся доступны.
    /// Порядок вариантов узла = <see cref="ChoiceDirection"/>.
    /// </summary>
    public sealed class ChoiceWheelView : MonoBehaviour
    {
        /// <summary>Количество позиций колеса.</summary>
        public const int DirectionCount = 4;

        private static readonly string[] ButtonNames =
            { "ButtonAnswerUP", "ButtonAnswerRight", "ButtonAnswerDown", "ButtonAnswerLeft" };
        private static readonly string[] BonusNames =
            { "LabelUP", "LabelRight", "LabelDown", "LabelLeft" };

        [Header("Кнопки (пусто — поиск по имени среди детей этого объекта)")]
        [SerializeField] private ChoiceButtonView[] buttons = new ChoiceButtonView[DirectionCount];

        [Header("Контейнеры бонусов (HoverViewBonus/Label*) — пусто: авто-поиск")]
        [SerializeField] private RectTransform[] bonusRoots = new RectTransform[DirectionCount];

        [Header("Анимация появления (FadeInUp)")]
        [Min(0f)] [SerializeField] private float fadeDuration = 0.5f;
        [Min(0f)] [SerializeField] private float fadeOffset = 80f;

        private readonly TMP_Text[] bonusLabels = new TMP_Text[DirectionCount];
        private readonly TMP_Text[] percentLabels = new TMP_Text[DirectionCount];
        private readonly Vector2[] homePositions = new Vector2[DirectionCount];

        private NovelSession session;
        private IReadOnlyList<ChoiceOption> options;
        private ChoiceDirection? hoveredDirection;

        /// <summary>Кнопки доступны игроку (колесо полностью появилось).</summary>
        public bool InputEnabled { get; private set; }

        private void Awake()
        {
            AutoWire();

            for (int i = 0; i < DirectionCount; i++)
            {
                int direction = i; // захват для замыкания
                var button = buttons[i];
                if (button == null) continue;

                homePositions[i] = ((RectTransform)button.transform).anchoredPosition;
                button.Hovered += () => OnButtonHovered(direction);
                button.Unhovered += () => OnButtonUnhovered(direction);
                button.Button.onClick.AddListener(() => Select((ChoiceDirection)direction));
                button.SetInteractable(false);
                button.gameObject.SetActive(false);
            }

            foreach (var root in bonusRoots)
                if (root != null) root.gameObject.SetActive(false);
        }

        /// <summary>Подключает колесо к ядру. Вызывается из NovelSessionRunner.</summary>
        public void Bind(NovelSession novelSession) => session = novelSession;

        /// <summary>Раскладывает варианты по кнопкам без анимации (до FadeInUp).</summary>
        public void Prepare(IReadOnlyList<ChoiceOption> presented)
        {
            options = presented;

            for (int i = 0; i < DirectionCount; i++)
            {
                var button = buttons[i];
                if (button == null) continue;

                bool used = i < presented.Count;
                // Текст ставим ДО SetActive: иначе TMP перестраивает старый текст
                // на скрытом объекте и падает на fallback-материалах (known bug TMP).
                button.SetText(used ? presented[i].text : string.Empty);
                button.gameObject.SetActive(used);
                button.SetInteractable(false);
                HideBonus(i);
            }
        }

        /// <summary>Появление снизу вверх; по завершении кнопки доступны.</summary>
        public IEnumerator PlayFadeIn()
        {
            int running = 0;
            for (int i = 0; i < DirectionCount; i++)
            {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeSelf) continue;

                running++;
                int index = i;
                StartCoroutine(FadeButton(button, homePositions[index], () => running--));
            }

            while (running > 0) yield return null;

            for (int i = 0; i < DirectionCount; i++)
                if (buttons[i] != null && buttons[i].gameObject.activeSelf)
                    buttons[i].SetInteractable(true);

            InputEnabled = true;
        }

        /// <summary>
        /// Нажатие направления с клавиатуры/геймпада (из DialogueInputController).
        /// Первое нажатие — hover (выбор/подсветка варианта + бонусы),
        /// повторное нажатие в ту же сторону — активация.
        /// </summary>
        public void SelectFromInput(ChoiceDirection direction)
        {
            if (!InputEnabled) return;

            int index = (int)direction;
            if (options == null || index >= options.Count) return;

            var button = buttons[index];
            if (button == null || !button.gameObject.activeSelf) return;

            if (hoveredDirection == direction)
            {
                button.SimulatePress(); // повторное нажатие — активация
                return;
            }

            MoveHover(direction);
        }

        /// <summary>Переносит курсор-выбор на кнопку направления.</summary>
        private void MoveHover(ChoiceDirection direction)
        {
            if (hoveredDirection.HasValue && hoveredDirection.Value != direction)
            {
                var previous = buttons[(int)hoveredDirection.Value];
                if (previous != null) previous.ClearHover();
            }

            hoveredDirection = direction;
            buttons[(int)direction].ShowHovered();
        }

        /// <summary>Прячет колесо и подписи бонусов.</summary>
        public void Hide()
        {
            InputEnabled = false;
            options = null;
            hoveredDirection = null;

            foreach (var button in buttons)
                if (button != null)
                {
                    button.SetInteractable(false);
                    button.gameObject.SetActive(false);
                }

            foreach (var root in bonusRoots)
                if (root != null) root.gameObject.SetActive(false);
        }

        private IEnumerator FadeButton(ChoiceButtonView button, Vector2 home, Action onDone)
        {
            yield return FadeMotion.Play((RectTransform)button.transform, home,
                Vector2.down * fadeOffset, fadeDuration);
            onDone();
        }

        private void Select(ChoiceDirection direction)
        {
            if (!InputEnabled || session == null) return;

            int index = (int)direction;
            if (options == null || index >= options.Count) return;

            InputEnabled = false;
            session.SelectChoice(index);
        }

        private void OnButtonHovered(int direction)
        {
            ShowBonus(direction);

            // Курсор мыши над кнопкой — синхронизируем курсор клавиатуры/геймпада.
            var current = (ChoiceDirection)direction;
            if (hoveredDirection.HasValue && hoveredDirection.Value != current)
            {
                var previous = buttons[(int)hoveredDirection.Value];
                if (previous != null) previous.ClearHover();
            }
            hoveredDirection = current;
        }

        private void OnButtonUnhovered(int direction)
        {
            HideBonus(direction);
            if (hoveredDirection == (ChoiceDirection)direction)
                hoveredDirection = null;
        }

        private void ShowBonus(int direction)
        {
            if (options == null || direction >= options.Count) return;

            var root = bonusRoots[direction];
            if (root == null) return;

            var option = options[direction];
            bool hasChance = option.successChance >= 0f;

            // Все тексты — до активации контейнеров (защита от краша TMP на fallback-материалах).
            var bonusLabel = bonusLabels[direction];
            if (bonusLabel != null) bonusLabel.text = option.bonusText ?? string.Empty;

            var percentLabel = percentLabels[direction];
            if (percentLabel != null)
            {
                if (hasChance)
                    percentLabel.text = Mathf.RoundToInt(option.successChance * 100f) + "%";
                percentLabel.gameObject.SetActive(hasChance);
            }

            root.gameObject.SetActive(!string.IsNullOrEmpty(option.bonusText) || hasChance);
        }

        private void HideBonus(int direction)
        {
            var root = bonusRoots[direction];
            if (root != null) root.gameObject.SetActive(false);
        }

        private void AutoWire()
        {
            for (int i = 0; i < DirectionCount; i++)
            {
                if (buttons[i] == null)
                {
                    var buttonTransform = FindDeepChild(transform, ButtonNames[i]);
                    if (buttonTransform != null)
                        buttons[i] = buttonTransform.GetComponent<ChoiceButtonView>();
                }

                if (buttons[i] == null)
                    Debug.LogWarning($"[Novel] ChoiceWheelView: не найдена кнопка {ButtonNames[i]}.", this);

                if (bonusRoots[i] == null)
                    bonusRoots[i] = FindDeepChild(transform, BonusNames[i]) as RectTransform;

                if (bonusRoots[i] != null)
                {
                    foreach (var label in bonusRoots[i].GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (label.name == "LabelBonus") bonusLabels[i] = label;
                        else if (label.name == "PercentLabel") percentLabels[i] = label;
                    }
                }
            }
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }
    }
}
