using System.IO;
using UnityEditor;
using UnityEngine;

/// Editor shortcuts for the Phase 9 round log (BilliardsRoundLog).
public static class BilliardsRoundLogMenu
{
    [MenuItem("Tools/OverTimer/Billiards/Round Log Summary")]
    static void Summary()
    {
        var rounds = BilliardsRoundLog.LoadAll();
        Debug.Log("[Billiards] Round log summary (" + BilliardsRoundLog.FilePath + ")\n" + BilliardsRoundLog.Summarize(rounds));
    }

    [MenuItem("Tools/OverTimer/Billiards/Reveal Round Log File")]
    static void Reveal()
    {
        string path = BilliardsRoundLog.FilePath;
        if (File.Exists(path)) EditorUtility.RevealInFinder(path);
        else Debug.Log("[Billiards] no round log yet: " + path);
    }
}
