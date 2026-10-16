using System;
using System.Collections.Generic;

public enum Pocket { TL, TM, TR, BL, BM, BR }

/// What the simulation saw during one shot. Filled by BilliardsSim, judged by BilliardsReferee.
public sealed class ShotReport
{
    public string firstContact;   // first ball the cue touched; null = touched nothing
    public bool cueScratched;
    public readonly List<(string ball, Pocket pocket)> pots = new List<(string, Pocket)>(); // in the order they dropped
}

public enum Verdict { Continue, Win, Fail }

public sealed class ShotResult
{
    public Verdict verdict = Verdict.Continue;
    public bool foul;
    public string failReason;
    public readonly List<string> popped = new List<string>();  // legally potted, in order -> queue view slides these out
    public readonly List<string> respot = new List<string>();  // go back on the table ("cue" included on a scratch)
}

/// Pure rules, no Unity. One instance per table.
public sealed class BilliardsReferee
{
    readonly List<string> _queue;
    readonly HashSet<string> _forbidden;
    readonly Dictionary<string, Pocket> _called;
    readonly bool _strictFirstContact;
    readonly bool _finalWrongPocketFails;

    public IReadOnlyList<string> Queue => _queue;
    public string Head  => _queue.Count > 0 ? _queue[0] : null;
    public string Final => _queue.Count > 0 ? _queue[_queue.Count - 1] : null;

    public BilliardsReferee(IEnumerable<string> order, IEnumerable<string> forbidden,
                            Dictionary<string, Pocket> called,
                            bool strictFirstContact = true, bool finalWrongPocketFails = true)
    {
        _queue = new List<string>(order);
        _forbidden = new HashSet<string>(forbidden ?? Array.Empty<string>());
        _called = called ?? new Dictionary<string, Pocket>();
        _strictFirstContact = strictFirstContact;
        _finalWrongPocketFails = finalWrongPocketFails;
    }

    // Clone support: only the queue changes during a round, the rule sets are shared.
    BilliardsReferee(BilliardsReferee o)
    {
        _queue = new List<string>(o._queue);
        _forbidden = o._forbidden; _called = o._called;
        _strictFirstContact = o._strictFirstContact; _finalWrongPocketFails = o._finalWrongPocketFails;
    }

    public BilliardsReferee Clone() => new BilliardsReferee(this);

    public ShotResult Judge(ShotReport r)
    {
        var res = new ShotResult();
        string headAtStart = Head;

        res.foul = r.cueScratched
                || r.firstContact == null
                || (_strictFirstContact && r.firstContact != headAtStart);

        if (r.cueScratched) res.respot.Add("cue");

        foreach (var (ball, pocket) in r.pots)
        {
            if (ball == "cue") continue;
            if (_forbidden.Contains(ball)) return Fail(res, $"{ball} is forbidden");

            int idx = _queue.IndexOf(ball);
            if (idx < 0) continue;                               // blocker ball: stays down, no effect

            bool isFinal     = idx == _queue.Count - 1;
            bool wrongPocket = _called.TryGetValue(ball, out var want) && want != pocket;
            bool legal       = !res.foul && idx == 0 && !wrongPocket;

            if (legal) { _queue.RemoveAt(0); res.popped.Add(ball); continue; }

            if (isFinal)
            {
                // Early or on a foul is always fatal. Wrong pocket alone is fatal only if configured (Q2).
                bool fatal = res.foul || idx != 0 || _finalWrongPocketFails;
                if (fatal)
                    return Fail(res, res.foul    ? $"{ball} dropped on a foul"
                                   : idx != 0    ? $"{ball} dropped too early"
                                                 : $"{ball} went in the wrong pocket");
            }
            res.respot.Add(ball);
        }

        if (_queue.Count == 0) res.verdict = Verdict.Win;
        return res;
    }

    static ShotResult Fail(ShotResult r, string why) { r.verdict = Verdict.Fail; r.failReason = why; return r; }
}
