namespace VisualNovel.Training
{
    /// <summary>Результат нажатия по ноте.</summary>
    public enum TrainingHitResult
    {
        /// <summary>Идеальное попадание — полная награда.</summary>
        Perfect,

        /// <summary>Хорошее попадание — половина награды.</summary>
        Good,

        /// <summary>Промах — не та клавиша или время вышло.</summary>
        Miss
    }
}
