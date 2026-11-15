using System.Collections;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Короткие ease-out анимации появления UI (fade + сдвиг) без внешних твинеров.
    /// CanvasGroup добавляется автоматически, если его нет.
    /// </summary>
    public static class FadeMotion
    {
        /// <summary>
        /// Плавное появление: alpha 0→1 и сдвиг от home+startOffset к home.
        /// FadeInDown — startOffset вверх (окно реплики), FadeInUp — вниз (колесо выбора).
        /// </summary>
        public static IEnumerator Play(RectTransform target, Vector2 home, Vector2 startOffset, float duration)
        {
            if (target == null) yield break;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();

            if (duration <= 0f)
            {
                group.alpha = 1f;
                target.anchoredPosition = home;
                yield break;
            }

            group.alpha = 0f;
            target.anchoredPosition = home + startOffset;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = EaseOutCubic(elapsed / duration);
                group.alpha = t;
                target.anchoredPosition = Vector2.LerpUnclamped(home + startOffset, home, t);
                yield return null;
            }

            group.alpha = 1f;
            target.anchoredPosition = home;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
    }
}
