using System;

/// One call at a time, a minimum gap between calls, and a ring timer that only advances
/// while nothing else is happening. Pure C#: PhoneController drives it from Update with
/// GameDelta (0 while the clock is paused or the game is over).
///
///   Waiting --interval--> Ringing --answer--> InCall --EndCall--> Gap --callMinGap--> Waiting
///                            |--decline/timeout----------------------^
///
/// While the phone is off (or booting) a call that comes due is logged as missed and the
/// schedule moves on: it does NOT queue up and fire the moment the phone boots.
public sealed class CallScheduler
{
    public enum State { Waiting, Ringing, InCall, Gap }

    readonly PhoneConfig _cfg;
    readonly Random _rng;
    float _timer;                 // time left in the current state

    public State Current { get; private set; } = State.Waiting;
    public CallInfo Call { get; private set; }

    public Func<bool> CanRing = () => true;          // phone on and booted
    public Func<bool> ItrsEligible = () => false;    // a bill is pending

    public event Action<CallInfo> RingStarted;
    public event Action<CallInfo, CallEnd> RingStopped;   // answered / declined / missed (timeout or phone off)
    public event Action<CallInfo> CallFinished;          // InCall -> Gap

    public CallScheduler(PhoneConfig cfg, Random rng)
    {
        _cfg = cfg; _rng = rng;
        _timer = NextInterval();
    }

    /// Robocalls (Q10): multiplies every interval scheduled while it is set (0.5 = twice as often). The gap between calls is unchanged.
    public float IntervalScale = 1f;

    float NextInterval() => (_cfg.ringIntervalMin + (float)_rng.NextDouble() * (_cfg.ringIntervalMax - _cfg.ringIntervalMin)) * IntervalScale;

    /// gameDelta: seconds of RUNNING clock this frame (0 when paused / game over).
    public void Tick(float gameDelta)
    {
        if (gameDelta <= 0f) return;
        switch (Current)
        {
            case State.Waiting:
                _timer -= gameDelta;
                if (_timer > 0f) return;
                var call = CallerRoller.Roll(_rng, _cfg, ItrsEligible());
                if (!CanRing())
                {
                    RingStopped?.Invoke(call, CallEnd.Missed);   // logged as a missed call
                    _timer = NextInterval();
                    return;
                }
                Call = call; Current = State.Ringing; _timer = _cfg.maxRingDuration;
                RingStarted?.Invoke(call);
                return;

            case State.Ringing:
                _timer -= gameDelta;
                if (_timer <= 0f) StopRinging(CallEnd.Missed);
                return;

            case State.InCall:
                return;                        // the call screen ends itself via EndCall()

            case State.Gap:
                _timer -= gameDelta;
                if (_timer <= 0f) { Current = State.Waiting; _timer = NextInterval(); }
                return;
        }
    }

    public bool Answer()
    {
        if (Current != State.Ringing) return false;
        var c = Call;
        Current = State.InCall;
        RingStopped?.Invoke(c, CallEnd.Answered);
        return true;
    }

    public bool Decline()
    {
        if (Current != State.Ringing) return false;
        StopRinging(CallEnd.Declined);
        return true;
    }

    /// Call screen finished (hung up, choice made, caller timed out).
    public void EndCall()
    {
        if (Current != State.InCall) return;
        var c = Call;
        Call = null; Current = State.Gap; _timer = _cfg.callMinGap;
        CallFinished?.Invoke(c);
    }

    /// Phone forced off mid-ring or mid-call (Cable Eater, power button, game over).
    public void Abort()
    {
        if (Current == State.Ringing) StopRinging(CallEnd.Missed);
        else if (Current == State.InCall) EndCall();
    }

    void StopRinging(CallEnd how)
    {
        var c = Call;
        Call = null; Current = State.Gap; _timer = _cfg.callMinGap;
        RingStopped?.Invoke(c, how);
    }
    
    public void ForceRingNow()
    {
        if (Current != State.Waiting && Current != State.Gap) return;
        Current = State.Waiting;
        _timer = 0f;
    }
}
