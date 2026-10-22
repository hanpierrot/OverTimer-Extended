using System;
using System.Collections.Generic;

/// Replays every stored solution headless and checks it still wins, then checks it is not a knife-edge. Pure C#, so it
/// runs in the Editor, in tests and (via the cheat menu) inside a build. Re-run after ANY physics constant changes.
public static class BilliardsPresetVerifier
{
    public const int   RobustTrials  = 30;
    public const float AngleJitter   = 0.3f;     // degrees, +-, before quantizing: players cannot aim to 0.1
    public const float PowerJitter   = 0.02f;    // +-
    public const float KnifeEdgeBelow = 0.7f;

    public sealed class Row
    {
        public string id;
        public bool pass;
        public bool skipped;
        public string reason;
        public int shotsUsed;
        public bool checkedRobustness;
        public float robust;                     // the WORST single shot's pass rate (0..1)
        public int worstShot;                    // 1-based
        public float fullReplay;                 // pass rate when every shot of the solution is jittered at once
        public bool knifeEdge;
    }

    public static List<Row> VerifyAll(GameConfig cfg) => VerifyAll(cfg, BilliardsPresetLibrary.All);

    public static List<Row> VerifyAll(GameConfig cfg, IEnumerable<BilliardsPreset> presets)
    {
        var rows = new List<Row>();
        foreach (var p in presets) rows.Add(VerifyPreset(p, cfg));
        return rows;
    }

    public static Row VerifyPreset(BilliardsPreset p, GameConfig cfg)
    {
        var row = new Row { id = p.id };

        if (!BilliardsPresetValidator.Validate(p, cfg.billiardsParOffsets, BilliardsSimParams.FromConfig(cfg)))
        {
            row.reason = "validator failed (see warnings)";
            return row;
        }
        if (p.solution == null || p.solution.Length == 0)
        {
            row.skipped = true;
            row.reason = "no solution - skipped";
            return row;
        }

        long hashA = Replay(p, cfg, out string why, out int used);
        row.shotsUsed = used;
        if (why != null) { row.reason = why; return row; }

        long hashB = Replay(p, cfg, out string why2, out _);
        if (why2 != null || hashA != hashB) { row.reason = "replay is not deterministic: " + (why2 ?? "different final state"); return row; }

        row.pass = true;
        row.reason = $"wins on shot {used} of {p.solution.Length}";

        Robustness(p, cfg, out float[] perShot, out row.fullReplay);
        row.checkedRobustness = true;
        row.robust = 1f;
        for (int i = 0; i < perShot.Length; i++)
            if (perShot[i] < row.robust || row.worstShot == 0) { row.robust = perShot[i]; row.worstShot = i + 1; }
        row.knifeEdge = row.robust < KnifeEdgeBelow;
        return row;
    }

    /// A readable report (one line per table plus a total) for the Editor menu, the cheat menu and the console of a build.
    public static string Report(List<Row> rows, out int pass, out int fail, out int skipped, out int knifeEdge)
    {
        var sb = new System.Text.StringBuilder();
        pass = fail = skipped = knifeEdge = 0;
        foreach (var r in rows)
        {
            string status = r.skipped ? "SKIP" : !r.pass ? "FAIL" : r.knifeEdge ? "PASS (knife-edge)" : "PASS";
            sb.Append($"  {status,-18} {r.id,-22} {r.reason}");
            if (r.checkedRobustness)
                sb.Append($"; robust {r.robust * 100f:0}% (worst shot {r.worstShot}), blind replay {r.fullReplay * 100f:0}%");
            sb.AppendLine();
            if (r.skipped) skipped++; else if (!r.pass) fail++; else { pass++; if (r.knifeEdge) knifeEdge++; }
        }
        sb.Append($"  {pass} passed ({knifeEdge} knife-edge), {fail} failed, {skipped} skipped");
        return sb.ToString();
    }

    /// Plays the stored solution on Hard with no mirror. Returns the final state hash; `failure` is null on success,
    /// meaning the round was won on exactly the last solution shot.
    static long Replay(BilliardsPreset p, GameConfig cfg, out string failure, out int shotsUsed)
    {
        var round = new BilliardsRound(p, default, BilliardsDifficulty.Hard, cfg.billiardsBetMax, cfg);
        failure = null;
        shotsUsed = 0;

        for (int i = 0; i < p.solution.Length; i++)
        {
            var shot = ShotInput.Quantized(p.solution[i].angle, p.solution[i].power);
            round.Fire(shot);
            round.Sim.RunToRest();
            var res = round.Resolve();
            shotsUsed = i + 1;
            bool last = i == p.solution.Length - 1;

            if (res.verdict == Verdict.Fail) { failure = $"failed on shot {i + 1}: {res.failReason}"; break; }
            if (res.verdict == Verdict.Win && !last) { failure = $"won early, on shot {i + 1} of {p.solution.Length}"; break; }
            if (res.verdict == Verdict.Continue && last) { failure = $"not won after the last shot (queue head: {round.Referee.Head})"; break; }
        }
        return round.Sim.StateHash();
    }

    // ================= robustness =================

    /// How forgiving is the solution? Two numbers, both from RobustTrials seeded trials:
    ///  perShot[i]  - shot i is fired from the exact state it was recorded from, with its angle jittered by +-AngleJitter
    ///                and its power by +-PowerJitter (then quantized); it passes when the shot does the same thing as the
    ///                recorded one (same pots in the same order, same re-spots, same foul flag, same verdict). Measuring each
    ///                shot alone is what a player faces: they re-aim from wherever the balls actually are.
    ///  fullReplay  - the literal version: jitter EVERY shot of the solution at once and replay it blind. A multi-shot
    ///                solution is open-loop, so this drops fast with length; it is reported but not used to flag a table.
    /// A table whose worst shot passes under KnifeEdgeBelow is a knife-edge: technically solvable, unfair to aim.
    public static void Robustness(BilliardsPreset p, GameConfig cfg, out float[] perShot, out float fullReplay)
    {
        int seed = 17;
        foreach (char c in p.id ?? "") seed = seed * 31 + c;
        var rng = new Random(seed);

        int n = p.solution.Length;
        perShot = new float[n];
        var exact = new ShotInput[n];
        for (int i = 0; i < n; i++) exact[i] = ShotInput.Quantized(p.solution[i].angle, p.solution[i].power);

        var before = new BilliardsRound(p, default, BilliardsDifficulty.Hard, cfg.billiardsBetMax, cfg);
        for (int i = 0; i < n; i++)
        {
            var reference = before.Clone();
            reference.Fire(exact[i]);
            reference.Sim.RunToRest();
            ShotResult want = reference.Resolve();

            int passed = 0;
            for (int t = 0; t < RobustTrials; t++)
            {
                var trial = before.Clone();
                trial.Fire(Jitter(exact[i], rng));
                trial.Sim.RunToRest();
                if (SameOutcome(want, trial.Resolve())) passed++;
            }
            perShot[i] = passed / (float)RobustTrials;
            before = reference;                                   // carry on from the recorded, exact state
        }

        int wins = 0;
        for (int t = 0; t < RobustTrials; t++)
        {
            var round = new BilliardsRound(p, default, BilliardsDifficulty.Hard, cfg.billiardsBetMax, cfg);
            Verdict last = Verdict.Continue;
            for (int i = 0; i < n && last == Verdict.Continue; i++)
            {
                round.Fire(Jitter(exact[i], rng));
                round.Sim.RunToRest();
                last = round.Resolve().verdict;
                if (last == Verdict.Win && i != n - 1) { last = Verdict.Fail; }   // won early: not the recorded solution
            }
            if (last == Verdict.Win) wins++;
        }
        fullReplay = wins / (float)RobustTrials;
    }

    static ShotInput Jitter(ShotInput s, Random rng)
    {
        float a = s.angle + (float)((rng.NextDouble() * 2.0 - 1.0) * AngleJitter);
        float pw = s.power + (float)((rng.NextDouble() * 2.0 - 1.0) * PowerJitter);
        return ShotInput.Quantized(a, pw);
    }

    static bool SameOutcome(ShotResult a, ShotResult b)
    {
        if (a.verdict != b.verdict || a.foul != b.foul) return false;
        if (a.popped.Count != b.popped.Count || a.respot.Count != b.respot.Count) return false;
        for (int i = 0; i < a.popped.Count; i++) if (a.popped[i] != b.popped[i]) return false;
        for (int i = 0; i < a.respot.Count; i++) if (a.respot[i] != b.respot[i]) return false;
        return true;
    }
}
