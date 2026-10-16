using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor のテスト広告が、Packages 内の見本プレハブを変更済みにする。
// そのプレハブは保存できないので、保存対象から外す。
[InitializeOnLoad]
public static class AdMobPlaceholderSaveGuard
{
    const string PackageRoot = "Packages/com.google.ads.mobile";
    const string PlaceholderFolder = "PlaceholderAds";

    static AdMobPlaceholderSaveGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.delayCall += ClearPlaceholderDirt;
    }

    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += ClearPlaceholderDirt;
    }

    public static void ClearPlaceholderDirt()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PackageRoot });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.IndexOf(PlaceholderFolder, StringComparison.Ordinal) < 0)
                continue;
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
                EditorUtility.ClearDirty(asset);
        }
    }
}

public sealed class AdMobPlaceholderSaveProcessor : AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        AdMobPlaceholderSaveGuard.ClearPlaceholderDirt();
        var kept = new List<string>(paths.Length);
        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i].Replace('\\', '/');
            if (path.IndexOf("Packages/com.google.ads.mobile/", StringComparison.Ordinal) >= 0
                && path.IndexOf("PlaceholderAds", StringComparison.Ordinal) >= 0)
                continue;
            kept.Add(paths[i]);
        }
        return kept.ToArray();
    }
}
