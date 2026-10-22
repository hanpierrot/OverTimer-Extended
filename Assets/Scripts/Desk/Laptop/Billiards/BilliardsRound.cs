using System;
using System.Collections.Generic;

/// One table being played. Pure C#: BilliardsApp drives it, the verifier drives it headless.
public sealed class BilliardsRound
{
    public BilliardsPreset    Preset     { get; }
    public TableMirror        Mirror     { get; }
    public BilliardsDifficulty Difficulty { get; }
    public int                Bet        { get; }
    public int                ShotsLeft  { get; private set; }
    public BilliardsSim       Sim        { get; }
    public BilliardsReferee   Referee    { get; }
    public bool               IsOver     { get; private set; }

    public BilliardsRound(BilliardsPreset preset, TableMirror mirror, BilliardsDifficulty difficulty,
                          int bet, GameConfig cfg, int extraShots = 0)
    {
        Preset = preset; Mirror = mirror; Difficulty = difficulty; Bet = bet;
        ShotsLeft = preset.ShotsFor(difficulty, cfg.billiardsParOffsets) + extraShots;
        Sim = new BilliardsSim(preset, BilliardsSimParams.FromConfig(cfg));

        var called = new Dictionary<string, Pocket>();
        foreach (var c in preset.called ?? Array.Empty<CalledPocket>())
            if (Enum.TryParse<Pocket>(c.pocket, out var pk)) called[c.ball] = pk;

        Referee = new BilliardsReferee(preset.order, preset.forbidden, called,
                                       cfg.billiardsStrictFirstContact, cfg.billiardsFinalWrongPocketFails);
    }

    BilliardsRound(BilliardsRound o)
    {
        Preset = o.Preset; Mirror = o.Mirror; Difficulty = o.Difficulty; Bet = o.Bet;
        ShotsLeft = o.ShotsLeft; IsOver = o.IsOver;
        Sim = o.Sim.Clone();
        Referee = o.Referee.Clone();
    }

    /// An independent copy in the same state (for solvers and the robustness check).
    public BilliardsRound Clone() => new BilliardsRound(this);

    public void Fire(ShotInput quantizedShot)
    {
        if (IsOver || ShotsLeft <= 0) return;
        ShotsLeft--;
        Sim.Shoot(quantizedShot);
    }

    /// Call once after Sim.Step() returns false.
    public ShotResult Resolve()
    {
        var res = Referee.Judge(Sim.Report);
        if (res.verdict == Verdict.Continue)
        {
            Sim.ApplyRespots(res.respot);
            if (ShotsLeft <= 0) { res.verdict = Verdict.Fail; res.failReason = "Out of shots"; }
        }
        IsOver = res.verdict != Verdict.Continue;
        return res;
    }
}
