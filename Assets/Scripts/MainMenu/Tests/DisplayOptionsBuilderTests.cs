using System.Linq;
using NUnit.Framework;

namespace VisualNovel.MainMenu.Tests
{
    /// <summary>
    /// EditMode-тесты построителя вариантов настроек дисплея (Unity Test Runner → EditMode).
    /// </summary>
    public sealed class DisplayOptionsBuilderTests
    {
        [Test]
        public void BuildMonitorNames_Numbered()
        {
            Assert.AreEqual(new[] { "Display 1", "Display 2", "Display 3" },
                DisplayOptionsBuilder.BuildMonitorNames(3));
        }

        [Test]
        public void BuildMonitorNames_ZeroOrNegative_GivesSingleDisplay()
        {
            Assert.AreEqual(new[] { "Display 1" }, DisplayOptionsBuilder.BuildMonitorNames(0));
            Assert.AreEqual(new[] { "Display 1" }, DisplayOptionsBuilder.BuildMonitorNames(-2));
        }

        [Test]
        public void DeduplicateResolutions_RemovesDuplicates_KeepsOrder()
        {
            var raw = new[] { (1920, 1080), (1920, 1080), (1280, 720), (1280, 720), (800, 600) };

            var result = DisplayOptionsBuilder.DeduplicateResolutions(raw);

            Assert.AreEqual(new[] { "1920x1080", "1280x720", "800x600" }, result.ToArray());
        }

        [Test]
        public void BuildFpsCaps_StandardMonitor60()
        {
            var caps = DisplayOptionsBuilder.BuildFpsCaps(60);

            Assert.AreEqual(new[] { 30, 60, DisplayOptionsBuilder.UnlimitedFps }, caps.ToArray());
        }

        [Test]
        public void BuildFpsCaps_HighRefreshMonitor144()
        {
            var caps = DisplayOptionsBuilder.BuildFpsCaps(144);

            Assert.Contains(60, caps);
            Assert.Contains(120, caps);
            Assert.Contains(144, caps);
            Assert.IsFalse(caps.Contains(165));
            Assert.AreEqual(DisplayOptionsBuilder.UnlimitedFps, caps.Last());
        }

        [Test]
        public void BuildFpsCaps_NonStandardRefresh_Included()
        {
            var caps = DisplayOptionsBuilder.BuildFpsCaps(59.94);

            Assert.Contains(60, caps); // 59.94 округляется до 60
            Assert.AreEqual(DisplayOptionsBuilder.UnlimitedFps, caps.Last());
        }

        [Test]
        public void BuildFpsCaps_InvalidRefresh_FallsBackTo60()
        {
            var caps = DisplayOptionsBuilder.BuildFpsCaps(0);

            Assert.AreEqual(new[] { 30, 60, DisplayOptionsBuilder.UnlimitedFps }, caps.ToArray());
        }

        [Test]
        public void FormatFpsCap_Formats()
        {
            Assert.AreEqual("60 FPS", DisplayOptionsBuilder.FormatFpsCap(60));
            Assert.AreEqual("Без ограничения", DisplayOptionsBuilder.FormatFpsCap(DisplayOptionsBuilder.UnlimitedFps));
        }
    }
}
