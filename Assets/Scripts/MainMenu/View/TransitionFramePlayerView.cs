using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Покадровый проигрыватель перехода: раздаёт спрайты на fullscreen-Image
    /// поверх канваса. Кадры — массив спрайтов в порядке проигрывания
    /// (PNG с Texture Type = Sprite), темп — в FPS.
    /// На кадре <see cref="switchAtFrame"/> вызывается onSwitchPoint — там
    /// переключаются панели, чтобы смена экрана попала в середину анимации.
    /// Image оверлея с Raycast Target блокирует клики, пока играет анимация.
    /// </summary>
    public sealed class TransitionFramePlayerView : MonoBehaviour
    {
        [SerializeField] private Image image;

        [Tooltip("Кадры перехода по порядку")]
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("Скорость проигрывания, кадров в секунду")]
        [SerializeField, Min(1f)] private float fps = 30f;

        [Tooltip("На этом кадре переключаются панели (обычно середина анимации)")]
        [SerializeField, Min(0)] private int switchAtFrame = 10;

        /// <summary>Есть ли кадры для проигрывания.</summary>
        public bool HasFrames => frames != null && frames.Length > 0;

        /// <summary>
        /// Играет кадры от начала до конца. onSwitchPoint вызывается один раз —
        /// на кадре переключения (или в конце, если индекс за пределами массива).
        /// </summary>
        public IEnumerator Play(Action onSwitchPoint)
        {
            gameObject.SetActive(true);

            bool switched = false;
            float frameTime = 1f / fps;

            for (int i = 0; i < frames.Length; i++)
            {
                if (image != null) image.sprite = frames[i];

                if (!switched && i >= switchAtFrame)
                {
                    switched = true;
                    onSwitchPoint?.Invoke();
                }

                yield return new WaitForSeconds(frameTime);
            }

            if (!switched) onSwitchPoint?.Invoke();

            gameObject.SetActive(false);
        }
    }
}
