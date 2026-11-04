using System.Collections;
using UnityEngine;
using TMPro;

namespace VisualNovel.Training
{
    /// <summary>
    /// Обратная связь для игрока: фразы («Так держать!»), анимации персонажа,
    /// звуки попадания/промаха. Триггеры аниматора настраиваются в инспекторе,
    /// поэтому разные тренировки могут иметь свои анимации на одном ядре.
    /// </summary>
    public sealed class TrainingFeedbackView : MonoBehaviour
    {
        [Header("Текст обратной связи")]
        [SerializeField] private TMP_Text phraseText;
        [SerializeField] private float phraseShowTime = 0.8f;

        [Header("Персонаж")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private string successTrigger = "TrainingSuccess";
        [SerializeField] private string missTrigger = "TrainingMiss";

        [Header("Звуки")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip missClip;

        private Coroutine phraseRoutine;

        private void Awake()
        {
            if (phraseText == null)
                phraseText = GetComponentInChildren<TMP_Text>(true);

            if (phraseText != null)
                phraseText.enabled = false;
        }

        public void ShowHit(string phrase)
        {
            ShowPhrase(phrase);
            if (characterAnimator != null)
                characterAnimator.SetTrigger(successTrigger);
            PlayClip(hitClip);
        }

        public void ShowMiss(string phrase)
        {
            ShowPhrase(phrase);
            if (characterAnimator != null)
                characterAnimator.SetTrigger(missTrigger);
            PlayClip(missClip);
        }

        public void ShowPhrase(string phrase)
        {
            if (phraseText == null)
            {
                Debug.LogWarning("[Training] В TrainingFeedbackView не назначен Phrase Text (TextMeshPro).", this);
                return;
            }

            if (phraseRoutine != null) StopCoroutine(phraseRoutine);
            phraseRoutine = StartCoroutine(ShowPhraseRoutine(phrase));
        }

        private IEnumerator ShowPhraseRoutine(string phrase)
        {
            phraseText.text = phrase;
            phraseText.enabled = true;
            yield return new WaitForSeconds(phraseShowTime);
            phraseText.enabled = false;
            phraseRoutine = null;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
