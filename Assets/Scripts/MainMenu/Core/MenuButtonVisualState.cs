namespace VisualNovel.MainMenu
{
    /// <summary>
    /// Визуальные состояния кнопки меню.
    /// NotActive — обычная кнопка без фона.
    /// Hovered — под курсором / выбрана навигацией: белый квадратный фон.
    /// Pressed — нажата: красный текст.
    /// </summary>
    public enum MenuButtonVisualState
    {
        NotActive = 0,
        Hovered = 1,
        Pressed = 2
    }
}
