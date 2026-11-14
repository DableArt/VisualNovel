using System.Collections.Generic;
using NUnit.Framework;

namespace VisualNovel.MainMenu.Tests
{
    /// <summary>
    /// EditMode-тесты автомата состояний кнопки меню (Unity Test Runner → EditMode).
    /// </summary>
    public sealed class MenuButtonModelTests
    {
        private MenuButtonModel model;
        private List<MenuButtonVisualState> states;

        [SetUp]
        public void SetUp()
        {
            model = new MenuButtonModel();
            states = new List<MenuButtonVisualState>();
            model.StateChanged += s => states.Add(s);
        }

        [Test]
        public void StartsNotActive()
        {
            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
            Assert.IsTrue(model.IsEnabled);
        }

        [Test]
        public void PointerEnterExit_TogglesHover()
        {
            model.PointerEnter();
            Assert.AreEqual(MenuButtonVisualState.Hovered, model.CurrentState);

            model.PointerExit();
            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);

            Assert.AreEqual(
                new[] { MenuButtonVisualState.Hovered, MenuButtonVisualState.NotActive },
                states.ToArray());
        }

        [Test]
        public void PressCycle_Hover_Pressed_BackToHover()
        {
            model.PointerEnter();
            model.PointerDown();
            Assert.AreEqual(MenuButtonVisualState.Pressed, model.CurrentState);

            model.PointerUp(); // указатель ещё над кнопкой
            Assert.AreEqual(MenuButtonVisualState.Hovered, model.CurrentState);
        }

        [Test]
        public void PointerUpOutside_ReturnsToNotActive()
        {
            model.PointerEnter();
            model.PointerDown();
            model.PointerExit(); // увели курсор при зажатой кнопке
            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);

            model.PointerUp();
            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
        }

        [Test]
        public void PointerDownOutside_IsIgnored()
        {
            model.PointerDown();
            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
            Assert.IsEmpty(states);
        }

        [Test]
        public void Disabled_IgnoresInput_AndResetsState()
        {
            model.PointerEnter();
            model.SetEnabled(false);

            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
            Assert.IsFalse(model.IsPointerInside);

            model.PointerEnter();
            model.PointerDown();
            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);

            model.SetEnabled(true);
            model.PointerEnter();
            Assert.AreEqual(MenuButtonVisualState.Hovered, model.CurrentState);
        }

        [Test]
        public void PointerEnterWhilePressed_KeepsPressed()
        {
            model.PointerEnter();
            model.PointerDown();
            model.PointerEnter(); // повторный enter (например смена фокуса навигации)

            Assert.AreEqual(MenuButtonVisualState.Pressed, model.CurrentState);
        }

        [Test]
        public void ForceReset_ClearsHoveredState()
        {
            model.PointerEnter();

            model.ForceReset();

            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
            Assert.IsFalse(model.IsPointerInside);
            Assert.AreEqual(
                new[] { MenuButtonVisualState.Hovered, MenuButtonVisualState.NotActive },
                states.ToArray());
        }

        [Test]
        public void ForceReset_ClearsPressedState()
        {
            model.PointerEnter();
            model.PointerDown();

            model.ForceReset();

            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
            Assert.IsFalse(model.IsPointerInside);
        }

        [Test]
        public void ForceReset_WhenNotActive_NoEvent()
        {
            model.ForceReset();

            Assert.AreEqual(MenuButtonVisualState.NotActive, model.CurrentState);
            Assert.IsEmpty(states);
        }

        [Test]
        public void WorksAgainAfterForceReset()
        {
            // Сценарий: панель скрыли в момент hover → сброс → показали снова.
            model.PointerEnter();
            model.ForceReset();

            model.PointerEnter();
            Assert.AreEqual(MenuButtonVisualState.Hovered, model.CurrentState);

            model.PointerDown();
            Assert.AreEqual(MenuButtonVisualState.Pressed, model.CurrentState);
        }
    }
}
