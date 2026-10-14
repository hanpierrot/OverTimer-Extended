using System;
using System.Collections.Generic;
using UnityEngine;
#if !BILLIARDS_HARNESS
using UnityEditor;
#endif

/// Editor-only self-tests for the Billiards data layer (Phase 1). Run from
/// Tools/OverTimer/Billiards/Run Self-Tests. Pure C# apart from the preset-loading and asset checks at the end.
public static class BilliardsSelfTests
{
#if !BILLIARDS_HARNESS
    [MenuItem("Tools/OverTimer/Billiards/Run Self-Tests")]
    static void Run()
    {
        int failed = RunAll(Debug.Log);
        if (failed == 0) Debug.Log("[Billiards] Self-tests: ALL OK");
        else Debug.LogError($"[Billiards] Self-tests: {failed} FAILED");
    }
#endif

    static int _fails, _checks;
    static Action<string> _log;

    static void Check(bool c, string name)
    {
        _checks++;
        _log((c ? "PASS " : "FAIL ") + name);
        if (!c) _fails++;
    }

    static ShotReport Rep(string first, bool scratch, params (string, Pocket)[] pots)
    {
        var r = new ShotReport { firstContact = first, cueScratched = scratch };
        foreach (var p in pots) r.pots.Add(p);
        return r;
    }

    static BilliardsReferee Ref(Dictionary<string, Pocket> called = null, string[] forb = null) =>
        new BilliardsReferee(new[] { "1", "3", "5", "8" }, forb, called);

    static GameConfig NewConfig() => ScriptableObject.CreateInstance<GameConfig>();

    static BilliardsPreset Table(params (string id, float x, float y)[] balls)
    {
        var spawns = new BallSpawn[balls.Length];
        for (int i = 0; i < balls.Length; i++) spawns[i] = new BallSpawn { id = balls[i].id, x = balls[i].x, y = balls[i].y };
        return new BilliardsPreset { id = "scenario", par = 1, balls = spawns, order = new[] { balls[balls.Length - 1].id } };
    }

    // ---- Phase 2 scenarios S1-S8 (+ respot checks). Run through the real sim with the default GameConfig values. ----
    static void RunScenarios(GameConfig cfg, BilliardsPreset straight, BilliardsPreset four)
    {
        var prm = BilliardsSimParams.FromConfig(cfg);
        float dt = BilliardsSim.FixedDt;
        float r = prm.ballRadius;

        // S1: the stored straight_test solution still wins. The angle is exact by construction; only the power was tuned.
        var row = BilliardsPresetVerifier.VerifyPreset(straight, cfg);
        Check(row.pass, "S1 straight_test solution wins on its last shot: " + row.reason);
        var rows = BilliardsPresetVerifier.VerifyAll(cfg);
        var fourRow = rows.Find(x => x.id == "four_in_order_01");
        Check(fourRow != null && fourRow.skipped, "verifier skips four_in_order_01 (no solution yet): " + (fourRow != null ? fourRow.reason : "missing"));

        // S2: head-on hit. First contact is the object ball, which takes almost all the speed.
        var sim = new BilliardsSim(Table(("cue", 0.25f, 0.5f), ("1", 0.5f, 0.5f)), prm);
        sim.Shoot(ShotInput.Quantized(0f, 0.5f));
        for (int k = 0; k < 3000 && sim.Report.firstContact == null; k++) sim.Step();
        sim.Step();
        float cueSpeed = sim.Velocity(sim.IndexOf("cue")).magnitude, objSpeed = sim.Velocity(sim.IndexOf("1")).magnitude;
        Check(sim.Report.firstContact == "1" && objSpeed > 10f * cueSpeed,
            $"S2 head-on: first contact '1', object ball speed {objSpeed:0.00} vs cue {cueSpeed:0.000} just after impact");

        // S3: lone cue into the bottom cushion. Only the normal component is reversed and scaled by the cushion restitution
        // (the tangential component is kept), so the outgoing ANGLE is steeper than a mirror image by design.
        sim = new BilliardsSim(Table(("cue", 0.25f, 0.5f)), prm);
        int ci = sim.IndexOf("cue");
        sim.Shoot(ShotInput.Quantized(-30f, 0.4f));
        Vector2 prev = sim.Velocity(ci), after = default; bool bounced = false;
        for (int k = 0; k < 3000 && !bounced; k++)
        {
            if (!sim.Step()) break;
            Vector2 v = sim.Velocity(ci);
            if (prev.y < 0f && v.y > 0f) { bounced = true; after = v; } else prev = v;
        }
        float speedBefore = prev.magnitude, keep = (speedBefore - prm.rollingDecel * dt) / speedBefore;   // friction runs before the cushion in the same step
        float wantY = -prm.cushionRestitution * prev.y * keep, wantX = prev.x * keep;
        Check(bounced && Math.Abs(after.y - wantY) < 0.01f * Math.Abs(wantY) + 1e-4f && Math.Abs(after.x - wantX) < 0.01f * Math.Abs(wantX) + 1e-4f,
            $"S3 cushion: vy {prev.y:0.000} -> {after.y:0.000} (x{-after.y / (prev.y * keep):0.00}, restitution {prm.cushionRestitution}), vx kept ({prev.x * keep:0.000} -> {after.x:0.000})");
        Check(sim.Report.firstContact == null, "S3 a cushion hit is not a first contact");

        // S4: cue fired straight at the TR pocket drops, touching nothing on the way.
        sim = new BilliardsSim(Table(("cue", 0.6f, 0.6f)), prm);
        sim.Shoot(ShotInput.Quantized(26.6f, 0.6f));
        var rep = sim.RunToRest();
        Check(rep.cueScratched && rep.firstContact == null && rep.pots.Count == 1 && rep.pots[0].pocket == Pocket.TR,
            "S4 cue straight into TR: scratched, first contact null, dropped in " + (rep.pots.Count > 0 ? rep.pots[0].pocket.ToString() : "nothing"));

        // S5: 200 seeded random shots on the four_in_order_01 layout. Nothing leaves the felt, no pair stays overlapped,
        // every shot settles before the safety cap. After each shot everything that dropped is put back.
        var rng = new System.Random(1234);
        sim = new BilliardsSim(four, prm);
        int capSteps = (int)Math.Ceiling(prm.maxShotSeconds / dt);
        float worstOut = 0f, worstOverlap = 0f; int slowest = 0; bool allSettled = true; int totalPots = 0;
        void Inspect()
        {
            int n = sim.BallCount;
            for (int i = 0; i < n; i++)
            {
                if (sim.IsPotted(i)) continue;
                Vector2 pi = sim.Position(i);
                float outside = Math.Max(Math.Max(-pi.x, pi.x - FeltMapper.TableW), Math.Max(-pi.y, pi.y - FeltMapper.TableH));
                if (outside > worstOut) worstOut = outside;
                for (int j = i + 1; j < n; j++)
                {
                    if (sim.IsPotted(j)) continue;
                    Vector2 pj = sim.Position(j);
                    float dx = pj.x - pi.x, dy = pj.y - pi.y;
                    float overlap = 2f * r - (float)Math.Sqrt(dx * dx + dy * dy);
                    if (overlap > worstOverlap) worstOverlap = overlap;
                }
            }
        }
        for (int s = 0; s < 200; s++)
        {
            float ang = (float)(rng.NextDouble() * 360.0 - 180.0), pw = (float)(0.05 + rng.NextDouble() * 0.95);
            sim.Shoot(ShotInput.Quantized(ang, pw));
            while (sim.Step()) Inspect();
            Inspect();
            if (sim.StepsThisShot >= capSteps) allSettled = false;
            if (sim.StepsThisShot > slowest) slowest = sim.StepsThisShot;
            var down = new List<string>();
            for (int i = 0; i < sim.BallCount; i++) if (sim.IsPotted(i)) down.Add(sim.IdOf(i));
            totalPots += down.Count;
            sim.ApplyRespots(down);
        }
        Check(worstOut <= 1e-3f, $"S5 no ball outside the felt (worst overshoot {worstOut:0.00000}), {totalPots} drops over 200 shots");
        Check(worstOverlap <= 0.01f * r, $"S5 no pair overlapping (worst {worstOverlap:0.00000} = {worstOverlap / r * 100f:0.0}% of a radius, limit 1%)");
        Check(allSettled, $"S5 every shot settled before the safety cap (slowest {slowest} of {capSteps} steps)");

        // S6: same shot twice gives the same state; a Clone behaves identically and shares nothing.
        long RunHash(BilliardsSim s0) { s0.Shoot(ShotInput.Quantized(12.3f, 0.8f)); s0.RunToRest(); return s0.StateHash(); }
        long h1 = RunHash(new BilliardsSim(four, prm)), h2 = RunHash(new BilliardsSim(four, prm));
        Check(h1 == h2, "S6 same shot twice: identical StateHash");
        var original = new BilliardsSim(four, prm);
        long before = original.StateHash();
        var clone = original.Clone();
        long hc = RunHash(clone);
        Check(original.StateHash() == before, "S6 running a Clone does not touch the original");
        Check(hc == h1 && RunHash(original) == h1, "S6 Clone and original give the same StateHash for the same shot");

        // S7: a fake frame loop (random frame times, <= 8 steps per frame, leftover carried) ends in the exact same state as RunToRest.
        var frng = new System.Random(77);
        var framed = new BilliardsSim(four, prm);
        framed.Shoot(ShotInput.Quantized(5f, 0.9f));
        double acc = 0.0; bool done = false; int frames = 0;
        while (!done && frames < 200000)
        {
            frames++;
            acc += 1.0 / 240.0 + frng.NextDouble() * (1.0 / 20.0 - 1.0 / 240.0);
            int steps = 0;
            while (acc >= dt && steps < 8) { acc -= dt; steps++; if (!framed.Step()) { done = true; break; } }
            if (acc > 8 * dt) acc = 8 * dt;
        }
        var direct = new BilliardsSim(four, prm);
        direct.Shoot(ShotInput.Quantized(5f, 0.9f)); direct.RunToRest();
        Check(done && framed.StateHash() == direct.StateHash(), $"S7 frame loop ({frames} frames) ends in the same StateHash as RunToRest");

        // S8: with no friction the shot never rests by itself, so the safety cap must end it. No hang.
        var noFriction = prm; noFriction.rollingDecel = 0f; noFriction.maxShotSeconds = 2f;
        var endless = new BilliardsSim(Table(("cue", 0.35f, 0.5f)), noFriction);
        endless.Shoot(ShotInput.Quantized(25f, 1f));                          // 25 deg never reaches a pocket in 2 s (checked by scanning angles)
        endless.RunToRest();
        int wantSteps = (int)Math.Ceiling(2f / dt);
        Check(!endless.IsMoving && endless.StepsThisShot == wantSteps, $"S8 rollingDecel 0: settled by the safety cap at step {endless.StepsThisShot} (cap {wantSteps})");

        // Respot: straight_test drops ball 1 in TR and leaves the cue touching its spawn spot, so the ring search kicks in.
        var rs = new BilliardsSim(Table(("cue", 0.6f, 0.6f), ("1", 0.8f, 0.8f)), prm);
        rs.Shoot(ShotInput.Quantized(26.6f, 0.5f)); rs.RunToRest();
        int ball = rs.IndexOf("1"), cue = rs.IndexOf("cue");
        Check(rs.IsPotted(ball) && !rs.IsPotted(cue), "respot setup: ball 1 dropped, cue stayed up");
        Vector2 spawn = new Vector2(1.6f, 0.8f), cuePos = rs.Position(cue);
        bool spawnFree = Math.Sqrt((cuePos.x - spawn.x) * (cuePos.x - spawn.x) + (cuePos.y - spawn.y) * (cuePos.y - spawn.y)) >= 2f * r + 0.001f;
        rs.ApplyRespots(new[] { "1" });
        Vector2 back = rs.Position(ball);
        Vector2 want = spawnFree ? spawn : new Vector2(spawn.x + 2.2f * r, spawn.y);                        // ring 1, candidate 0 (angle 0)
        Check(!rs.IsPotted(ball) && Math.Abs(back.x - want.x) < 1e-4f && Math.Abs(back.y - want.y) < 1e-4f,
            $"respot: ball 1 back at ({back.x:0.0000}, {back.y:0.0000}), expected {(spawnFree ? "its own spot" : "ring 1 / angle 0")} ({want.x:0.0000}, {want.y:0.0000})");
    }

    /// Returns the number of failed checks. `log` receives one line per check.
    public static int RunAll(Action<string> log)
    {
        _fails = 0; _checks = 0; _log = log;

        // --- Referee
        var r = Ref(); var s = r.Judge(Rep("1", false, ("1", Pocket.TL)));
        Check(s.verdict == Verdict.Continue && r.Head == "3" && s.popped.Count == 1, "legal pot pops head");
        r = Ref(); s = r.Judge(Rep("1", false, ("1", Pocket.TL), ("3", Pocket.BR)));
        Check(r.Head == "5" && s.popped.Count == 2, "combo in order counts both");
        r = Ref(); s = r.Judge(Rep("1", false, ("3", Pocket.BR), ("1", Pocket.TL)));
        Check(r.Head == "3" && s.respot.Contains("3") && s.popped.Contains("1"), "out-of-order 3 respotted, 1 counts");
        r = Ref(); s = r.Judge(Rep("1", false, ("8", Pocket.TR)));
        Check(s.verdict == Verdict.Fail, "final ball early = fail: " + s.failReason);
        r = Ref(); s = r.Judge(Rep("3", false, ("1", Pocket.TL)));
        Check(s.foul && s.respot.Contains("1") && r.Head == "1", "wrong first contact = foul, pot respotted");
        r = Ref(); s = r.Judge(Rep("1", true, ("1", Pocket.TL)));
        Check(s.foul && s.respot.Contains("cue") && s.respot.Contains("1"), "scratch respots cue and the pot");
        r = Ref(); s = r.Judge(Rep(null, false));
        Check(s.foul, "touched nothing = foul");
        var called = new Dictionary<string, Pocket> { { "1", Pocket.TL }, { "8", Pocket.TR } };
        r = Ref(called); s = r.Judge(Rep("1", false, ("1", Pocket.BL)));
        Check(s.respot.Contains("1") && r.Head == "1" && !s.foul, "wrong pocket (non-final) = respot, not a foul");
        r = new BilliardsReferee(new[] { "8" }, null, called); s = r.Judge(Rep("8", false, ("8", Pocket.BR)));
        Check(s.verdict == Verdict.Fail, "wrong pocket on final = fail: " + s.failReason);
        r = new BilliardsReferee(new[] { "5", "8" }, null, null); s = r.Judge(Rep("5", false, ("5", Pocket.TM), ("8", Pocket.BM)));
        Check(s.verdict == Verdict.Win, "5 then 8 in one shot = win");
        r = new BilliardsReferee(new[] { "5", "8" }, null, null); s = r.Judge(Rep("5", true, ("8", Pocket.BM)));
        Check(s.verdict == Verdict.Fail, "final on a scratch = fail");
        r = Ref(null, new[] { "12" }); s = r.Judge(Rep("1", false, ("12", Pocket.TM)));
        Check(s.verdict == Verdict.Fail, "forbidden = fail");
        r = Ref(); s = r.Judge(Rep("1", false, ("12", Pocket.TM)));
        Check(s.verdict == Verdict.Continue && r.Head == "1", "blocker ball drops harmlessly");

        // --- Mirror
        var m = new TableMirror(true, false);
        Check(m.MapPocket(Pocket.TL) == Pocket.TR && m.MapPocket(Pocket.TM) == Pocket.TM, "flipX pockets");
        m = new TableMirror(false, true);
        Check(m.MapPocket(Pocket.TL) == Pocket.BL && m.MapPocket(Pocket.BM) == Pocket.TM, "flipY pockets");
        m = new TableMirror(true, true);
        foreach (Pocket p in Enum.GetValues(typeof(Pocket)))
            if (m.MapPocket(m.MapPocket(p)) != p) Check(false, "involution " + p);
        var pt = new Vector2(0.2f, 0.7f); var back = m.Point(m.Point(pt));
        Check(Math.Abs(back.x - pt.x) < 1e-6 && Math.Abs(back.y - pt.y) < 1e-6 && m.MapPocket(Pocket.TL) == Pocket.BR, "flipXY point/pocket round trip");

        // --- Shots per difficulty, validator, payout
        var pre = new BilliardsPreset { id = "t", par = 4, shots = new[] { 0, 0, 3 }, balls = new[] { new BallSpawn { id = "cue", x = .2f, y = .5f }, new BallSpawn { id = "1", x = .5f, y = .5f } }, order = new[] { "1" } };
        var cfg = NewConfig();
        Check(pre.ShotsFor(BilliardsDifficulty.Easy, cfg.billiardsParOffsets) == 8 && pre.ShotsFor(BilliardsDifficulty.Hard, cfg.billiardsParOffsets) == 3, "shots override only hard");
        _log("validator on hard<par (a warning below is expected):");
        Check(!BilliardsPresetValidator.Validate(pre, cfg.billiardsParOffsets), "validator catches hard < par");
        pre.shots = null;
        foreach (BilliardsDifficulty d in Enum.GetValues(typeof(BilliardsDifficulty)))
            _log($"  par 4, {d,-6}: shots {pre.ShotsFor(d, cfg.billiardsParOffsets)}  payout at $40 = ${BilliardsPayout.OnWin(40, d, pre, cfg)}  at $10 = ${BilliardsPayout.OnWin(10, d, pre, cfg)}");
        Check(BilliardsPayout.OnWin(40, BilliardsDifficulty.Easy, pre, cfg) == 121 && BilliardsPayout.OnWin(10, BilliardsDifficulty.Easy, pre, cfg) == 30
           && BilliardsPayout.OnWin(40, BilliardsDifficulty.Medium, pre, cfg) == 223 && BilliardsPayout.OnWin(10, BilliardsDifficulty.Medium, pre, cfg) == 56
           && BilliardsPayout.OnWin(40, BilliardsDifficulty.Hard, pre, cfg) == 304 && BilliardsPayout.OnWin(10, BilliardsDifficulty.Hard, pre, cfg) == 76,
            "payout table (EV-based): 121/223/304 at $40, 30/56/76 at $10");
        // Expected value of one table at the assumed win rate must equal netRate * T * bet/betMax (the design intent).
        foreach (BilliardsDifficulty d in Enum.GetValues(typeof(BilliardsDifficulty)))
        {
            int i = (int)d;
            float secs = (pre.par + pre.ShotsFor(d, cfg.billiardsParOffsets)) * 0.5f * cfg.billiardsSecondsPerShot;
            float ev = cfg.billiardsAssumedWinRate[i] * BilliardsPayout.OnWin(40, d, pre, cfg) - 40f;
            float want = cfg.billiardsTargetRate[i] * secs;
            Check(Math.Abs(ev - want) < 1f, $"expected net at $40, {d}: {ev:0.0} matches target rate x seconds = {want:0.0}");
        }
        var scaled = new BilliardsPreset { id = "s", par = 4, payoutScale = 2f, balls = pre.balls, order = pre.order };
        Check(BilliardsPayout.OnWin(40, BilliardsDifficulty.Medium, scaled, cfg) > BilliardsPayout.OnWin(40, BilliardsDifficulty.Medium, pre, cfg), "payoutScale raises the payout");

        // --- Aim assist maths
        var others = new List<Vector2> { new Vector2(1f, 0.5f) };
        Check(AimAssist.FirstBallContact(new Vector2(0.2f, 0.5f), new Vector2(1, 0), others, 0.035f, out int hi, out float t) && Math.Abs(t - (0.8f - 0.07f)) < 1e-4, "straight sweep distance = gap - 2r");
        AimAssist.Deflections(new Vector2(0.93f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1, 0), out var od, out var cd, out var cut);
        Check(Math.Abs(cut - 1f) < 1e-4, "full hit cut=1 (cue stops)");

        // --- Q2 flag: final ball wrong pocket can be made non-fatal
        var called2 = new Dictionary<string, Pocket> { { "8", Pocket.TR } };
        r = new BilliardsReferee(new[] { "8" }, null, called2, true, false); s = r.Judge(Rep("8", false, ("8", Pocket.BR)));
        Check(s.verdict == Verdict.Continue && s.respot.Contains("8"), "flag off: final wrong pocket = respot");
        r = new BilliardsReferee(new[] { "5", "8" }, null, called2, true, false); s = r.Judge(Rep("5", false, ("8", Pocket.TR)));
        Check(s.verdict == Verdict.Fail, "flag off: final early still fails");
        r = new BilliardsReferee(new[] { "1", "3" }, null, null, false, true); s = r.Judge(Rep("3", false, ("1", Pocket.TL)));
        Check(!s.foul && r.Head == "3", "strict first contact off: hitting 3 first then potting 1 is legal");

        // --- ShotInput quantizing
        var q = ShotInput.Quantized(26.5651f, 0.4567f);
        Check(Math.Abs(q.angle - 26.6f) < 1e-4 && Math.Abs(q.power - 0.46f) < 1e-4, "quantize 26.5651/0.4567 -> 26.6/0.46");
        q = ShotInput.Quantized(190f, 1.5f);
        Check(Math.Abs(q.angle - (-170f)) < 1e-4 && Math.Abs(q.power - 1f) < 1e-6, "quantize wraps 190 -> -170, clamps power");

        // --- FeltMapper: angles are table-space, not normalized
        var fm = new FeltMapper(588f, new TableMirror(false, false));
        var felt = fm.TableToFelt(new Vector2(2f, 1f));
        Check(Math.Abs(felt.x - 588f) < 1e-3 && Math.Abs(felt.y - 294f) < 1e-3, "table (2,1) -> felt (588,294)");
        var rt = fm.FeltToTable(fm.TableToFelt(new Vector2(1.3f, 0.4f)));
        Check(Math.Abs(rt.x - 1.3f) < 1e-4 && Math.Abs(rt.y - 0.4f) < 1e-4, "felt round trip");
        float ang = FeltMapper.AngleDeg(fm.FeltDirToTable(new Vector2(0.4f, 0.2f) * 294f));
        Check(Math.Abs(ang - 26.565f) < 0.01f, "corner aim from (1.6,0.8) is 26.57 deg in table space (not 45)");
        var fmx = new FeltMapper(588f, new TableMirror(true, true));
        var mrt = fmx.FeltToTable(fmx.TableToFelt(new Vector2(0.5f, 0.25f)));
        Check(Math.Abs(mrt.x - 0.5f) < 1e-4 && Math.Abs(mrt.y - 0.25f) < 1e-4, "mirrored felt round trip");
        var mf = fmx.TableToFelt(new Vector2(0.5f, 0.25f));
        Check(Math.Abs(mf.x - (1.5f * 294f)) < 1e-3 && Math.Abs(mf.y - (0.75f * 294f)) < 1e-3, "flipXY draws (0.5,0.25) at (1.5,0.75)");

        // --- GameConfig validation incl. tunneling bound
        var gc = NewConfig(); Check(gc.ValidateBilliards(out _), "default billiards config valid");
        gc.billiardsBallRadius = 0.035f; gc.billiardsMaxShotSpeed = 6f;
        Check(!gc.ValidateBilliards(out var why), "earlier r=0.035/speed=6 rejected: " + why);
        gc = NewConfig(); gc.billiardsCornerPocketRadius = 0.09f;
        Check(!gc.ValidateBilliards(out var whyC), "corner pocket 0.09 (too tight for the jaws) rejected: " + whyC);
        gc = NewConfig(); gc.billiardsSidePocketRadius = 0.075f;
        Check(!gc.ValidateBilliards(out var whyS), "side pocket 0.075 (too tight for the jaws) rejected: " + whyS);
        gc = NewConfig(); gc.billiardsAssumedWinRate = new[] { 0.8f, 0f, 0.5f };
        Check(!gc.ValidateBilliards(out var why2), "win rate of 0 rejected (would divide by zero): " + why2);
        gc = NewConfig(); gc.billiardsAssumedWinRate = new[] { 0.8f, 0.5f };
        Check(!gc.ValidateBilliards(out _), "win rate array with 2 entries rejected");

        // --- Ball sprite mapping
        Check(BallSprites.SpriteNameFor("cue") == "Ball-White" && BallSprites.SpriteNameFor("1") == "A1" && BallSprites.SpriteNameFor("7") == "G7", "solid sprite names");
        Check(BallSprites.SpriteNameFor("8") == "H8" && BallSprites.SpriteNameFor("9") == "A - 1" && BallSprites.SpriteNameFor("11") == "C - 3" && BallSprites.SpriteNameFor("15") == "G - 7", "8-ball and stripe sprite names");

        // --- Pending effects reset clears values and listeners
        int fired = 0; BilliardsPendingEffects.ExtraShots = 2; BilliardsPendingEffects.ExtraShotsConsumed += () => fired++;
        Check(BilliardsPendingEffects.TakeExtraShots() == 2 && fired == 1 && BilliardsPendingEffects.ExtraShots == 0, "extra shots consumed once, event fired");
        BilliardsPendingEffects.CueSlip = true; BilliardsPendingEffects.Reset();
        BilliardsPendingEffects.ExtraShots = 1; BilliardsPendingEffects.TakeExtraShots();
        Check(!BilliardsPendingEffects.CueSlip && fired == 1, "Reset clears flags and old listeners (no leak into next run)");
        BilliardsPendingEffects.Reset();
        Check(BallSprites.Matches("A1_0", "1") && BallSprites.Matches("C - 3_0", "11") && BallSprites.Matches("H8", "8") && !BallSprites.Matches("A1_0", "9"), "sprite match accepts Multiple-mode _0 suffix");

        // --- Task 1.4: CostModifierService enum values match what the card assets store (main added Blackjack at 2)
        Check((int)CostModifierService.Target.Ticket == 0 && (int)CostModifierService.Target.CardPack == 1
              && (int)CostModifierService.Target.Blackjack == 2 && (int)CostModifierService.Target.Billiards == 3,
            "CostModifierService.Target: Ticket=0, CardPack=1, Blackjack=2, Billiards=3 (card assets keep their meaning)");

        // --- Tasks 1.6 / 1.7: shipped presets, loaded through the real library
        var all = BilliardsPresetLibrary.All;
        _log($"loaded {all.Count} preset(s) from Resources/{BilliardsPresetLibrary.ResourcesPath}");
        Check(all.Count >= 2, "both sample presets load");
        BilliardsPreset four = null, straight = null;
        foreach (var p in all) { if (p.id == "four_in_order_01") four = p; if (p.id == "straight_test") straight = p; }
        Check(four != null && Math.Abs(four.payoutScale - 1f) < 1e-6, "four_in_order_01: payoutScale defaults to 1 (field absent from the file)");
        Check(four != null && four.ShotsFor(BilliardsDifficulty.Easy, cfg.billiardsParOffsets) == 8
                           && four.ShotsFor(BilliardsDifficulty.Medium, cfg.billiardsParOffsets) == 6
                           && four.ShotsFor(BilliardsDifficulty.Hard, cfg.billiardsParOffsets) == 4, "four_in_order_01: shots Easy 8 / Medium 6 / Hard 4");
        Check(straight != null && straight.par == 1, "straight_test: par == 1");
        foreach (var p in all)
            Check(BilliardsPresetValidator.Validate(p, cfg.billiardsParOffsets), $"validator clean for preset '{p.id}'");

        if (four != null && straight != null) RunScenarios(cfg, straight, four);

#if !BILLIARDS_HARNESS
        // --- Task 1.3 / 1.2: the real GameConfig asset
        var asset = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Settings/Configs/GameConfig.asset");
        Check(asset != null, "GameConfig.asset loads");
        if (asset != null)
        {
            Check(asset.ValidateBilliards(out var assetWhy), "GameConfig.asset: ValidateBilliards passes" + (assetWhy != null ? " (" + assetWhy + ")" : ""));
            Check(asset.billiardsParOffsets != null && asset.billiardsParOffsets.Length == 3
               && asset.billiardsTargetRate != null && asset.billiardsTargetRate.Length == 3
               && asset.billiardsAssist != null && asset.billiardsAssist.Length == 3,
               "GameConfig.asset: billiards arrays have 3 entries (the asset has really been written with the new fields)");
        }
#endif

        _log($"{_checks - _fails}/{_checks} checks passed");
        return _fails;
    }
}
