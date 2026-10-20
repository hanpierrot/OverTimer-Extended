using System;
using System.Collections.Generic;

/// Which first-time tutorial panel is next. Pure logic: each id is shown once (seen ids are remembered through the two
/// delegates, PlayerPrefs in the game), one panel at a time, later requests wait in line. Owned by TutorialService.
public sealed class TutorialTracker
{
    readonly Func<string, bool> _isSeen;
    readonly Action<string> _markSeen;
    readonly Queue<string> _queue = new Queue<string>();
    readonly HashSet<string> _queued = new HashSet<string>();

    public TutorialTracker(Func<string, bool> isSeen, Action<string> markSeen)
    {
        _isSeen = isSeen;
        _markSeen = markSeen;
    }

    /// The id being shown right now, or null.
    public string Current { get; private set; }
    public int Waiting => _queue.Count;

    /// Ask for a panel. False when it was already seen, is on screen, or is already waiting.
    public bool Request(string id)
    {
        if (string.IsNullOrEmpty(id) || _isSeen(id) || id == Current || !_queued.Add(id)) return false;
        _queue.Enqueue(id);
        return true;
    }

    /// The next id to show (it becomes Current), or null while one is on screen or none is waiting.
    public string TakeNext()
    {
        if (Current != null || _queue.Count == 0) return null;
        Current = _queue.Dequeue();
        _queued.Remove(Current);
        return Current;
    }

    /// The panel was closed: the id counts as seen.
    public void Finish()
    {
        if (Current == null) return;
        _markSeen(Current);
        Current = null;
    }
}
