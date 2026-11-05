using System;
using System.Collections.Generic;

namespace VisualNovel.Novel
{
    /// <summary>
    /// Граф диалога: список узлов + стартовый узел. Источник данных для сессии.
    /// </summary>
    [Serializable]
    public sealed class DialogueGraph
    {
        /// <summary>Id узла, с которого начинается сцена.</summary>
        public string startNodeId = string.Empty;

        /// <summary>Все узлы сцены.</summary>
        public List<DialogueNode> nodes = new();

        private Dictionary<string, DialogueNode> lookup;

        public bool TryGetNode(string id, out DialogueNode node)
        {
            EnsureLookup();
            return lookup.TryGetValue(id ?? string.Empty, out node);
        }

        private void EnsureLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, DialogueNode>();
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                lookup[node.id] = node;
            }
        }
    }
}
