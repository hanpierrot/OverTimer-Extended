using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One app icon on the phone's home screen, keyed by a string id (Batch 2's IconGate hides/shows by this id).</summary>
public class PhoneIconView : MonoBehaviour
{
    [SerializeField] private string appId;
    [SerializeField] private Button button;
    [SerializeField] private GameObject badge;
    [SerializeField] private TMP_Text badgeText;

    public string AppId => appId;
    public Button Button => button;

    public void SetBadge(int count)
    {
        if (badge == null) return;
        badge.SetActive(count > 0);
        if (count > 0 && badgeText != null) PhoneUi.SetText(badgeText, count > 9 ? "9+" : count.ToString());
    }
}

/// <summary>Small helpers shared by the phone views.</summary>
public static class PhoneUi
{
    /// <summary>Assign text only when it changed (no per-frame string churn on TMP).</summary>
    public static void SetText(TMP_Text t, string s)
    {
        if (t != null && t.text != s) t.text = s;
    }

    public static void SetActive(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }
}
