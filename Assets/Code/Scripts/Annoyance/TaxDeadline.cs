using System;

/// The 30-second rule for one ITRS bill. ITRSService keeps the bill itself (CurrentAssessment);
/// this only tracks how long is left. Shown in two places (Taxes app + desk/mailbox letter),
/// resolved once.
public sealed class TaxDeadline
{
    public enum Outcome { Pending, Paid, Penalized }

    public int Amount { get; private set; }
    public float SecondsLeft { get; private set; }
    public Outcome State { get; private set; } = Outcome.Pending;

    public event Action<TaxDeadline> Resolved;   // fires exactly once

    public TaxDeadline(int amount, float windowSeconds) { Amount = amount; SecondsLeft = windowSeconds; }

    /// Counts only while the clock runs (pass gameDelta), so a paused call can't run the window out.
    public void Tick(float gameDelta)
    {
        if (State != Outcome.Pending || gameDelta <= 0f) return;
        SecondsLeft -= gameDelta;
        if (SecondsLeft <= 0f) { SecondsLeft = 0f; Finish(Outcome.Penalized); }
    }

    public void MarkPaid() { if (State == Outcome.Pending) Finish(Outcome.Paid); }

    /// ITRS "payment plan" call.
    public void Extend(float seconds) { if (State == Outcome.Pending) SecondsLeft += seconds; }

    /// Audit card doubling an open bill.
    public void Double() { if (State == Outcome.Pending) Amount *= 2; }

    void Finish(Outcome o) { State = o; Resolved?.Invoke(this); }
}
