using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Effects that cards have queued for the Billiards app. Static because the app's GameObject may not have Awoken (or
/// may be closed) when a card is placed.
///
/// Every effect is a GRANT owned by the card that made it, so the card's life cycle stays in charge (same contract as
/// Overtime / Butterfingers on the Mining side, minus their leaks):
///  - the effect waits until Billiards uses it; using it ends the card (EffectCard.EndEffect: face down, collected);
///  - if the card goes away first (pawned, destroyed, timed out) its OnRemoved calls Withdraw, so "pawn the card but
///    keep the effect" is impossible and an unplayed card is a real escape;
///  - everything is cleared on every scene load, because Restart is a scene reload and statics would otherwise
///    leak into the next run.
public static class BilliardsPendingEffects
{
    public enum Kind { ExtraShots, TrickShot, CueSlip }

    sealed class Grant
    {
        public Kind kind;
        public float amount;                       // shots, or the +- power jitter as a fraction
        public CardItem owner;                     // null only in tests
    }

    static readonly List<Grant> Grants = new List<Grant>();

    /// Raised whenever the set of pending effects changes (a card placed, an effect used, a card withdrawn).
    public static event Action Changed;

    public static int ExtraShots
    {
        get { float n = 0f; foreach (var g in Grants) if (g.kind == Kind.ExtraShots) n += g.amount; return Mathf.RoundToInt(n); }
    }
    public static bool TrickShotPending => Has(Kind.TrickShot);
    public static bool CueSlipPending => Has(Kind.CueSlip);

    static bool Has(Kind kind) { foreach (var g in Grants) if (g.kind == kind) return true; return false; }

    /// Called by a card's OnPlaced.
    public static void Add(Kind kind, float amount, CardItem owner)
    {
        Grants.Add(new Grant { kind = kind, amount = amount, owner = owner });
        Changed?.Invoke();
    }

    /// Called by a card's OnRemoved: whatever that card granted and Billiards has not used yet is dropped.
    public static void Withdraw(CardItem owner)
    {
        if (owner == null) return;
        if (Grants.RemoveAll(g => g.owner == owner) > 0) Changed?.Invoke();
    }

    /// At the start of a round: every pending extra shot at once. Their cards end (they have been used).
    public static int TakeExtraShots()
    {
        var used = Grants.FindAll(g => g.kind == Kind.ExtraShots);
        if (used.Count == 0) return 0;
        float n = 0f;
        foreach (var g in used) n += g.amount;
        Consume(used);
        return Mathf.RoundToInt(n);
    }

    /// When a shot is fired: ONE trick shot preview is used up per shot.
    public static bool TakeTrickShot() => TakeOne(Kind.TrickShot, out _);

    /// When a shot is fired: ONE cue slip is used up per shot. Returns the +- power jitter as a fraction, 0 when none.
    public static float TakeCueSlip() => TakeOne(Kind.CueSlip, out float amount) ? amount : 0f;

    static bool TakeOne(Kind kind, out float amount)
    {
        amount = 0f;
        var g = Grants.Find(x => x.kind == kind);
        if (g == null) return false;
        amount = g.amount;
        Consume(new List<Grant> { g });
        return true;
    }

    static void Consume(List<Grant> used)
    {
        foreach (var g in used) Grants.Remove(g);          // removed first: EndEffect -> OnRemoved -> Withdraw must find nothing left
        Changed?.Invoke();
        foreach (var g in used) if (g.owner != null) g.owner.EndEffect();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        Reset();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Reset();

    public static void Reset()
    {
        Grants.Clear();
        Changed = null;
    }
}
