using System;
using Newtonsoft.Json;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Загружает граф диалога из JSON-строки (Newtonsoft.Json уже стоит в проекте).
    /// Формат — см. Assets/Resources/Novel/DemoDialogue.json.
    /// </summary>
    public sealed class JsonDialogueProvider : IDialogueProvider
    {
        private readonly string json;

        public JsonDialogueProvider(string json)
        {
            this.json = !string.IsNullOrWhiteSpace(json)
                ? json
                : throw new ArgumentNullException(nameof(json));
        }

        public DialogueGraph Load()
        {
            var graph = JsonConvert.DeserializeObject<DialogueGraph>(json);
            if (graph == null || graph.nodes == null || graph.nodes.Count == 0)
                throw new InvalidOperationException("[Novel] JSON диалога пуст или не содержит узлов.");

            return graph;
        }
    }
}
