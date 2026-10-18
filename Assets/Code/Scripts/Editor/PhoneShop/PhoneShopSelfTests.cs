using System;
using UnityEditor;
using UnityEngine;
using Random = System.Random;   // the pure classes take a System.Random (RngService); never UnityEngine.Random

/// Headless checks for the Phone/Shop plan's pure classes (PHONE_SHOP_DELIVERY_IMPLEMENTATION Appendix B).
/// Grows with each phase: add checks, never delete one.
public static class PhoneShopSelfTests
{
    static int _fails, _passes;

    static void Check(bool ok, string name)
    {
        if (ok) _passes++; else { _fails++; Debug.LogError("[PhoneShop] FAIL " + name); }
    }

    [MenuItem("Tools/OverTimer/PhoneShop/Run Self-Tests")]
    public static void Run()
    {
        _fails = 0; _passes = 0;

        ClockCore();
        CallsAndPower();
        InboxAndBanners();
        TaxDeadlines();
        Dialogues();

        if (_fails == 0) Debug.Log($"[PhoneShop] ALL OK ({_passes} checks)");
        else Debug.LogError($"[PhoneShop] {_fails} FAILED, {_passes} passed");
    }

    // ---- Phase 1.3: CallerRoller, CallScheduler, PhonePower
    static void CallsAndPower()
    {
        var cfg = new PhoneConfig(); var rng = new Random(1); int N = 200000;
        var shown = new System.Collections.Generic.Dictionary<CallerKind, int>();
        var truth = new System.Collections.Generic.Dictionary<CallTruth, int>();
        for (int i = 0; i < N; i++)
        {
            var c = CallerRoller.Roll(rng, cfg, true);
            shown.TryGetValue(c.shownAs, out int s); shown[c.shownAs] = s + 1;
            truth.TryGetValue(c.truth, out int t); truth[c.truth] = t + 1;
        }
        double sp = truth[CallTruth.Telemarketer] / (double)N;
        Check(Math.Abs(shown[CallerKind.Telemarketer] / (double)N - 0.25) < 0.01 && Math.Abs(shown[CallerKind.Unknown] / (double)N - 0.40) < 0.01, "caller ID weights 25/25/40/10 (Unknown is the most common)");
        Check(Math.Abs(sp - 0.49) < 0.01, $"real telemarketer share ~49% (got {sp:P1})");
        rng = new Random(2); bool anyItrs = false;
        for (int i = 0; i < 50000; i++) if (CallerRoller.Roll(rng, cfg, false).truth == CallTruth.ITRS) anyItrs = true;
        Check(!anyItrs, "no ITRS calls while no bill is pending");
        var unk = new CallInfo { shownAs = CallerKind.Unknown, truth = CallTruth.Client };
        Check(unk.PausesClock && !new CallInfo { truth = CallTruth.Telemarketer }.PausesClock, "client/ITRS pause the clock, telemarketer doesn't");

        var cs = new CallScheduler(cfg, new Random(3)); var log = new System.Collections.Generic.List<string>();
        cs.RingStarted += c => log.Add("ring"); cs.RingStopped += (c, e) => log.Add(e.ToString()); cs.CallFinished += c => log.Add("done");
        float first = 0; while (cs.Current != CallScheduler.State.Ringing && first < 100) { cs.Tick(0.1f); first += 0.1f; }
        Check(log.Count == 1 && log[0] == "ring" && first >= 29.9f && first <= 60.2f, $"first ring within 30-60 s (at {first:F1} s)");
        for (int i = 0; i < 200; i++) cs.Tick(0f);
        Check(cs.Current == CallScheduler.State.Ringing, "paused clock (gameDelta 0) freezes the ring timer");
        Check(cs.Answer() && cs.Current == CallScheduler.State.InCall, "answer -> in call");
        for (int i = 0; i < 1000; i++) cs.Tick(0.1f);
        Check(cs.Current == CallScheduler.State.InCall, "no second call while one is active (100 s later)");
        cs.EndCall(); float tt = 0; while (cs.Current != CallScheduler.State.Ringing) { cs.Tick(0.1f); tt += 0.1f; if (tt > 200) break; }
        Check(tt >= 12f + 30f - 0.11f && tt <= 12f + 60f + 0.11f, $"next ring = 12 s gap + 30-60 s interval (got {tt:F1} s)");
        for (int i = 0; i < 101; i++) cs.Tick(0.1f);
        Check(log[log.Count - 1] == "Missed", "unanswered ring times out after 10 s -> Missed");
        var off = new CallScheduler(cfg, new Random(4)); bool on = false; off.CanRing = () => on; int missed = 0, rang = 0;
        off.RingStopped += (c, e) => { if (e == CallEnd.Missed) missed++; }; off.RingStarted += c => rang++;
        for (int i = 0; i < 1300; i++) off.Tick(0.1f);
        Check(rang == 0 && missed >= 2, $"phone off: calls are logged as missed, none ring ({missed} missed)");
        on = true; off.Tick(0.1f); Check(off.Current != CallScheduler.State.Ringing, "booting the phone does NOT release a backlog of calls");

        var fast = new CallScheduler(cfg, new Random(5));
        while (fast.Current != CallScheduler.State.Ringing) fast.Tick(0.1f);
        fast.Answer(); fast.EndCall(); fast.IntervalScale = 0.5f; float ft = 0;      // the scale applies to intervals scheduled from now on
        while (fast.Current != CallScheduler.State.Ringing && ft < 200) { fast.Tick(0.1f); ft += 0.1f; }
        Check(ft <= 12f + 30f + 0.2f, $"Robocalls (IntervalScale 0.5): next ring within 12 s gap + 30 s (at {ft:F1} s)");
        var abort = new CallScheduler(cfg, new Random(6)); int stops = 0; abort.RingStopped += (c, e) => stops++;
        while (abort.Current != CallScheduler.State.Ringing) abort.Tick(0.1f);
        abort.Abort(); Check(abort.Current == CallScheduler.State.Gap && stops == 1, "Abort() while ringing ends the ring as missed and enters the gap");

        var pw = new PhonePower(5f);
        pw.Press(); Check(pw.Current == PhonePower.State.Off, "press: on -> off");
        pw.Press(); Check(pw.Current == PhonePower.State.Booting, "press: off -> booting");
        for (int i = 0; i < 49; i++) pw.Tick(0.1f); Check(pw.Current == PhonePower.State.Booting, "still booting at 4.9 s");
        pw.Tick(0.2f); Check(pw.IsUsable, "on after 5 s");
        object eater = "cableEater"; pw.ForceOff(eater);
        Check(pw.Current == PhonePower.State.Off && !pw.Press(), "Cable Eater forces off; the button is dead");
        pw.ReleaseForceOff(eater); Check(pw.Current == PhonePower.State.Off && pw.Press() && pw.Current == PhonePower.State.Booting, "after release the player must boot it (stays off)");
        pw.ForceOff(eater); Check(pw.Current == PhonePower.State.Off, "forced off mid-boot cancels the boot");
    }

    // ---- Call dialogue (client/ITRS/telemarketer scripts)
    static void Dialogues()
    {
        var d = new CallDialogue(2);
        Check(d.Index == 0 && !d.IsFinal, "a 2-line call starts on line 0, not final");
        Check(d.Advance() && d.IsFinal && d.Index == 1, "reply moves to the last line");
        Check(!d.Advance() && d.Index == 1, "the last line can't be advanced past");
        var one = new CallDialogue(1); Check(one.IsFinal, "a 1-line script is final at once");
        Check(new CallDialogue(0).IsFinal, "an empty script is treated as one final line");

        var cat = ScriptableObject.CreateInstance<CallerProfileCatalog>();
        foreach (CallTruth t in new[] { CallTruth.Telemarketer, CallTruth.Client, CallTruth.ITRS })
        {
            var s = cat.Script(t);
            Check(s.Count >= 2 && s[s.Count - 1].replyLabel == "Hang up.", t + " default script ends with a \"Hang up.\" line");
        }
        Check(cat.Script(CallTruth.Client)[0].replyLabel == "Got you.", "client's first reply is \"Got you.\"");
        Check(cat.Pick(CallerKind.Client, -5) != null && cat.Pick(CallerKind.Unknown, 123456789) != null, "profile pick works for any seed");
        Check(!string.IsNullOrEmpty(cat.NoReplyLine) && cat.NoReplyLine.Contains("Are you there"), "client no-reply goodbye line exists");
        UnityEngine.Object.DestroyImmediate(cat);

        var rec = new CallRecord { name = "Client", outcome = CallEnd.Answered };
        rec.transcript.Add("Client: hi");
        Check(rec.transcript.Count == 1 && rec.result == "", "call record starts with no result");
    }

    // ---- Phase 1.4: TaxDeadline
    static void TaxDeadlines()
    {
        var td = new TaxDeadline(40, 30f); int res = 0; td.Resolved += _ => res++;
        for (int i = 0; i < 299; i++) td.Tick(0.1f); Check(td.State == TaxDeadline.Outcome.Pending, "pending at 29.9 s");
        for (int i = 0; i < 100; i++) td.Tick(0f); Check(td.State == TaxDeadline.Outcome.Pending, "paused clock doesn't run the window out");
        td.Tick(0.2f); Check(td.State == TaxDeadline.Outcome.Penalized && res == 1, "unpaid at 30 s -> penalized once");
        td.MarkPaid(); td.Tick(5f); Check(res == 1 && td.State == TaxDeadline.Outcome.Penalized, "no second resolution");
        var td2 = new TaxDeadline(40, 30f); td2.Extend(15f); for (int i = 0; i < 400; i++) td2.Tick(0.1f);
        Check(td2.State == TaxDeadline.Outcome.Pending, "ITRS payment plan +15 s");
        td2.Double(); td2.MarkPaid(); Check(td2.Amount == 80 && td2.State == TaxDeadline.Outcome.Paid, "Audit doubles, then paid");
        var td3 = new TaxDeadline(40, 30f); td3.MarkPaid(); td3.Tick(100f); Check(td3.State == TaxDeadline.Outcome.Paid, "a paid bill never penalises later");
    }

    // ---- Phase 1.3: Inbox, BannerQueue
    static void InboxAndBanners()
    {
        var ib = new Inbox(); var bq = new BannerQueue(4f); ib.Added += bq.Push;
        for (int i = 0; i < 5; i++) ib.Add(MessageKind.System, "m" + i, "");
        Check(bq.Visible.Count == 3 && bq.Waiting == 2 && ib.Unread == 5, "5 at once: 3 banners, 2 waiting, all 5 in the Inbox");
        bq.Tick(4.1f); Check(bq.Visible.Count == 2 && bq.Waiting == 0, "after 4 s the waiting two show");
        bq.Tick(4.1f); Check(bq.Visible.Count == 0, "all banners gone; Inbox still has 5");
        Check(ib.All[0].title == "m4", "Inbox is newest first");
    }

    // ---- Phase 1.1: PauseLock, SpeedModifiers
    static void ClockCore()
    {
        var pl = new PauseLock(); int flips = 0; pl.PausedChanged += _ => flips++;
        object call = "call", cut = "cutscene";
        pl.Acquire(call); pl.Acquire(cut); pl.Release(call);
        Check(pl.IsPaused, "overlap: releasing the call keeps the cutscene's pause");
        pl.Release(cut); Check(!pl.IsPaused && flips == 2, "clock resumes only when the last holder releases (2 flips)");
        pl.Acquire(call); pl.Acquire(call); pl.Release(call); Check(!pl.IsPaused, "same holder twice counts once");
        Check(!pl.Release("nobody"), "releasing an unknown holder is a no-op");

        var sm = new SpeedModifiers(); object snail = "snail", ring = "ring";
        sm.Set(snail, 0.75f); sm.Set(ring, 1.5f);
        Check(Math.Abs(sm.Effective - 1.125f) < 1e-5, "snail 0.75 x ring 1.5 = 1.125 (stack, no clobber)");
        sm.Remove(ring); Check(Math.Abs(sm.Effective - 0.75f) < 1e-5, "ring ends: snail still applies");
        sm.Set("scarab", 0f); Check(sm.Effective == 0f, "Time Scarab x0 stops the clock");
        sm.Clear(); sm.Set("a", 2f); sm.Set("b", 2f); Check(sm.Effective == 3f, "clamped to 3");
        sm.Set("a", 1f); Check(sm.Count == 2 && sm.Effective == 2f, "same key replaces, never double-counts");
    }
}
