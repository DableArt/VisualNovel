using System;
using System.Collections;
using UnityEngine;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Актёр сцены: проигрывает анимированную сценку по id и по окончании
    /// возвращается в Idle, где персонаж стоит и ждёт.
    /// Анимаций пока нет: без Animator или с пустым id сразу сообщает об окончании.
    /// </summary>
    public sealed class CharacterActorView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string idleStateName = "Idle";
        [Tooltip("Страховка от зависания, если состояние сценки не найдено или зациклено")]
        [Min(1f)] [SerializeField] private float maxSceneSeconds = 60f;

        /// <summary>Идёт ли сейчас анимированная сценка.</summary>
        public bool IsPlayingScene { get; private set; }

        /// <summary>Сценка доиграла, персонаж вернулся в Idle.</summary>
        public event Action SceneFinished;

        /// <summary>Проиграть сценку по имени состояния в Animator.</summary>
        public void PlayScene(string sceneAnimationId)
        {
            if (animator == null || string.IsNullOrEmpty(sceneAnimationId))
            {
                SceneFinished?.Invoke();
                return;
            }

            StopAllCoroutines();
            StartCoroutine(PlayThenIdle(sceneAnimationId));
        }

        /// <summary>Мгновенно вернуться в Idle (например, при скипе).</summary>
        public void SnapToIdle()
        {
            StopAllCoroutines();
            IsPlayingScene = false;
            if (animator != null) animator.CrossFadeInFixedTime(idleStateName, 0.1f, 0);
        }

        private IEnumerator PlayThenIdle(string stateName)
        {
            IsPlayingScene = true;
            int stateHash = Animator.StringToHash(stateName);
            animator.CrossFadeInFixedTime(stateName, 0.1f, 0);

            // Даём Animator кадр на переход, затем ждём конца состояния.
            // Сценки не должны лупиться — иначе ждём до maxSceneSeconds.
            yield return null;

            float waited = 0f;
            while (waited < maxSceneSeconds)
            {
                if (!animator.IsInTransition(0))
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    if (info.shortNameHash == stateHash && info.normalizedTime >= 1f)
                        break;
                }

                waited += Time.deltaTime;
                yield return null;
            }

            animator.CrossFadeInFixedTime(idleStateName, 0.25f, 0);
            IsPlayingScene = false;
            SceneFinished?.Invoke();
        }
    }
}
