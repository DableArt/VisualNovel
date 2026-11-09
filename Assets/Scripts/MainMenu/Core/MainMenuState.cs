namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Состояния главного меню.
    /// Intro — играет фоновая анимация / анимация появления кнопок, ввод заблокирован.
    /// Idle — меню интерактивно.
    /// Leaving — выбран переход (в игру, выход), играет анимация ухода, ввод заблокирован.
    /// </summary>
    public enum MainMenuState
    {
        Intro = 0,
        Idle = 1,
        Leaving = 2
    }
}
