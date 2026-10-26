using UnityEditor;
using UnityEngine;

/// <summary>
/// Finds the single asset of a type, so the Editor tools keep working when an asset folder is moved
/// (they used to hard-code "Assets/Settings/Configs/GameConfig.asset" and broke when it moved to Assets/Data/Configs).
/// </summary>
public static class EditorAssetFinder
{
    public static T FindSingle<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (guids.Length > 1) Debug.LogWarning($"[Editor] {guids.Length} assets of type {typeof(T).Name}; using {path}");
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
