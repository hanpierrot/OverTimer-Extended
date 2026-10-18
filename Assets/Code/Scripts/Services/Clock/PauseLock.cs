using System;
using System.Collections.Generic;

/// Reference-counted clock pause. ClockService owns one of these and treats "paused" as
/// "at least one holder". Replaces the plain PauseCountdown/ResumeCountdown on/off switch,
/// which breaks as soon as a paused call and a cutscene overlap.
public sealed class PauseLock
{
    readonly HashSet<object> _holders = new HashSet<object>();

    public bool IsPaused => _holders.Count > 0;
    public int HolderCount => _holders.Count;
    public event Action<bool> PausedChanged;   // fires only on a real change

    /// Same holder twice counts once. Returns true if this call paused the clock.
    public bool Acquire(object holder)
    {
        if (holder == null) throw new ArgumentNullException(nameof(holder));
        bool was = IsPaused;
        _holders.Add(holder);
        if (!was && IsPaused) { PausedChanged?.Invoke(true); return true; }
        return false;
    }

    /// Releasing a holder that isn't held is a silent no-op. Returns true if the clock resumed.
    public bool Release(object holder)
    {
        if (holder == null) return false;
        bool was = IsPaused;
        _holders.Remove(holder);
        if (was && !IsPaused) { PausedChanged?.Invoke(false); return true; }
        return false;
    }

    /// Game over / scene teardown only.
    public void ReleaseAll()
    {
        bool was = IsPaused;
        _holders.Clear();
        if (was) PausedChanged?.Invoke(false);
    }
}
