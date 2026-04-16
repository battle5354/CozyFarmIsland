using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoRenameDuplicates
{
    // Unity duplicate pattern: "Object (1)"
    private static readonly Regex UnityDuplicateRegex = new Regex(
        @"^(.*?)(?:\s*\(\d+\))$",
        RegexOptions.Compiled
    );

    // Custom suffix pattern: "Object_01"
    private static readonly Regex CustomSuffixRegex = new Regex(
        @"^(.*?)(?:_(\d+))$",
        RegexOptions.Compiled
    );

    private const int NumberPadding = 2;
    private static bool renameQueued;

    static AutoRenameDuplicates()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        if (renameQueued)
            return;

        renameQueued = true;
        EditorApplication.delayCall += ProcessSelectedDuplicates;
    }

    private static void ProcessSelectedDuplicates()
    {
        renameQueued = false;

        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
            return;

        foreach (GameObject go in selectedObjects)
        {
            if (go == null)
                continue;

            // Rename only objects that Unity just duplicated,
            // because they usually look like "Name (1)"
            if (!LooksLikeUnityDuplicate(go.name))
                continue;

            string baseName = ExtractBaseName(go.name);
            int nextIndex = GetNextAvailableIndex(go, baseName);
            string newName = $"{baseName}_{nextIndex.ToString().PadLeft(NumberPadding, '0')}";

            if (go.name == newName)
                continue;

            Undo.RecordObject(go, "Auto Rename Duplicate");
            go.name = newName;
            EditorUtility.SetDirty(go);
        }
    }

    private static bool LooksLikeUnityDuplicate(string objectName)
    {
        return UnityDuplicateRegex.IsMatch(objectName);
    }

    private static string ExtractBaseName(string objectName)
    {
        Match duplicateMatch = UnityDuplicateRegex.Match(objectName);
        string rawBaseName = duplicateMatch.Success ? duplicateMatch.Groups[1].Value.Trim() : objectName.Trim();

        // If source object was already like "Crate_01", then duplicate should become "Crate_02"
        Match customMatch = CustomSuffixRegex.Match(rawBaseName);
        if (customMatch.Success)
            return customMatch.Groups[1].Value.Trim();

        return rawBaseName;
    }

    private static int GetNextAvailableIndex(GameObject targetObject, string baseName)
    {
        int maxIndex = 0;

        if (targetObject.transform.parent != null)
        {
            Transform parent = targetObject.transform.parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || child.gameObject == targetObject)
                    continue;

                int index = ExtractCustomIndex(child.name, baseName);
                if (index > maxIndex)
                    maxIndex = index;
            }
        }
        else
        {
            Scene scene = targetObject.scene;
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                if (root == null || root == targetObject)
                    continue;

                int index = ExtractCustomIndex(root.name, baseName);
                if (index > maxIndex)
                    maxIndex = index;
            }
        }

        return maxIndex + 1;
    }

    private static int ExtractCustomIndex(string objectName, string baseName)
    {
        string pattern = $"^{Regex.Escape(baseName)}_(\\d+)$";
        Match match = Regex.Match(objectName, pattern);

        if (!match.Success)
            return 0;

        if (int.TryParse(match.Groups[1].Value, out int parsedIndex))
            return parsedIndex;

        return 0;
    }
}