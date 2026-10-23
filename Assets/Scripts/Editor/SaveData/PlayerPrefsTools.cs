using UnityEditor;
using UnityEngine;

/// Menu items to reset the saved data (PlayerPrefs) without entering Play mode.
/// The game stores: TutorialSeen, HintSeen_<HintId>, CardCollection_UnlockedIds, RunHistory_* (PROJECT.md §7.5).
public static class PlayerPrefsTools
{
    const string Menu = "Tools/OverTimer/Save Data/";

    [MenuItem(Menu + "Clear All PlayerPrefs")]
    static void ClearAll()
    {
        if (!Confirm("Delete ALL PlayerPrefs of this project?\n\nThis wipes the intro tutorial flag, hint flags, the card collection and the run history.")) return;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[SaveData] All PlayerPrefs deleted.");
    }

    [MenuItem(Menu + "Reset Tutorial and Hints Only")]
    static void ResetGuidance()
    {
        if (!Confirm("Reset the intro tutorial and every hint flag?\n\nThe card collection and the run history are kept.")) return;

        PlayerPrefs.DeleteKey("TutorialSeen");
        foreach (HintId id in System.Enum.GetValues(typeof(HintId))) PlayerPrefs.DeleteKey($"HintSeen_{id}");
        PlayerPrefs.Save();
        Debug.Log("[SaveData] Tutorial and hint flags reset.");
    }

    static bool Confirm(string message)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Save Data", "Exit Play mode first: the running game keeps its own copy of these values.", "OK");
            return false;
        }
        return EditorUtility.DisplayDialog("Save Data", message, "Delete", "Cancel");
    }
}
