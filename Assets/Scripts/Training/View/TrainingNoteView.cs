using UnityEngine;
using UnityEngine.UI;

namespace VisualNovel.Training
{
    /// <summary>
    /// Визуал одной стрелки-ноты: летит к зоне удара и исчезает.
    /// Спрайты стрелок назначаются в инспекторе (4 направления).
    /// </summary>
    public sealed class TrainingNoteView : MonoBehaviour
    {
        [SerializeField] private Image arrowImage;
        [SerializeField] private Sprite leftSprite;
        [SerializeField] private Sprite downSprite;
        [SerializeField] private Sprite upSprite;
        [SerializeField] private Sprite rightSprite;

        private RectTransform rect;
        private Vector2 spawnPosition;
        private Vector2 hitPosition;
        private float travelTime;
        private float spawnTime;

        private void Awake()
        {
            // Поле можно не заполнять в инспекторе — берём Image со своего объекта.
            if (arrowImage == null)
                arrowImage = GetComponent<Image>();
        }

        public void Init(TrainingNote note, Vector2 spawnPosition, Vector2 hitPosition,
            float travelTime, float spawnTime)
        {
            this.spawnPosition = spawnPosition;
            this.hitPosition = hitPosition;
            this.travelTime = travelTime;
            this.spawnTime = spawnTime;

            rect = (RectTransform)transform;
            rect.anchoredPosition = spawnPosition;

            if (arrowImage != null)
            {
                arrowImage.sprite = SpriteFor(note.Key);
                arrowImage.enabled = true;
            }
            else
            {
                Debug.LogWarning("[Training] На префабе ноты нет Image — стрелка будет невидимой.", this);
            }
        }

        /// <summary>Позиционирует ноту по времени сессии. Возвращает false, если ноту пора удалить.</summary>
        public bool UpdatePosition(float sessionTime)
        {
            float t = Mathf.InverseLerp(spawnTime, spawnTime + travelTime, sessionTime);
            if (t > 1.3f) return false; // улетела за зону удара

            rect.anchoredPosition = Vector2.LerpUnclamped(spawnPosition, hitPosition, t);
            return true;
        }

        public void PlayHitFlash() => Destroy(gameObject);

        public void PlayMissFade() => Destroy(gameObject);

        private Sprite SpriteFor(UnityEngine.InputSystem.Key key) => key switch
        {
            UnityEngine.InputSystem.Key.LeftArrow => leftSprite,
            UnityEngine.InputSystem.Key.DownArrow => downSprite,
            UnityEngine.InputSystem.Key.UpArrow => upSprite,
            UnityEngine.InputSystem.Key.RightArrow => rightSprite,
            _ => null
        };
    }
}
