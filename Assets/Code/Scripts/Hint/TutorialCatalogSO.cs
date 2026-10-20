using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The text of every first-time tutorial panel (Assets/Settings/Configs/Tutorials.asset). Ids are permanent (the seen
/// flag is stored per id). The texts here are PLACEHOLDERS: Ari writes the final copy. Rich text like
/// &lt;color=#FFD166&gt;word&lt;/color&gt; works, as on the intro panel.
/// </summary>
[CreateAssetMenu(fileName = "Tutorials", menuName = "OverTimer/Tutorials")]
public class TutorialCatalogSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Permanent key, e.g. \"first_tax\". Never rename.")]
        public string id;
        [Tooltip("What triggers it (for whoever edits the text).")]
        public string whenShown;
        public TutorialPanel.Step[] steps;
    }

    public List<Entry> entries = new List<Entry>();

    public Entry Find(string id)
    {
        foreach (var e in entries) if (e != null && e.id == id) return e;
        return null;
    }
}
