using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Utils
{
    public static void SetExistingGameViewSize(int width, int height)
    {
#if UNITY_EDITOR
        var asm = typeof(EditorWindow).Assembly;

        // Access GameViewSizes singleton
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var singletonType = asm.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        var instanceProp = singletonType.GetProperty("instance");
        var gameViewSizesInstance = instanceProp.GetValue(null, null);

        // Get current group (aspect ratios for Standalone, iOS, Android, etc.)
        var currentGroupProp = sizesType.GetProperty("currentGroup");
        var group = currentGroupProp.GetValue(gameViewSizesInstance, null);

        // Methods
        var getTotalCount = group.GetType().GetMethod("GetTotalCount");
        var getGameViewSize = group.GetType().GetMethod("GetGameViewSize");

        int totalCount = (int)getTotalCount.Invoke(group, null);

        int foundIndex = -1;

        for (int i = 0; i < totalCount; i++)
        {
            var sizeObj = getGameViewSize.Invoke(group, new object[] { i });
            var widthProp = sizeObj.GetType().GetProperty("width");
            var heightProp = sizeObj.GetType().GetProperty("height");

            int w = (int)widthProp.GetValue(sizeObj);
            int h = (int)heightProp.GetValue(sizeObj);

            if (w == width && h == height)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex == -1)
        {
            Debug.LogWarning($"? No existing GameView size found for {width}x{height}");
            return;
        }

        // Switch GameView to that index
        var gvType = asm.GetType("UnityEditor.GameView");
        var gameViewWindow = EditorWindow.GetWindow(gvType);
        var sizeSelectionCallback = gvType.GetMethod("SizeSelectionCallback",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        sizeSelectionCallback.Invoke(gameViewWindow, new object[] { foundIndex, null });

        Debug.Log($"? Switched GameView to existing size {width}x{height} (index {foundIndex})");
#endif
    }
    public static IList<T> ShuffleList<T>(this IList<T> list)
    {
        if (list == null || list.Count <= 1)
        {
            return list;
        }

        System.Random rng = new System.Random();
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }

        return list;
    }

}
