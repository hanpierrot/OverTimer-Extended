using UnityEngine;

/// <summary>
/// First-time tutorial panels for the Phone, Shop and Delivery mechanics (plan Phase 3.3; DESIGN §5.16). Any system calls
/// TutorialService.TryShow("id") at the moment a mechanic first appears: the panel shows once per id (the seen flag is
/// PlayerPrefs "Tutorial_Seen_&lt;id&gt;"), one at a time, and the CLOCK IS PAUSED while it is open (Q5). It reuses the
/// intro's TutorialPanel. Resetting the flags (cheat menu) shows them all again; so does the Creepy Jester's
/// PlayerPrefs.DeleteAll (accepted, P18).
/// </summary>
public class TutorialService : MonoBehaviour
{
    private const string KeyPrefix = "Tutorial_Seen_";
    private const string IntroKey = "TutorialSeen";          // the intro panel's own flag (TutorialPanel)

    public static TutorialService Instance { get; private set; }

    [SerializeField] private TutorialPanel panel;
    [SerializeField] private TutorialCatalogSO catalog;

    private TutorialTracker _tracker;

    /// <summary>Safe to call from anywhere, any time: does nothing when there is no service or the id was already seen.</summary>
    public static void TryShow(string id)
    {
        if (Instance != null) Instance.Request(id);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _tracker = new TutorialTracker(IsSeen, MarkSeen);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Request(string id)
    {
        if (catalog == null || catalog.Find(id) == null) return;
        _tracker.Request(id);
    }

    private void Update()
    {
        if (panel == null || catalog == null || _tracker == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (panel.IsOpen) return;                           // the intro, or an earlier tutorial, is still up

        string id = _tracker.TakeNext();
        if (id == null) return;

        var entry = catalog.Find(id);
        if (entry == null || entry.steps == null || entry.steps.Length == 0) { _tracker.Finish(); return; }
        panel.Present(entry.steps, _tracker.Finish);
    }

    /// <summary>Cheat menu: every tutorial (and the intro) will show again.</summary>
    public void ResetAll()
    {
        if (catalog != null)
            foreach (var e in catalog.entries)
                if (e != null && !string.IsNullOrEmpty(e.id)) PlayerPrefs.DeleteKey(KeyPrefix + e.id);
        PlayerPrefs.DeleteKey(IntroKey);
        PlayerPrefs.Save();
    }

    private static bool IsSeen(string id) => PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1;

    private static void MarkSeen(string id)
    {
        PlayerPrefs.SetInt(KeyPrefix + id, 1);
        PlayerPrefs.Save();
    }
}
