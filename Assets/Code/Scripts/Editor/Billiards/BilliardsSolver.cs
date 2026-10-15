using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// Editor-only authoring helper: brute-forces a solution for a table with the REAL simulation and the real referee.
/// It tries every aim on a fine grid at a few powers, keeps the shots that make clean progress (a legal pot, no foul, no
/// re-spot), ranks them by how forgiving they are (the same +-0.3 degree / +-0.02 power jitter the verifier uses), and
/// searches deeper from the most forgiving ones. Iterative deepening, so the first solution found has the fewest shots.
public static class BilliardsSolver
{
    public sealed class Options
    {
        public int maxShots = 6;
        public int beam = 2;                      // how many of the best shots to explore per stage
        public float angleStep = 0.5f;            // degrees
        public float[] powers = { 0.3f, 0.45f, 0.6f, 0.75f, 0.9f, 1f };
        public int trials = 30;                   // jitter trials per candidate (the verifier uses 30 too)
        public float minRobust = 0.8f;            // candidates below this are dropped; the verifier flags tables under 0.7
        public bool preferFewerShots = true;      // rank wins and bigger combos above robustness (false: most forgiving shot first)
        public bool bankOnly;                     // every potting shot must put the potted ball off a cushion first
        public Action<string> log;
    }

    static readonly Vector2[] PocketCentres =
    {
        BilliardsSim.PocketCentre(Pocket.TL), BilliardsSim.PocketCentre(Pocket.TM), BilliardsSim.PocketCentre(Pocket.TR),
        BilliardsSim.PocketCentre(Pocket.BL), BilliardsSim.PocketCentre(Pocket.BM), BilliardsSim.PocketCentre(Pocket.BR),
    };

    sealed class Candidate
    {
        public ShotInput shot;
        public float robust;
        public int popped;
        public bool win;
        public bool bank;
        public BilliardsRound after;
    }

    public static List<ShotInput> Solve(BilliardsPreset preset, GameConfig cfg, Options o, out string notes)
    {
        var root = new BilliardsRound(preset, default, BilliardsDifficulty.Hard, cfg.billiardsBetMax, cfg, 60);   // plenty of shots: the depth limit is ours
        var cache = new Dictionary<string, List<Candidate>>();
        var sb = new StringBuilder();

        for (int depth = 1; depth <= o.maxShots; depth++)
        {
            o.log?.Invoke($"depth {depth}...");
            var path = new List<Candidate>();
            if (Search(root, path, depth, o, cache, string.Empty))
            {
                var shots = new List<ShotInput>();
                foreach (var c in path)
                {
                    shots.Add(c.shot);
                    sb.AppendLine($"  {c.shot.angle,7:0.0} deg  power {c.shot.power:0.00}  robust {c.robust * 100:0}%  pots {c.popped}{(c.bank ? "  BANK" : "")}{(c.win ? "  WIN" : "")}");
                }
                notes = sb.ToString();
                return shots;
            }
        }
        notes = "no solution within " + o.maxShots + " shots";
        return null;
    }

    /// Solve, verify, and (when it passes) write Resources/BilliardsPresets/<id>.json. Returns a report.
    /// A solution that is a knife-edge is reported but NOT written unless allowKnifeEdge is set.
    public static string SolveAndWrite(BilliardsPreset preset, GameConfig cfg, Options o, bool allowKnifeEdge = false)
    {
        string problem = null;
        if (!BilliardsPresetValidator.Validate(preset, cfg.billiardsParOffsets, BilliardsSimParams.FromConfig(cfg))) problem = "validator failed (see console)";
        if (problem != null) return preset.id + ": " + problem;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var shots = Solve(preset, cfg, o, out string notes);
        if (shots == null)
        {
            string none = $"{preset.id}: {notes} ({sw.Elapsed.TotalSeconds:0}s)\n";
            System.IO.File.AppendAllText("Temp/billiards_solve.log", none);
            return none;
        }

        var copy = JsonUtility.FromJson<BilliardsPreset>(JsonUtility.ToJson(preset));
        copy.par = shots.Count;
        copy.solution = shots.ToArray();
        foreach (var b in copy.balls) { b.x = (float)Math.Round(b.x, 4); b.y = (float)Math.Round(b.y, 4); }
        copy.shots = new int[] { 0, 0, 0 };

        var row = BilliardsPresetVerifier.VerifyPreset(copy, cfg);
        string report = BilliardsPresetVerifier.Report(new List<BilliardsPresetVerifier.Row> { row }, out _, out _, out _, out _);
        string written = "not written";
        if (row.pass && (!row.knifeEdge || allowKnifeEdge))
        {
            written = "written to " + BilliardsPresetJson.Write(copy);
        }
        string result = $"{preset.id}: {shots.Count} shot(s) in {sw.Elapsed.TotalSeconds:0}s, {written}\n{notes}{report}\n";
        System.IO.File.AppendAllText("Temp/billiards_solve.log", result);     // long solves outlive the tool call that started them
        return result;
    }

    static bool Search(BilliardsRound state, List<Candidate> path, int left, Options o, Dictionary<string, List<Candidate>> cache, string key)
    {
        if (left <= 0) return false;
        if (!cache.TryGetValue(key, out var candidates))
        {
            candidates = Candidates(state, o);
            cache[key] = candidates;
        }

        int tried = 0;
        foreach (var c in candidates)
        {
            if (tried >= o.beam) break;
            if (!c.win && left == 1) continue;                    // out of shots and the table is not cleared
            tried++;
            path.Add(c);
            if (c.win) return true;
            if (Search(c.after, path, left - 1, o, cache, key + "|" + c.shot.angle.ToString("0.0") + "," + c.shot.power.ToString("0.00"))) return true;
            path.RemoveAt(path.Count - 1);
        }
        return false;
    }

    /// All clean-progress shots from this state, best first (wins first, then more pots, then more forgiving).
    static List<Candidate> Candidates(BilliardsRound state, Options o)
    {
        var found = new List<Candidate>();
        float r = state.Sim.BallRadius;
        var touched = new bool[state.Sim.BallCount];

        for (float a = -180f; a < 180f; a += o.angleStep)
            foreach (float power in o.powers)
            {
                var shot = ShotInput.Quantized(a, power);
                var attempt = state.Clone();
                var res = Fire(attempt, shot, touched, r);
                var c = Evaluate(attempt, res, shot, touched, o);
                if (c != null) found.Add(c);
            }

        // robustness for the survivors
        var rng = new System.Random(1234);
        var kept = new List<Candidate>();
        foreach (var c in found)
        {
            int ok = 0;
            for (int t = 0; t < o.trials; t++)
            {
                var jittered = ShotInput.Quantized(c.shot.angle + (float)((rng.NextDouble() * 2 - 1) * BilliardsPresetVerifier.AngleJitter),
                                                   c.shot.power + (float)((rng.NextDouble() * 2 - 1) * BilliardsPresetVerifier.PowerJitter));
                var attempt = state.Clone();
                var res = Fire(attempt, jittered, touched, r);
                var other = Evaluate(attempt, res, jittered, touched, o);
                if (other != null && other.popped == c.popped && other.win == c.win) ok++;
            }
            c.robust = ok / (float)o.trials;
            if (c.robust >= o.minRobust) kept.Add(c);
        }

        kept.Sort((x, y) =>
        {
            if (o.preferFewerShots)
            {
                int w = y.win.CompareTo(x.win); if (w != 0) return w;
                int p = y.popped.CompareTo(x.popped); if (p != 0) return p;
            }
            return y.robust.CompareTo(x.robust);
        });

        // keep the list varied: skip near-duplicates of a better shot
        var distinct = new List<Candidate>();
        foreach (var c in kept)
        {
            bool dup = false;
            foreach (var d in distinct)
                if (Mathf.Abs(Mathf.DeltaAngle(c.shot.angle, d.shot.angle)) < 3f && Mathf.Abs(c.shot.power - d.shot.power) < 0.3f) { dup = true; break; }
            if (!dup) distinct.Add(c);
            if (distinct.Count >= 12) break;
        }
        return distinct;
    }

    static Candidate Evaluate(BilliardsRound attempt, ShotResult res, ShotInput shot, bool[] touched, Options o)
    {
        bool clean = res.verdict != Verdict.Fail && !res.foul && res.respot.Count == 0 && res.popped.Count >= 1;
        if (!clean) return null;
        bool bank = false;
        foreach (string id in res.popped) { int i = attempt.Sim.IndexOf(id); if (i >= 0 && touched[i]) bank = true; }
        if (o.bankOnly && !bank) return null;
        return new Candidate { shot = shot, popped = res.popped.Count, win = res.verdict == Verdict.Win, bank = bank, after = attempt };
    }

    /// Fires and runs to rest while noting which balls touched a cushion (centre within a hair of where its edge bounces).
    static ShotResult Fire(BilliardsRound round, ShotInput shot, bool[] touched, float r)
    {
        Array.Clear(touched, 0, touched.Length);
        round.Fire(shot);
        var sim = round.Sim;
        float eps = 0.004f;
        while (sim.Step())
        {
            for (int i = 0; i < sim.BallCount; i++)
            {
                if (sim.IsPotted(i)) continue;
                Vector2 p = sim.Position(i);
                bool nearPocket = false;                          // rolling into a corner along the rail is not a bank
                for (int k = 0; k < PocketCentres.Length; k++)
                    if ((p - PocketCentres[k]).sqrMagnitude < 0.09f) { nearPocket = true; break; }
                if (nearPocket) continue;
                if (p.x <= r + eps || p.x >= FeltMapper.TableW - r - eps || p.y <= r + eps || p.y >= FeltMapper.TableH - r - eps) touched[i] = true;
            }
        }
        return round.Resolve();
    }
}
