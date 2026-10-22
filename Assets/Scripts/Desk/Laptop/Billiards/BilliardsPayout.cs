using UnityEngine;

public static class BilliardsPayout
{
    /// Gross payout on a win (stake included), set so the EXPECTED net gain matches the target rate
    /// once losses are counted. With win chance p, a table that should take T seconds and a bet b:
    ///     p * payout - b  =  netRate * T * (b / betMax)
    ///     payout          =  b * (1 + netRate * T / betMax) / p
    /// T comes from the shots granted, so long tables pay more without hand-tuning each one.
    /// payoutScale on the preset scales the net-gain term.
    public static int OnWin(int bet, BilliardsDifficulty d, BilliardsPreset p, GameConfig cfg)
    {
        int i = (int)d;
        int granted = p.ShotsFor(d, cfg.billiardsParOffsets);
        float expectedShots   = (p.par + granted) * 0.5f;
        float expectedSeconds = expectedShots * cfg.billiardsSecondsPerShot;
        float expectedNetAtMaxBet = cfg.billiardsTargetRate[i] * expectedSeconds * p.payoutScale;
        float multiple = (1f + expectedNetAtMaxBet / cfg.billiardsBetMax) / cfg.billiardsAssumedWinRate[i];
        return Mathf.RoundToInt(bet * multiple);
    }
}
