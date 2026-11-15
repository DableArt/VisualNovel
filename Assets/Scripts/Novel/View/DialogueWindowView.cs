using System.Collections;
using TMPro;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Текстовое окно с репликой: имя персонажа + текст.
    /// Появляется сверху вниз (FadeInDown). Позже окно сможет позиционироваться
    /// возле рта говорящего — для этого достаточно двигать его RectTransform
    /// перед PlayFadeIn, логика появления не изменится.
    /// </summary>
    public sealed class DialogueWindowView : MonoBehaviour
    {
        [Header("Ссылки (пусто — поиск по имени среди детей)")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text textLabel;

        [Header("Анимация появления (FadeInDown)")]
        [Min(0f)] [SerializeField] private float fadeDuration = 0.4f;
        [Min(0f)] [SerializeField] private float fadeOffset = 80f;

        private RectTransform rect;
        private Vector2 homePosition;
        private bool initialized;

        private void Awake() => EnsureInitialized();

        /// <summary>Подставляет реплику узла в окно.</summary>
        public void SetLine(string speaker, string text)
        {
            EnsureInitialized();
            if (nameLabel != null) nameLabel.text = speaker ?? string.Empty;
            if (textLabel != null) textLabel.text = text ?? string.Empty;
        }

        /// <summary>Появление сверху вниз. Запускать корутиной из композитора сцены.</summary>
        public IEnumerator PlayFadeIn()
        {
            EnsureInitialized();
            gameObject.SetActive(true);
            yield return FadeMotion.Play(rect, homePosition, Vector2.up * fadeOffset, fadeDuration);
        }

        /// <summary>Мгновенно скрыть окно.</summary>
        public void HideInstant() => gameObject.SetActive(false);

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            rect = (RectTransform)transform;
            homePosition = rect.anchoredPosition;

            if (nameLabel == null) nameLabel = FindChildText("NameCharacter");
            if (textLabel == null) textLabel = FindChildText("Text (TMP)");
            if (nameLabel == null || textLabel == null)
                Debug.LogWarning("[Novel] DialogueWindowView: не найдены дочерние NameCharacter/Text (TMP).", this);
        }

        private TMP_Text FindChildText(string childName)
        {
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                if (label.name == childName) return label;
            return null;
        }
    }
}
