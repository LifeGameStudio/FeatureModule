using DA_Assets.FCU;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class EditorScript
{
    [MenuItem("CONTEXT/Transform/RemoveSyncHelper")]
    public static void RemoveScript() { Remove<SyncHelper>(); }

    [MenuItem("CONTEXT/Transform/RemoveLayoutElement")]
    public static void RemoveLayoutElement() { Remove<LayoutElement>(); }

    [MenuItem("CONTEXT/Transform/RemoveContentSizeFilter")]
    public static void RemoveContentSizeFilter() { Remove<ContentSizeFitter>(); }

    private static void Remove<T>() where T : Component
    {
        var selected   = Selection.activeGameObject;
        var components = selected.GetComponentsInChildren<T>(true);

        foreach (var c in components)
        {
            Object.DestroyImmediate(c);
        }
    }
}