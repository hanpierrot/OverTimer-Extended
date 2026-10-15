using System;
using System.Collections.Generic;
using UnityEngine;

/// Loads Resources/BilliardsPresets/*.json once. Immutable after loading, so a static cache is safe
/// across scene reloads (unlike mutable run state).
public static class BilliardsPresetLibrary
{
    public const string ResourcesPath = "BilliardsPresets";
    static List<BilliardsPreset> _cache;

    public static IReadOnlyList<BilliardsPreset> All
    {
        get
        {
            if (_cache == null) Load();
            return _cache;
        }
    }

    static void Load()
    {
        _cache = new List<BilliardsPreset>();
        foreach (var asset in Resources.LoadAll<TextAsset>(ResourcesPath))
        {
            var p = Parse(asset.text, asset.name);
            if (p != null) _cache.Add(p);
        }
        _cache.Sort((a, b) => string.CompareOrdinal(a.id, b.id));   // fixed order: same seed -> same table
    }

    public static BilliardsPreset Parse(string json, string sourceName)
    {
        BilliardsPreset p;
        try { p = JsonUtility.FromJson<BilliardsPreset>(json); }
        catch (Exception e) { Debug.LogWarning($"[Billiards] {sourceName}: bad JSON ({e.Message})"); return null; }
        if (p == null || string.IsNullOrEmpty(p.id)) { Debug.LogWarning($"[Billiards] {sourceName}: missing id"); return null; }
        return p;
    }

    /// Random table + random mirror. excludeId avoids repeating the table just played (pass null to allow).
    public static (BilliardsPreset preset, TableMirror mirror) Draw(System.Random rng, string excludeId, string forceId = null)
    {
        var all = All;
        if (all.Count == 0) return (null, default);
        int i = rng.Next(all.Count);                               // always consumed, so forcing a table never changes the rest of the seed
        if (!string.IsNullOrEmpty(forceId))
            for (int k = 0; k < all.Count; k++)
                if (all[k].id == forceId) return (all[k], TableMirror.Roll(rng));
        if (all.Count > 1 && all[i].id == excludeId) i = (i + 1 + rng.Next(all.Count - 1)) % all.Count;
        return (all[i], TableMirror.Roll(rng));
    }
}
