using System.Collections.Generic;
using NUnit.Framework;

namespace VisualNovel.MainMenu.Tests
{
    /// <summary>
    /// EditMode-тесты ядра главного меню (Unity Test Runner → EditMode).
    /// Ядро — чистый C#, сцена не нужна.
    /// </summary>
    public sealed class MainMenuCoreTests
    {
        private MainMenuCore core;
        private List<MainMenuState> states;
        private List<MainMenuCommand> commands;

        [SetUp]
        public void SetUp()
        {
            core = new MainMenuCore();
            states = new List<MainMenuState>();
            commands = new List<MainMenuCommand>();
            core.StateChanged += s => states.Add(s);
            core.CommandInvoked += c => commands.Add(c);
        }

        [Test]
        public void StartsInIntro_BlocksCommands()
        {
            Assert.AreEqual(MainMenuState.Intro, core.CurrentState);
            Assert.IsFalse(core.InvokeCommand(MainMenuCommand.NewGame));
            Assert.IsEmpty(commands);
        }

        [Test]
        public void CompleteIntro_SwitchesToIdle_WithEvent()
        {
            core.CompleteIntro();

            Assert.AreEqual(MainMenuState.Idle, core.CurrentState);
            Assert.AreEqual(new[] { MainMenuState.Idle }, states.ToArray());

            core.CompleteIntro(); // повторный вызов игнорируется
            Assert.AreEqual(1, states.Count);
        }

        [Test]
        public void ContinueAndLoad_BlockedWithoutSave()
        {
            core.CompleteIntro();

            Assert.IsFalse(core.CanExecute(MainMenuCommand.Continue));
            Assert.IsFalse(core.CanExecute(MainMenuCommand.LoadGame));
            Assert.IsFalse(core.InvokeCommand(MainMenuCommand.Continue));
            Assert.IsEmpty(commands);
        }

        [Test]
        public void ContinueAndLoad_AllowedWithSave_RaiseAvailabilityEvent()
        {
            bool? availability = null;
            core.SaveAvailabilityChanged += v => availability = v;
            core.CompleteIntro();

            core.HasSaveGame = true;

            Assert.AreEqual(true, availability);
            Assert.IsTrue(core.CanExecute(MainMenuCommand.Continue));
            Assert.IsTrue(core.CanExecute(MainMenuCommand.LoadGame));
        }

        [Test]
        public void TransitionCommands_LeadToLeaving_AndBlockFurtherInput()
        {
            core.HasSaveGame = true;
            core.CompleteIntro();

            Assert.IsTrue(core.InvokeCommand(MainMenuCommand.NewGame));

            Assert.AreEqual(new[] { MainMenuCommand.NewGame }, commands.ToArray());
            Assert.AreEqual(MainMenuState.Leaving, core.CurrentState);
            Assert.IsFalse(core.InvokeCommand(MainMenuCommand.Settings));
        }

        [Test]
        public void SubmenuCommands_KeepMenuIdle()
        {
            core.CompleteIntro();

            Assert.IsTrue(core.InvokeCommand(MainMenuCommand.Settings));
            Assert.AreEqual(MainMenuState.Idle, core.CurrentState);

            Assert.IsTrue(core.InvokeCommand(MainMenuCommand.AdditionalMaterials));
            Assert.AreEqual(MainMenuState.Idle, core.CurrentState);
            Assert.AreEqual(2, commands.Count);
        }

        [Test]
        public void ReturnToMenu_RestartsIntro()
        {
            core.CompleteIntro();
            core.InvokeCommand(MainMenuCommand.Exit);
            Assert.AreEqual(MainMenuState.Leaving, core.CurrentState);

            core.ReturnToMenu();

            Assert.AreEqual(MainMenuState.Intro, core.CurrentState);
            Assert.AreEqual(
                new[] { MainMenuState.Idle, MainMenuState.Leaving, MainMenuState.Intro },
                states.ToArray());
        }
    }
}
