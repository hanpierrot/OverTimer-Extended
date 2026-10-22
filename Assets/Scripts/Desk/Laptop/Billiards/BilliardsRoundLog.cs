using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// Dev-only measurement of real play (Phase 9): one line per finished round, for the balance playtests.
/// Active only in the Editor and in Development Builds (Debug.isDebugBuild); in a release build every call returns at once.
///
/// What is measured, in REAL seconds (Time.unscaledTime, so the game's clock speed cards and Time.timeScale do not matter):
///   seconds       from the moment Play was accepted to the result
///   aimSeconds    mean time between a shot becoming available (Aiming) and that shot being fired
///   shotSeconds   mean time of a whole shot cycle: aiming + the ball rolling + the verdict (this is what
///                 GameConfig.billiardsSecondsPerShot means)
///   net           money gained minus what Play cost (a win returns stake + profit; a loss or forfeit is -charge)
/// Rounds are appended to billiards_rounds.csv in Application.persistentDataPath (not on WebGL) and kept in memory.
///
/// Deliberate exception to "reset statics on every scene load": the in-memory list survives a Restart so a playtest
/// of several runs adds up. It never affects gameplay, only observes it.
public static class BilliardsRoundLog
{
    public enum Outcome { Win, Lost, Forfeit, Abandoned }

    public sealed class Entry
    {
        public string time, presetId, difficulty, outcome;
        public int bet, charge, shotsGranted, shotsUsed, net;
        public float seconds, aimSeconds, shotSeconds;
    }

    public const string FileName = "billiards_rounds.csv";
    const string Header = "time,preset,difficulty,bet,charge,shotsGranted,shotsUsed,seconds,aimSeconds,shotSeconds,outcome,net";

    static readonly List<Entry> Memory = new List<Entry>();
    public static IReadOnlyList<Entry> Entries => Memory;

    // the round in progress
    static bool _active;
    static string _preset, _difficulty;
    static int _bet, _charge, _granted, _shots;
    static float _startedAt, _aimStartedAt, _aimTotal, _cycleTotal;
    static bool _aiming;

    public static bool Enabled => Debug.isDebugBuild;
    public static bool RoundActive => _active;

    /// Tests switch this off so they do not add rounds to the real playtest file.
    public static bool WriteToFile = true;

    public static void ClearSession() { Memory.Clear(); _active = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void NewSession() { Memory.Clear(); _active = false; }

    /// Play was accepted and paid for.
    public static void Begin(string presetId, string difficulty, int bet, int charge, int shotsGranted)
    {
        if (!Enabled) return;
        _active = true; _aiming = false;
        _preset = presetId; _difficulty = difficulty; _bet = bet; _charge = charge; _granted = shotsGranted;
        _shots = 0; _aimTotal = 0f; _cycleTotal = 0f;
        _startedAt = Time.unscaledTime;
    }

    /// The player can aim (after Play, and after every verdict that continues the round).
    public static void AimStarted()
    {
        if (!_active) return;
        _aimStartedAt = Time.unscaledTime;
        _aiming = true;
    }

    public static void ShotFired()
    {
        if (!_active || !_aiming) return;
        _aimTotal += Time.unscaledTime - _aimStartedAt;
        _shots++;
        _aiming = false;
    }

    /// A verdict that continues the round: the shot cycle (aim + roll + verdict) is over, the next aim starts.
    public static void ShotJudged()
    {
        if (!_active) return;
        _cycleTotal += Time.unscaledTime - _aimStartedAt;
        AimStarted();
    }

    /// Win: payoutReceived = refund + profit. Lost / Forfeit / Abandoned: pass 0.
    public static void End(Outcome outcome, int payoutReceived)
    {
        if (!_active) return;
        _active = false;
        float now = Time.unscaledTime;
        if (outcome == Outcome.Win || outcome == Outcome.Lost) _cycleTotal += now - _aimStartedAt;   // the last shot's cycle ends with the result

        var e = new Entry
        {
            time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            presetId = _preset, difficulty = _difficulty, outcome = outcome.ToString(),
            bet = _bet, charge = _charge, shotsGranted = _granted, shotsUsed = _shots,
            seconds = now - _startedAt,
            aimSeconds = _shots > 0 ? _aimTotal / _shots : 0f,
            shotSeconds = _shots > 0 ? _cycleTotal / _shots : 0f,
            net = payoutReceived - _charge,
        };
        Memory.Add(e);
        string line = ToCsv(e);
        Debug.Log("[BilliardsLog] " + line);
        Append(line);
    }

    static string ToCsv(Entry e) => string.Join(",", e.time, e.presetId, e.difficulty, e.bet, e.charge, e.shotsGranted, e.shotsUsed,
        e.seconds.ToString("0.0", CultureInfo.InvariantCulture), e.aimSeconds.ToString("0.00", CultureInfo.InvariantCulture),
        e.shotSeconds.ToString("0.00", CultureInfo.InvariantCulture), e.outcome, e.net);

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    static void Append(string line)
    {
        if (!WriteToFile || Application.platform == RuntimePlatform.WebGLPlayer) return;   // no real file system on WebGL: the in-memory list only
        try
        {
            string path = FilePath;
            bool fresh = !File.Exists(path);
            File.AppendAllText(path, (fresh ? Header + "\n" : "") + line + "\n");
        }
        catch (Exception ex) { Debug.LogWarning("[BilliardsLog] could not write the log file: " + ex.Message); }
    }

    /// Reads every round ever logged on this machine (all sessions); falls back to this session's list.
    public static List<Entry> LoadAll()
    {
        var list = new List<Entry>();
        try
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer && File.Exists(FilePath))
            {
                foreach (string raw in File.ReadAllLines(FilePath))
                {
                    var f = raw.Split(',');
                    if (f.Length < 12 || f[0] == "time") continue;
                    list.Add(new Entry
                    {
                        time = f[0], presetId = f[1], difficulty = f[2],
                        bet = int.Parse(f[3]), charge = int.Parse(f[4]), shotsGranted = int.Parse(f[5]), shotsUsed = int.Parse(f[6]),
                        seconds = float.Parse(f[7], CultureInfo.InvariantCulture), aimSeconds = float.Parse(f[8], CultureInfo.InvariantCulture),
                        shotSeconds = float.Parse(f[9], CultureInfo.InvariantCulture), outcome = f[10], net = int.Parse(f[11]),
                    });
                }
                return list;
            }
        }
        catch (Exception ex) { Debug.LogWarning("[BilliardsLog] could not read the log file: " + ex.Message); }
        list.AddRange(Memory);
        return list;
    }

    /// Per difficulty: rounds, win rate, mean seconds per round, mean aim and shot seconds, net money per second.
    public static string Summarize(IReadOnlyList<Entry> rounds)
    {
        if (rounds == null || rounds.Count == 0) return "no rounds logged yet";
        var sb = new StringBuilder();
        sb.AppendLine($"{rounds.Count} round(s)");
        foreach (string d in new[] { "Easy", "Medium", "Hard" })
        {
            int n = 0, wins = 0, shots = 0, net = 0;
            float secs = 0f, aim = 0f, cycle = 0f;
            foreach (var e in rounds)
            {
                if (e.difficulty != d) continue;
                n++; if (e.outcome == "Win") wins++;
                shots += e.shotsUsed; net += e.net; secs += e.seconds;
                aim += e.aimSeconds * e.shotsUsed; cycle += e.shotSeconds * e.shotsUsed;
            }
            if (n == 0) { sb.AppendLine($"  {d,-6} no rounds"); continue; }
            sb.AppendLine($"  {d,-6} {n,3} rounds  win {wins * 100f / n,3:0}%  {secs / n,5:0.0}s/round  {(shots > 0 ? aim / shots : 0f),4:0.0}s aim, {(shots > 0 ? cycle / shots : 0f),4:0.0}s/shot  net {net.ToString("+0;-0;0")}  ({(net / Mathf.Max(secs, 0.01f)).ToString("+0.00;-0.00;0.00")} $/s)");
        }
        return sb.ToString().TrimEnd();
    }
}
