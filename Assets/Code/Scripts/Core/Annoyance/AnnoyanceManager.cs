using UnityEngine;

/// <summary>
/// Central scheduler every interruption routes through - phone, ITRS, cat (DESIGN.md I8, §5.7).
/// Enforces a minimum gap between any two annoyance events and blocks a new one from
/// starting while another is still active. Callers request Begin/End around their own
/// event lifetime; instantaneous events (ITRS bills) can Begin then End immediately.
/// </summary>
public class AnnoyanceManager : MonoBehaviour
{
    public static AnnoyanceManager Instance { get; private set; }

    private bool isBusy;
    private float lastEventEndTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool CanStart => !isBusy && Time.time - lastEventEndTime >= GameManager.Instance.GameConfig.annoyanceMinGap;

    /// <summary>Call before starting an annoyance. Returns false if another is active or the gap hasn't elapsed - caller should retry shortly rather than drop the event.</summary>
    public bool TryBegin(string reason)
    {
        if (!CanStart) return false;
        isBusy = true;
        return true;
    }

    /// <summary>Call when the annoyance's effect ends (ring stops, bill resolved, cat shooed).</summary>
    public void End(string reason)
    {
        isBusy = false;
        lastEventEndTime = Time.time;
    }
}
