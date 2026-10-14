using System;
using System.Collections.Generic;

/// Replays every stored solution headless and checks it still wins. Pure C#, so it runs in the Editor,
/// in tests and (via the cheat menu, Phase 7) inside a build. Re-run after ANY physics constant changes.
public static class BilliardsPresetVerifier
{
    public sealed class Row
    {
        public string id;
        public bool pass;
        public bool skipped;
        public string reason;
        public int shotsUsed;
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

        if (!BilliardsPresetValidator.Validate(p, cfg.billiardsParOffsets))
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
        return row;
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
}
