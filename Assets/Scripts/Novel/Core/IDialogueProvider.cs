namespace VisualNovel.Novel
{
    /// <summary>
    /// Источник диалогов (паттерн Strategy). Сейчас — JSON,
    /// позже сюда же встанет адаптер к Ink/Fungus, когда появится сюжет.
    /// </summary>
    public interface IDialogueProvider
    {
        DialogueGraph Load();
    }
}
