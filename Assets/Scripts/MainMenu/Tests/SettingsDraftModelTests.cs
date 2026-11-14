using System.Collections.Generic;
using NUnit.Framework;

namespace VisualNovel.MainMenu.Tests
{
    /// <summary>
    /// EditMode-тесты черновика настроек (Unity Test Runner → EditMode).
    /// </summary>
    public sealed class SettingsDraftModelTests
    {
        private SettingsDraftModel model;
        private List<(string, int)> changes;
        private List<bool> dirtyFlags;

        [SetUp]
        public void SetUp()
        {
            model = new SettingsDraftModel();
            changes = new List<(string, int)>();
            dirtyFlags = new List<bool>();
            model.ValueChanged += (key, value) => changes.Add((key, value));
            model.DirtyChanged += dirty => dirtyFlags.Add(dirty);
            model.Register("language", 0);
            model.Register("subtitles", 1);
        }

        [Test]
        public void StartsClean_WithRegisteredDefaults()
        {
            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual(0, model.GetValue("language"));
            Assert.AreEqual(1, model.GetValue("subtitles"));
        }

        [Test]
        public void SetValue_MarksDirty_AndFiresEvents()
        {
            model.SetValue("language", 1);

            Assert.IsTrue(model.IsDirty);
            Assert.AreEqual(1, model.GetValue("language"));
            Assert.AreEqual(new[] { ("language", 1) }, changes.ToArray());
            Assert.AreEqual(new[] { true }, dirtyFlags.ToArray());
        }

        [Test]
        public void SetValue_SameValue_IsIgnored()
        {
            model.SetValue("language", 0);

            Assert.IsFalse(model.IsDirty);
            Assert.IsEmpty(changes);
        }

        [Test]
        public void SetValue_BackToDefault_ClearsDirty()
        {
            model.SetValue("language", 1);
            model.SetValue("language", 0);

            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual(new[] { true, false }, dirtyFlags.ToArray());
        }

        [Test]
        public void ResetToDefaults_RestoresAllChangedValues()
        {
            model.SetValue("language", 2);
            model.SetValue("subtitles", 0);

            model.ResetToDefaults();

            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual(0, model.GetValue("language"));
            Assert.AreEqual(1, model.GetValue("subtitles"));
            Assert.AreEqual(
                new[] { ("language", 2), ("subtitles", 0), ("language", 0), ("subtitles", 1) },
                changes.ToArray());
        }

        [Test]
        public void ResetToDefaults_WhenClean_DoesNothing()
        {
            model.ResetToDefaults();

            Assert.IsEmpty(changes);
            Assert.IsEmpty(dirtyFlags);
        }

        [Test]
        public void SetValue_UnknownKey_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => model.SetValue("unknown", 1));
        }
    }
}
