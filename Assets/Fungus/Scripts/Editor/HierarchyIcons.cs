// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Fungus
{
    [InitializeOnLoad]
    public class HierarchyIcons
    {
        static Texture2D TextureIcon
        {
            get { return Fungus.EditorUtils.FungusEditorResources.FungusMushroom; }
        }

        static readonly HashSet<EntityId> flowchartIDs = new HashSet<EntityId>();
        static bool initalHierarchyCheckFlag = true;

        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyIconCallback;
            EditorApplication.hierarchyChanged += HierarchyChanged;
        }

        static void HierarchyChanged()
        {
            flowchartIDs.Clear();

            if (EditorUtils.FungusEditorPreferences.hideMushroomInHierarchy)
                return;

            var flowcharts = UnityEngine.Object.FindObjectsByType<Flowchart>(FindObjectsSortMode.None);
            foreach (var flowchart in flowcharts)
                flowchartIDs.Add(flowchart.gameObject.GetEntityId());
        }

        static void HierarchyIconCallback(EntityId entityId, Rect selectionRect)
        {
            if (initalHierarchyCheckFlag)
            {
                HierarchyChanged();
                initalHierarchyCheckFlag = false;
            }

            if (EditorUtils.FungusEditorPreferences.hideMushroomInHierarchy)
                return;

            Rect r = new Rect(selectionRect);
            r.x -= 28;
            r.width = r.height;

            if (flowchartIDs.Contains(entityId))
                GUI.Label(r, TextureIcon);
        }
    }
}