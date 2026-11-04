using UnityEngine.InputSystem;

namespace VisualNovel.Training
{
    /// <summary>
    /// Одна «нота» тренировки: какую клавишу и когда нужно нажать.
    /// </summary>
    public sealed class TrainingNote
    {
        /// <summary>Клавиша, которую нужно нажать (Input System).</summary>
        public Key Key { get; }

        /// <summary>Момент времени (секунды от старта сессии), в который нота доезжает до зоны удара.</summary>
        public float HitTime { get; }

        /// <summary>Нота уже разыграна (попадание или промах).</summary>
        public bool IsResolved { get; private set; }

        /// <summary>Нота уже была показана игроку.</summary>
        public bool IsVisible { get; private set; }

        public TrainingNote(Key key, float hitTime)
        {
            Key = key;
            HitTime = hitTime;
        }

        public void MarkVisible() => IsVisible = true;

        public void Resolve() => IsResolved = true;
    }
}
