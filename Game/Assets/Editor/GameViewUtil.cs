using System;
using System.Reflection;
using UnityEditor;

// Sets the Game view to a fixed resolution (the Editor has no public API for this).
public static class GameViewUtil
{
    [MenuItem("Tools/Game View Portrait 1080x1920")]
    public static string SetPortrait()
    {
        return SetResolution(1080, 1920, "Portrait 1080x1920");
    }

    public static string SetResolution(int width, int height, string label)
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singletonType.GetProperty("instance").GetValue(null);
        object groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
        Type groupClass = group.GetType();

        var displayTexts = (string[])groupClass.GetMethod("GetDisplayTexts").Invoke(group, null);
        int index = -1;
        for (int i = 0; i < displayTexts.Length; i++)
        {
            if (displayTexts[i].Contains(label))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
            Type sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
            ConstructorInfo constructor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
            object size = constructor.Invoke(new[] { Enum.Parse(sizeKind, "FixedResolution"), width, height, label });
            groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null) - 1;
        }

        Type gameViewType = assembly.GetType("UnityEditor.GameView");
        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SetValue(gameView, index);
        gameView.Repaint();
        return label + " index=" + index;
    }
}
