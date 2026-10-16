using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Overtime.Blackjack;
using UnityEditor;
using UnityEngine;

/// Phase 9 balance model (Editor only): Tools/OverTimer/Billiards/Balance Report writes BILLIARDS_BALANCE.md.
///
/// What it measures and what it can NOT measure:
///  - A "noisy hands" bot plays every shipped table on the real simulation and the real referee. It follows the recorded
///    solution; when a shot does not do what it should, it re-plans from the actual state (a perfect-sight search that
///    prefers shots that also work two degrees either side). Its hands have an aim error (a normal distribution, in degrees)
///    and a power error. That gives the win rate of a player who SEES the best shot and merely aims imperfectly: an
///    UPPER bound for real players, who also have to find the shot. Real numbers come from the round log (Phase 9.1).
///  - Seconds per shot = the simulated roll time + an ASSUMED aim time. The aim time is a guess until the round log
///    gives the real one.
///  - Mining and Blackjack are computed from the live GameConfig and the real card logic.
public static class BilliardsBalanceReport
{
    public const float AssumedAimSeconds = 3.0f;               // GUESS: replace with the measured aimSeconds from the round log
    static readonly float[] Sigmas = { 0.15f, 0.3f, 0.6f };     // aim error, degrees (1 sigma)
    const float PowerSigma = 0.03f;
    const int Trials = 40;

    [MenuItem("Tools/OverTimer/Billiards/Balance Report")]
    static void Run()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Settings/Configs/GameConfig.asset");
        string problem = "GameConfig.asset not found";
        if (cfg == null || !cfg.ValidateBilliards(out problem)) { Debug.LogError("[Billiards] GameConfig missing or invalid: " + problem); return; }
        try
        {
            string report = Build(cfg, msg => EditorUtility.DisplayProgressBar("Billiards balance", msg, 0.5f));
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "BILLIARDS_BALANCE.md");
            File.WriteAllText(path, report);
            Debug.Log("[Billiards] balance report written to " + path);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // ================= the bot =================

    sealed class TableResult
    {
        public BilliardsPreset preset;
        public float sigma;
        public int[] winAt;            // per trial: 1-based shot of the win, -1 = none
        public int[] failAt;           // per trial: 1-based shot where the final ball dropped early / a fatal foul, -1 = none
        public float rollSeconds;      // mean simulated roll time of one shot
    }

    static string Signature(ShotResult r) =>
        $"{r.verdict}|{r.foul}|{string.Join(",", r.popped)}|{string.Join(",", r.respot)}";

    static TableResult PlayTable(BilliardsPreset p, GameConfig cfg, float sigma, int trials)
    {
        int maxShots = p.par + cfg.billiardsParOffsets[0];                     // the most any difficulty grants
        var solution = new ShotInput[p.solution.Length];
        for (int i = 0; i < solution.Length; i++) solution[i] = ShotInput.Quantized(p.solution[i].angle, p.solution[i].power);

        // what each recorded shot does from the exact recorded state
        var reference = new string[solution.Length];
        var exact = new BilliardsRound(p, default, BilliardsDifficulty.Hard, cfg.billiardsBetMax, cfg, 60);
        for (int i = 0; i < solution.Length; i++)
        {
            exact.Fire(solution[i]); exact.Sim.RunToRest();
            reference[i] = Signature(exact.Resolve());
        }

        var result = new TableResult { preset = p, sigma = sigma, winAt = new int[trials], failAt = new int[trials] };
        var rollTotals = new double[trials];
        var rollCounts = new int[trials];

        Parallel.For(0, trials, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, t =>
        {
            var rng = new System.Random(9000 + t * 31 + (int)(sigma * 100));
            var round = new BilliardsRound(p, default, BilliardsDifficulty.Hard, cfg.billiardsBetMax, cfg, 60);
            bool onPath = true;
            result.winAt[t] = -1; result.failAt[t] = -1;

            for (int shot = 0; shot < maxShots; shot++)
            {
                // The player has the plan and the ghost-ball assist: they adapt the planned shot to where the balls really are
                // (the cue ball is never exactly where the recording left it), or fall back to the best shot they can see.
                ShotInput intended = onPath && shot < solution.Length ? Adapt(round, solution[shot], reference[shot]) : null;
                if (intended == null) intended = Replan(round);
                var noisy = ShotInput.Quantized(intended.angle + (float)Gauss(rng) * sigma,
                                                intended.power + (float)Gauss(rng) * PowerSigma);
                round.Fire(noisy);
                round.Sim.RunToRest();
                rollTotals[t] += round.Sim.StepsThisShot * BilliardsSim.FixedDt;
                rollCounts[t]++;
                var res = round.Resolve();

                if (res.verdict == Verdict.Win) { result.winAt[t] = shot + 1; break; }
                if (res.verdict == Verdict.Fail) { result.failAt[t] = shot + 1; break; }
                if (onPath && (shot >= solution.Length || Signature(res) != reference[shot])) onPath = false;
            }
        });

        double total = 0; int count = 0;
        for (int t = 0; t < trials; t++) { total += rollTotals[t]; count += rollCounts[t]; }
        result.rollSeconds = count > 0 ? (float)(total / count) : 0f;
        return result;
    }

    static double Gauss(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// The recorded shot, adjusted to the balls' real positions: scans +-10 degrees around the recorded angle (0.25 steps) at a
    /// few nearby powers, and aims at the CENTRE of the widest range of angles that still does what the recording did.
    /// That is what a player lining up the ghost ball does. Returns null when nothing near the recording works any more.
    static ShotInput Adapt(BilliardsRound state, ShotInput recorded, string wanted)
    {
        float[] dPower = { 0f, -0.1f, 0.1f, -0.2f, 0.2f };
        const int Steps = 81;                                                   // 20 degrees at 0.25
        ShotInput best = null; int bestRun = 0;
        foreach (float dp in dPower)
        {
            float power = Mathf.Clamp(recorded.power + dp, 0.2f, 1f);
            int runStart = -1;
            for (int i = 0; i <= Steps; i++)
            {
                bool ok = false;
                if (i < Steps)
                {
                    var c = state.Clone();
                    c.Fire(ShotInput.Quantized(recorded.angle - 10f + i * 0.25f, power));
                    c.Sim.RunToRest();
                    ok = Signature(c.Resolve()) == wanted;
                }
                if (ok && runStart < 0) runStart = i;
                if ((!ok || i == Steps) && runStart >= 0)
                {
                    int len = i - runStart;
                    if (len > bestRun) { bestRun = len; best = ShotInput.Quantized(recorded.angle - 10f + (runStart + (len - 1) * 0.5f) * 0.25f, power); }
                    runStart = -1;
                }
            }
        }
        return best;
    }

    /// The shot a player who can see everything would pick from this state: the best outcome the referee allows (a win, then
    /// the most pots, then anything legal), aimed at the CENTRE of the widest run of angles that give it (0.5 degree steps,
    /// five powers). Not a global plan: it does not think about where the cue ball ends up, so it is a little weaker than a
    /// good player and a little stronger than a careless one.
    static ShotInput Replan(BilliardsRound state)
    {
        const int Angles = 720;                                                 // 0.5 degree steps
        float[] powers = { 0.35f, 0.5f, 0.65f, 0.8f, 1f };
        var score = new float[powers.Length][];
        float top = float.NegativeInfinity;
        for (int k = 0; k < powers.Length; k++)
        {
            score[k] = new float[Angles];
            for (int a = 0; a < Angles; a++)
            {
                var c = state.Clone();
                c.Fire(ShotInput.Quantized(-180f + a * 0.5f, powers[k]));
                c.Sim.RunToRest();
                var res = c.Resolve();
                float s = res.verdict == Verdict.Win ? 1000f
                        : res.verdict == Verdict.Fail || res.foul ? -100f
                        : res.respot.Count > 0 ? -10f
                        : res.popped.Count > 0 ? 100f + 10f * res.popped.Count
                        : 5f;                                                   // legal but nothing potted
                score[k][a] = s;
                if (s > top) top = s;
            }
        }

        int bestLen = 0, bestStart = 0, bestK = 0;
        for (int k = 0; k < powers.Length; k++)
        {
            for (int start = 0; start < Angles; start++)
            {
                if (score[k][start] < top || score[k][(start + Angles - 1) % Angles] >= top) continue;        // only at the start of a run
                int len = 0;
                while (len < Angles && score[k][(start + len) % Angles] >= top) len++;
                if (len > bestLen) { bestLen = len; bestStart = start; bestK = k; }
            }
        }
        if (bestLen == 0) return ShotInput.Quantized(0f, 0.5f);
        float centre = -180f + ((bestStart + (bestLen - 1) * 0.5f) % Angles) * 0.5f;
        return ShotInput.Quantized(centre, powers[bestK]);
    }

    // ================= comparators =================

    static void MiningRates(GameConfig cfg, out float payoutPerVein, out float hitsPerVein, out float perfect, out float relaxed)
    {
        var blocks = new List<BlockTypeDefinition>();
        foreach (var guid in AssetDatabase.FindAssets("t:BlockConfig"))
        {
            var bc = AssetDatabase.LoadAssetAtPath<BlockConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (bc != null && bc.blocks != null && bc.blocks.Length > 0) { blocks = new List<BlockTypeDefinition>(bc.blocks); break; }
        }
        double pay = 0, hits = 0; int n = 0;
        for (int len = cfg.veinLengthMin; len <= cfg.veinLengthMax; len++, n++)
        {
            for (int i = 0; i < len && i < blocks.Count; i++)
            {
                pay += i < cfg.blockPayout.Length ? cfg.blockPayout[i] : 0;
                hits += Mathf.CeilToInt(blocks[i].hp / (float)Mathf.Max(1, blocks[i].pickaxeDamage));
            }
        }
        payoutPerVein = (float)(pay / n);
        hitsPerVein = (float)(hits / n);
        perfect = payoutPerVein / (hitsPerVein * 0.7f + cfg.veinRespawn);       // hitCooldown 0.7 s: the fastest possible swing rate
        relaxed = payoutPerVein / (hitsPerVein * 1.0f + cfg.veinRespawn);       // one swing a second, a comfortable pace
    }

    /// Basic-strategy bot against the real BlackjackGame. Returns the mean CLOCK-seconds won per hand (ante is paid in clock
    /// seconds, payouts come as money, $1 = 1 s through the meter).
    static float BlackjackNetPerHand(GameConfig cfg, int hands, out float winRate)
    {
        var rng = new System.Random(4242);
        var game = new BlackjackGame(rng);
        double net = 0; int wins = 0;
        for (int h = 0; h < hands; h++)
        {
            game.Reset();
            double spent = cfg.blackjackAnte;
            game.Deal();
            if (game.State == HandState.PlayerTurn)
            {
                int up = game.DealerUpValue;
                while (game.State == HandState.PlayerTurn)
                {
                    int v = game.PlayerValue; bool soft = BlackjackGame.IsSoft(game.PlayerHand);
                    bool canDouble = game.PlayerHand.Count == 2;
                    if (canDouble && !soft && (v == 11 || (v == 10 && up <= 9))) { game.DoubleDown(); spent += cfg.blackjackAnte; }
                    else if (v >= 17 || (v >= 13 && up <= 6) || (v == 12 && up >= 4 && up <= 6)) game.Stand();
                    else game.Hit();
                }
            }
            int payout = game.Payout(cfg.blackjackWinPayout, cfg.blackjackPushPayout, cfg.blackjackNaturalPayout);
            net += payout - spent;
            if (game.Result == Outcome.PlayerWin || game.Result == Outcome.DealerBust || game.Result == Outcome.PlayerNatural) wins++;
        }
        winRate = wins / (float)hands;
        return (float)(net / hands);
    }

    // ================= the report =================

    public static string Build(GameConfig cfg, Action<string> progress)
    {
        var tables = new List<BilliardsPreset>();
        foreach (var p in BilliardsPresetLibrary.All)
            if (p.solution != null && p.solution.Length > 0 && p.id != "straight_test") tables.Add(p);      // the test table is not a player table

        // play every table at every aim error
        var results = LoadOrPlay(tables, cfg, progress);

        var sb = new StringBuilder();
        var ic = CultureInfo.InvariantCulture;
        sb.AppendLine("# Billiards balance report (Phase 9)");
        sb.AppendLine();
        sb.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm} from `GameConfig.asset` and {tables.Count} tables. **Nothing in `GameConfig.asset` was changed by this report.**");
        sb.AppendLine();
        sb.AppendLine("## How to read the numbers");
        sb.AppendLine();
        sb.AppendLine("- **Win rate** comes from a bot that follows each table's recorded solution and re-plans from the real state after a miss (it sees the best shot; its hands are imperfect). It is an UPPER bound for real players, who also have to find the shot. The real numbers come from the in-game round log (cheat menu: `Billiards: round log`, or `Tools/OverTimer/Billiards/Round Log Summary`).");
        sb.AppendLine($"- **Aim error** is one standard deviation of the aim, in degrees ({Trials} trials per table and error level; power error {PowerSigma:0.00}). Aiming is limited by the mouse: a 90-unit pull moves the aim 0.6 degrees per canvas unit at 960 wide and 0.3 at the 1920 build, and over a long shot an aim error is multiplied by about 1 / (2 x ball radius) at the contact, so 0.6 degrees at 1 unit of range already moves the object ball 5 degrees. **0.15 = a patient player at the 1920 build, 0.3 = a typical player, 0.6 = careless**.");
        sb.AppendLine($"- **Seconds per shot** = the simulated roll time + an ASSUMED {AssumedAimSeconds:0.0} s aim time (a guess: measure it, then set `billiardsSecondsPerShot`).");
        sb.AppendLine("- **$/s** is the expected NET money per second of play, losses included, before the 20% ITRS bill (Billiards is taxed on profit only; Mining on everything). Money is clock: $1 = 1 s, and the clock runs down 1 s per second, so anything under 1 $/s loses ground.");
        sb.AppendLine();

        // roll time
        double rollSum = 0; int rollN = 0;
        foreach (var r in results) { rollSum += r.rollSeconds; rollN++; }
        float roll = (float)(rollSum / rollN);
        float shotSeconds = roll + AssumedAimSeconds;

        var typical = results.FindAll(x => x.sigma == TypicalSigma);

        // ---- per difficulty at each aim error
        sb.AppendLine("## 1. Win rate by difficulty: the bot (all tables averaged)");
        sb.AppendLine();
        sb.AppendLine($"Mean simulated roll per shot: **{roll:0.0} s**, so a shot cycle is about **{shotSeconds:0.0} s** at the bot's assumed {AssumedAimSeconds:0.0} s aim time (config says `billiardsSecondsPerShot` = {cfg.billiardsSecondsPerShot:0.0}: a person thinks slower than the bot).");
        sb.AppendLine();
        sb.AppendLine("| aim error | difficulty | shots granted | assumed win (config) | bot win | mean shots played | bot seconds/round |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        var modelWin = new Dictionary<(float, int), float>();
        foreach (float sigma in Sigmas)
            for (int d = 0; d < 3; d++)
            {
                double winSum = 0, shotsSum = 0, secSum = 0, grantedSum = 0; int tablesN = 0;
                foreach (var r in results)
                {
                    if (r.sigma != sigma) continue;
                    Stats(r, d, cfg, out float p, out float meanShots, out int granted);
                    winSum += p; shotsSum += meanShots; secSum += meanShots * (r.rollSeconds + AssumedAimSeconds); grantedSum += granted; tablesN++;
                }
                float mw = (float)(winSum / tablesN);
                modelWin[(sigma, d)] = mw;
                sb.AppendLine($"| {sigma:0.00}° | {(BilliardsDifficulty)d} | {grantedSum / tablesN:0.0} | {cfg.billiardsAssumedWinRate[d] * 100:0}% | {mw * 100:0}% | {shotsSum / tablesN:0.0} | {secSum / tablesN:0} |");
            }
        sb.AppendLine();

        // ---- per table
        sb.AppendLine($"## 2. Per table at a typical hand ({TypicalSigma:0.0} degrees)");
        sb.AppendLine();
        sb.AppendLine("| table | par | shots E/M/H | bot win E/M/H | roll s/shot | payout at $10 E/M/H |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var r in typical)
        {
            var p = r.preset;
            var wins = new string[3]; var pays = new string[3]; var shots = new string[3];
            for (int d = 0; d < 3; d++)
            {
                Stats(r, d, cfg, out float pw, out _, out int granted);
                wins[d] = $"{pw * 100:0}%";
                pays[d] = "$" + BilliardsPayout.OnWin(10, (BilliardsDifficulty)d, p, cfg);
                shots[d] = granted.ToString();
            }
            sb.AppendLine($"| {p.id} | {p.par} | {string.Join("/", shots)} | {string.Join(" / ", wins)} | {r.rollSeconds:0.0} | {string.Join(" / ", pays)} |");
        }
        sb.AppendLine();

        // ---- comparators
        MiningRates(cfg, out float vein, out float hits, out float miningPerfect, out float miningRelaxed);
        float bjNet = BlackjackNetPerHand(cfg, 100000, out float bjWin);
        float bjSeconds = cfg.blackjackRevealDelay + cfg.blackjackResultDelay + 2f * 1.5f + 1f;     // reveal + result + two decisions + dealing
        float itrs = cfg.billRate;
        sb.AppendLine("## 3. The benchmarks: Mining and Blackjack (same GameConfig)");
        sb.AppendLine();
        sb.AppendLine("| activity | gross $/s | after the 20% ITRS bill | notes |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine($"| Mining, perfect swing rate | {miningPerfect:0.00} | {miningPerfect * (1 - itrs):0.00} | mean vein pays ${vein:0.0} for {hits:0.0} hits; fastest swing 0.7 s; respawn {cfg.veinRespawn:0.0} s. Needs no skill, only clicking |");
        sb.AppendLine($"| Mining, relaxed (1 swing/s) | {miningRelaxed:0.00} | {miningRelaxed * (1 - itrs):0.00} | |");
        sb.AppendLine($"| Blackjack, basic strategy | {(bjNet / bjSeconds).ToString("+0.00;-0.00", ic)} | n/a | {bjNet.ToString("+0.00;-0.00", ic)} clock-seconds per hand over {bjSeconds:0.0} s ({bjWin * 100:0}% of hands won); ante {cfg.blackjackAnte:0} s, win {cfg.blackjackWinPayout}, push {cfg.blackjackPushPayout}, natural {cfg.blackjackNaturalPayout}. It does not earn (the Blackjack rebalance is still pending), so Mining is the benchmark |");
        sb.AppendLine();

        // ---- sensitivity of the CURRENT config
        float[] skills = { 1.0f, 0.8f, 0.6f };
        float[] paces = { shotSeconds, 7f, 10f };
        sb.AppendLine("## 4. What the CURRENT config pays: expected net $/s at a $40 bet");
        sb.AppendLine();
        sb.AppendLine("The bot is an upper bound, so players are modelled as a fraction of its win rate (skill) who use proportionally more shots, at a given thinking pace (seconds per shot). Compare with Mining: relaxed " + miningRelaxed.ToString("0.00", ic) + ", perfect " + miningPerfect.ToString("0.00", ic) + " $/s gross.");
        sb.AppendLine();
        AppendSensitivity(sb, typical, cfg, cfg.billiardsAssumedWinRate, cfg.billiardsTargetRate, cfg.billiardsSecondsPerShot, skills, paces, ic);
        sb.AppendLine();

        // ---- affordability
        sb.AppendLine("## 5. Affordability (9.4)");
        sb.AppendLine();
        sb.AppendLine($"- Start: ${cfg.startMoney}, clock {cfg.startClock:0} s, win at {cfg.winClock:0} s. Minimum bet ${cfg.billiardsBetMin} = {cfg.billiardsBetMin * 100 / cfg.startMoney}% of the starting money, so the first bet is affordable from second one and the player can lose {cfg.startMoney / cfg.billiardsBetMin - 1} bets in a row and still play.");
        sb.AppendLine($"- The laptop is locked until the wire puzzle (${cfg.wireTaskMoneyReward}) is solved, so the earliest possible first round is after that puzzle, whatever the bet.");
        sb.AppendLine($"- A round costs the clock about {shotSeconds * 3:0}-{7f * 5:0} s (3 shots at the bot's pace up to 5 shots at 7 s) of the 120 s start clock. The clock runs down 1 s per second whatever you do, so the real question is the $/s above, not the entry price: the money only helps if it comes back faster than 1 $/s.");
        sb.AppendLine();

        // ---- recommendation: solve the targets for a typical player, show what the bot would earn
        float[] assumed = new float[3];
        for (int d = 0; d < 3; d++) assumed[d] = Mathf.Clamp(Mathf.Round(TypicalSkill * modelWin[(TypicalSigma, d)] / 0.05f) * 0.05f, 0.2f, 0.95f);
        float[] goal = { 0.4f * miningRelaxed, 0.9f * miningRelaxed, 0.5f * (miningRelaxed + miningPerfect) };
        float[] target = new float[3];
        for (int d = 0; d < 3; d++)
        {
            float ev0 = HumanRate(typical, d, cfg, assumed, 0f, TypicalSkill, TypicalPace, TypicalPace);
            float ev1 = HumanRate(typical, d, cfg, assumed, 1f, TypicalSkill, TypicalPace, TypicalPace);
            target[d] = Mathf.Round(Mathf.Max(0.1f, (goal[d] - ev0) / (ev1 - ev0)) * 10f) / 10f;
        }
        sb.AppendLine("## 6. Recommended values (for sign-off; none applied)");
        sb.AppendLine();
        sb.AppendLine("| field | now | recommended | why |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine($"| `billiardsSecondsPerShot` | {cfg.billiardsSecondsPerShot:0.0} | **keep {cfg.billiardsSecondsPerShot:0.0} until measured**, then the logged `shotSeconds` | the bot needs {shotSeconds:0.0} s a shot (roll {roll:0.0} s + {AssumedAimSeconds:0.0} s aim); a person who looks for the shot needs more. 7 is a plausible human pace, not a measurement |");
        sb.AppendLine($"| `billiardsAssumedWinRate` | {string.Join(", ", Array.ConvertAll(cfg.billiardsAssumedWinRate, x => x.ToString("0.00", ic)))} | **{string.Join(", ", Array.ConvertAll(assumed, x => x.ToString("0.00", ic)))}** | {TypicalSkill * 100:0}% of the bot's win rate at a typical hand ({modelWin[(TypicalSigma, 0)] * 100:0}% / {modelWin[(TypicalSigma, 1)] * 100:0}% / {modelWin[(TypicalSigma, 2)] * 100:0}%). Compare with the current values: a small change at most. Replace with the logged win rates after the playtest |");
        sb.AppendLine($"| `billiardsTargetRate` | {string.Join(", ", Array.ConvertAll(cfg.billiardsTargetRate, x => x.ToString("0.0", ic)))} | **{string.Join(", ", Array.ConvertAll(target, x => x.ToString("0.0", ic)))}** | solved so that a typical player ({TypicalSkill * 100:0}% skill, {TypicalPace:0} s per shot) earns **{goal[0]:0.0} / {goal[1]:0.0} / {goal[2]:0.0} $/s** net: Easy 40% of relaxed Mining, Medium 90% of it, Hard halfway between relaxed and perfect Mining (\"attractive, not dominant\"). Compare what the current numbers pay that same typical player (section 4) |");
        sb.AppendLine();
        sb.AppendLine("What those recommended values pay (expected net $/s at a $40 bet):");
        sb.AppendLine();
        AppendSensitivity(sb, typical, cfg, assumed, target, cfg.billiardsSecondsPerShot, skills, paces, ic);
        sb.AppendLine();
        float botHard = HumanRate(typical, 2, cfg, assumed, target[2], 1f, shotSeconds, cfg.billiardsSecondsPerShot);
        sb.AppendLine($"A perfect, fast player (100% skill, {shotSeconds:0.0} s a shot) would earn {botHard.ToString("0.0", ic)} $/s on Hard with the recommended targets, {(botHard > miningPerfect ? "above" : "below")} perfect Mining ({miningPerfect:0.0} $/s). That is the ceiling of the skill reward; if you want it lower, scale the Hard target down.");
        sb.AppendLine();
        // ---- the table spread: players see the table BEFORE picking a difficulty
        sb.AppendLine("## 7. The table spread: players can pick the difficulty after seeing the table");
        sb.AppendLine();
        sb.AppendLine("The racked screen shows the table and the three payouts, and the payout depends only on the difficulty, not on the table. But the bot's win rate varies enormously between tables (section 2). Expected net $/s for a typical player (80% skill, 7 s a shot) at a $40 bet with the CURRENT config:");
        sb.AppendLine();
        sb.AppendLine("| table | Easy | Medium | Hard | best pick |");
        sb.AppendLine("|---|---|---|---|---|");
        double fixedMedium = 0, picked = 0, fixedHard = 0;
        foreach (var r in typical)
        {
            var rate = new float[3];
            for (int d = 0; d < 3; d++) rate[d] = TableRate(r, d, cfg, cfg.billiardsAssumedWinRate, cfg.billiardsTargetRate[d], TypicalSkill, TypicalPace, cfg.billiardsSecondsPerShot);
            int best = rate[0] >= rate[1] && rate[0] >= rate[2] ? 0 : rate[1] >= rate[2] ? 1 : 2;
            fixedMedium += rate[1]; fixedHard += rate[2]; picked += rate[best];
            sb.AppendLine($"| {r.preset.id} | {rate[0].ToString("+0.0;-0.0", ic)} | {rate[1].ToString("+0.0;-0.0", ic)} | {rate[2].ToString("+0.0;-0.0", ic)} | {(BilliardsDifficulty)best} {rate[best].ToString("+0.0;-0.0", ic)} |");
        }
        sb.AppendLine();
        sb.AppendLine($"A player who always plays Medium earns {(fixedMedium / typical.Count).ToString("+0.0;-0.0", ic)} $/s on average, always Hard {(fixedHard / typical.Count).ToString("+0.0;-0.0", ic)}; a player who **looks at the table and picks the best difficulty earns {(picked / typical.Count).ToString("+0.0;-0.0", ic)} $/s**, and never has to take the table where Hard loses money (`blocker_01`: about 10% for the bot). Looking before choosing is worth {((picked - Math.Max(fixedMedium, fixedHard)) / typical.Count).ToString("+0.0;-0.0", ic)} $/s over the best fixed difficulty.");
        sb.AppendLine();
        sb.AppendLine("**Recommended fix (not applied; needs your call, it touches the preset format and `BilliardsPayout`):** store a measured win rate per table and difficulty in each preset (`winRates`, three numbers, written by this tool at 80% of the bot's typical-hand rate, rounded to 5%) and let `BilliardsPayout.OnWin` use it instead of the single `billiardsAssumedWinRate[difficulty]` when it is present. Then every table pays for how hard it really is, and picking the difficulty after seeing the table stops being free money. Until then, the per-difficulty config values only fit the AVERAGE table.");
        sb.AppendLine();
        sb.AppendLine("## Still needs a human");
        sb.AppendLine();
        sb.AppendLine("1. At least 20 real rounds across all three difficulties (more than one player), read from the round log; set `billiardsSecondsPerShot` to the measured mean `shotSeconds`, replace the assumed win rates with the logged ones, and re-solve the targets (this report does it).");
        sb.AppendLine("2. Re-run this report after changing any value (`Tools/OverTimer/Billiards/Balance Report`); it overwrites this file.");
        sb.AppendLine("3. Update the design docs (plan task 9.5) and sign off the final checkpoint.");
        return sb.ToString();
    }

    const float TypicalSigma = 0.3f;          // degrees: a typical player's aim error
    const float TypicalSkill = 0.8f;          // fraction of the bot's win rate a typical player reaches (a guess until the playtest)
    const float TypicalPace = 7f;             // seconds per shot of a typical player (a guess until the playtest)

    /// Bot win rate and mean shots played for a table at a difficulty.
    static void Stats(TableResult r, int d, GameConfig cfg, out float p, out float meanShots, out int granted)
    {
        granted = r.preset.par + cfg.billiardsParOffsets[d];
        int wins = 0; double played = 0;
        for (int t = 0; t < r.winAt.Length; t++)
        {
            if (r.winAt[t] > 0 && r.winAt[t] <= granted) { wins++; played += r.winAt[t]; }
            else if (r.failAt[t] > 0 && r.failAt[t] <= granted) played += r.failAt[t];
            else played += granted;
        }
        p = wins / (float)r.winAt.Length;
        meanShots = (float)(played / r.winAt.Length);
    }

    /// Expected net $/s at a $40 bet for a player of the given skill (a fraction of the bot's win rate; the rest of their
    /// shots are spent, so they use proportionally more) and pace, over all tables, with the payouts a config would give
    /// (assumed win rates, target rate x, and the seconds-per-shot the payout formula uses).
    static float HumanRate(List<TableResult> tables, int d, GameConfig cfg, float[] assumed, float x, float skill, float pace, float payoutSecondsPerShot)
    {
        double sum = 0;
        foreach (var r in tables) sum += TableRate(r, d, cfg, assumed, x, skill, pace, payoutSecondsPerShot);
        return (float)(sum / tables.Count);
    }

    /// The same for ONE table.
    static float TableRate(TableResult r, int d, GameConfig cfg, float[] assumed, float x, float skill, float pace, float payoutSecondsPerShot)
    {
        const int Bet = 40;
        Stats(r, d, cfg, out float pBot, out float meanShots, out int granted);
        float p = skill * pBot;
        float shots = meanShots + (1f - skill) * (granted - meanShots);
        float expectedShots = (r.preset.par + granted) * 0.5f;
        float expectedNet = x * expectedShots * payoutSecondsPerShot * r.preset.payoutScale;
        float payout = Bet * (1f + expectedNet / cfg.billiardsBetMax) / assumed[d];
        return (p * payout - Bet) / (shots * pace);
    }

    // ================= cache: the bot takes minutes, the report maths takes milliseconds =================

    const int BotVersion = 3;                  // bump when the bot or the noise model changes

    static string CacheKey(List<BilliardsPreset> tables, GameConfig cfg)
    {
        var sb = new StringBuilder();
        sb.Append(BotVersion).Append('|').Append(Trials).Append('|').Append(PowerSigma).Append('|').Append(string.Join(",", Sigmas));
        sb.Append('|').Append(cfg.billiardsBallRadius).Append(',').Append(cfg.billiardsCornerPocketRadius).Append(',').Append(cfg.billiardsSidePocketRadius)
          .Append(',').Append(cfg.billiardsMaxShotSpeed).Append(',').Append(cfg.billiardsRollingDecel).Append(',').Append(cfg.billiardsBallRestitution)
          .Append(',').Append(cfg.billiardsCushionRestitution).Append(',').Append(cfg.billiardsStopSpeed).Append(',').Append(string.Join(",", cfg.billiardsParOffsets));
        foreach (var t in tables) sb.Append('|').Append(BilliardsPresetJson.ToJson(t));
        uint h = 2166136261;
        foreach (char c in sb.ToString()) { h ^= c; h *= 16777619; }
        return h.ToString("x8");
    }

    static List<TableResult> LoadOrPlay(List<BilliardsPreset> tables, GameConfig cfg, Action<string> progress)
    {
        const string CachePath = "Temp/billiards_bot_cache.txt";
        string key = CacheKey(tables, cfg);
        var results = new List<TableResult>();
        try
        {
            if (File.Exists(CachePath))
            {
                var lines = File.ReadAllLines(CachePath);
                if (lines.Length > 1 && lines[0] == key)
                {
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var f = lines[i].Split(';');
                        var preset = tables.Find(x => x.id == f[0]);
                        if (preset == null) { results.Clear(); break; }
                        results.Add(new TableResult
                        {
                            preset = preset, sigma = float.Parse(f[1], CultureInfo.InvariantCulture), rollSeconds = float.Parse(f[2], CultureInfo.InvariantCulture),
                            winAt = Array.ConvertAll(f[3].Split(','), int.Parse), failAt = Array.ConvertAll(f[4].Split(','), int.Parse),
                        });
                    }
                    if (results.Count == tables.Count * Sigmas.Length) return results;
                    results.Clear();
                }
            }
        }
        catch (Exception ex) { Debug.LogWarning("[Billiards] bot cache unreadable, replaying: " + ex.Message); results.Clear(); }

        foreach (float sigma in Sigmas)
            foreach (var p in tables)
            {
                progress?.Invoke($"{p.id}, aim error {sigma:0.00} degrees...");
                results.Add(PlayTable(p, cfg, sigma, Trials));
            }

        try
        {
            var lines = new List<string> { key };
            foreach (var r in results)
                lines.Add($"{r.preset.id};{r.sigma.ToString(CultureInfo.InvariantCulture)};{r.rollSeconds.ToString(CultureInfo.InvariantCulture)};{string.Join(",", r.winAt)};{string.Join(",", r.failAt)}");
            File.WriteAllLines(CachePath, lines);
        }
        catch (Exception ex) { Debug.LogWarning("[Billiards] could not write the bot cache: " + ex.Message); }
        return results;
    }

    static void AppendSensitivity(StringBuilder sb, List<TableResult> tables, GameConfig cfg, float[] assumed, float[] target, float payoutSecondsPerShot,
                                  float[] skills, float[] paces, CultureInfo ic)
    {
        sb.AppendLine("| difficulty | skill | " + string.Join(" | ", Array.ConvertAll(paces, s => $"{s:0.0} s/shot")) + " |");
        sb.AppendLine("|---|---|" + string.Join("|", Array.ConvertAll(paces, s => "---")) + "|");
        for (int d = 0; d < 3; d++)
            foreach (float skill in skills)
            {
                var cells = new string[paces.Length];
                for (int i = 0; i < paces.Length; i++)
                    cells[i] = HumanRate(tables, d, cfg, assumed, target[d], skill, paces[i], payoutSecondsPerShot).ToString("+0.00;-0.00", ic);
                sb.AppendLine($"| {(BilliardsDifficulty)d} (target {target[d]:0.0}) | {skill * 100:0}% | {string.Join(" | ", cells)} |");
            }
    }
}
