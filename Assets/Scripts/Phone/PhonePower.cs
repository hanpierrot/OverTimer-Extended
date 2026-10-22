using System;
using System.Collections.Generic;

/// On / Off / Booting, plus forced-off locks (Cable Eater). Pure C#.
/// Boot time counts real seconds with the clock running (pass gameDelta); a boot interrupted by
/// a forced-off lock is cancelled.
public sealed class PhonePower
{
    public enum State { On, Off, Booting }

    readonly float _bootSeconds;
    readonly HashSet<object> _forcedOff = new HashSet<object>();
    float _bootLeft;

    public State Current { get; private set; } = State.On;
    public bool IsUsable => Current == State.On;
    public bool IsForcedOff => _forcedOff.Count > 0;
    public float BootProgress => Current == State.Booting ? 1f - _bootLeft / _bootSeconds : (Current == State.On ? 1f : 0f);

    public event Action<State> Changed;

    public PhonePower(float bootSeconds) { _bootSeconds = bootSeconds; }

    /// The power button. Off -> Booting; On/Booting -> Off. Ignored while forced off.
    public bool Press()
    {
        if (IsForcedOff) return false;
        Set(Current == State.Off ? State.Booting : State.Off);
        if (Current == State.Booting) _bootLeft = _bootSeconds;
        return true;
    }

    public void Tick(float gameDelta)
    {
        if (Current != State.Booting || gameDelta <= 0f) return;
        _bootLeft -= gameDelta;
        if (_bootLeft <= 0f) Set(State.On);
    }

    /// Cable Eater. The phone goes off and the button stops working until every lock is released.
    public void ForceOff(object holder)
    {
        _forcedOff.Add(holder);
        if (Current != State.Off) Set(State.Off);
    }

    /// Releasing does NOT turn the phone back on: the player boots it (5 s) like normal.
    public void ReleaseForceOff(object holder) => _forcedOff.Remove(holder);

    void Set(State s) { if (s == Current) return; Current = s; Changed?.Invoke(s); }
}
