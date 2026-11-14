using System;
using System.Collections.Generic;

namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Строит списки вариантов для настроек дисплея из «сырых» данных системы
    /// (мониторы, разрешения, частота обновления). Чистый C# — тестируется без Unity;
    /// данные ему поставляет вью из UnityEngine.Screen/Display.
    /// </summary>
    public static class DisplayOptionsBuilder
    {
        /// <summary>Маркер «без ограничения» в списке лимитов кадров.</summary>
        public const int UnlimitedFps = -1;

        /// <summary>Имена мониторов: "Display 1".. "Display N". Пустой ввод — один монитор.</summary>
        public static string[] BuildMonitorNames(int displayCount)
        {
            int count = Math.Max(1, displayCount);
            var names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = $"Display {i + 1}";
            return names;
        }

        /// <summary>
        /// Убирает дубли разрешений (одинаковые W×H с разной частотой) и форматирует "1920x1080".
        /// Порядок сохраняется.
        /// </summary>
        public static List<string> DeduplicateResolutions(IEnumerable<(int width, int height)> raw)
        {
            var seen = new HashSet<(int, int)>();
            var result = new List<string>();
            foreach (var (width, height) in raw)
            {
                if (!seen.Add((width, height))) continue;
                result.Add($"{width}x{height}");
            }
            return result;
        }

        /// <summary>
        /// Лимиты кадров, доступные на мониторе: стандартные значения не выше
        /// частоты обновления + сама частота + «без ограничения» (UnlimitedFps).
        /// </summary>
        public static List<int> BuildFpsCaps(double refreshRateHz)
        {
            int refresh = (int)Math.Round(refreshRateHz);
            if (refresh <= 0) refresh = 60;

            var caps = new SortedSet<int> { refresh };
            foreach (int standard in new[] { 30, 60, 90, 120, 144, 165, 240 })
                if (standard <= refresh) caps.Add(standard);

            var result = new List<int>(caps) { UnlimitedFps };
            return result;
        }

        /// <summary>Подпись варианта лимита: "60 FPS" / "Без ограничения".</summary>
        public static string FormatFpsCap(int cap) => cap == UnlimitedFps ? "Без ограничения" : $"{cap} FPS";
    }
}
