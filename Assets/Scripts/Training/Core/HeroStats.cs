using System;
using UnityEngine;

namespace VisualNovel.Training
{
    /// <summary>
    /// Характеристики героя. Компонент на объекте героя;
    /// в будущем можно заменить на ScriptableObject-профиль или сохранение.
    /// </summary>
    public sealed class HeroStats : MonoBehaviour
    {
        public event Action<TrainingStat, int> StatChanged;

        [SerializeField] private int strength;
        [SerializeField] private int agility;
        [SerializeField] private int intelligence;

        public int Strength => strength;
        public int Agility => agility;
        public int Intelligence => intelligence;

        public void Add(TrainingStat stat, int amount)
        {
            switch (stat)
            {
                case TrainingStat.Strength: strength += amount; break;
                case TrainingStat.Agility: agility += amount; break;
                case TrainingStat.Intelligence: intelligence += amount; break;
                default: throw new ArgumentOutOfRangeException(nameof(stat));
            }

            StatChanged?.Invoke(stat, Get(stat));
        }

        public int Get(TrainingStat stat) => stat switch
        {
            TrainingStat.Strength => strength,
            TrainingStat.Agility => agility,
            TrainingStat.Intelligence => intelligence,
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };
    }
}
