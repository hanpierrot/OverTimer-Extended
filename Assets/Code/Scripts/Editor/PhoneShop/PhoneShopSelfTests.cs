using System;
using UnityEditor;
using UnityEngine;

/// Headless checks for the Phone/Shop plan's pure classes (PHONE_SHOP_DELIVERY_IMPLEMENTATION Appendix B).
/// Grows with each phase: add checks, never delete one.
public static class PhoneShopSelfTests
{
    static int _fails, _passes;

    static void Check(bool ok, string name)
    {
        if (ok) _passes++; else { _fails++; Debug.LogError("[PhoneShop] FAIL " + name); }
    }

    [MenuItem("Tools/OverTimer/PhoneShop/Run Self-Tests")]
    public static void Run()
    {
        _fails = 0; _passes = 0;

        ClockCore();

        if (_fails == 0) Debug.Log($"[PhoneShop] ALL OK ({_passes} checks)");
        else Debug.LogError($"[PhoneShop] {_fails} FAILED, {_passes} passed");
    }

    // ---- Phase 1.1: PauseLock, SpeedModifiers
    static void ClockCore()
    {
        var pl = new PauseLock(); int flips = 0; pl.PausedChanged += _ => flips++;
        object call = "call", cut = "cutscene";
        pl.Acquire(call); pl.Acquire(cut); pl.Release(call);
        Check(pl.IsPaused, "overlap: releasing the call keeps the cutscene's pause");
        pl.Release(cut); Check(!pl.IsPaused && flips == 2, "clock resumes only when the last holder releases (2 flips)");
        pl.Acquire(call); pl.Acquire(call); pl.Release(call); Check(!pl.IsPaused, "same holder twice counts once");
        Check(!pl.Release("nobody"), "releasing an unknown holder is a no-op");

        var sm = new SpeedModifiers(); object snail = "snail", ring = "ring";
        sm.Set(snail, 0.75f); sm.Set(ring, 1.5f);
        Check(Math.Abs(sm.Effective - 1.125f) < 1e-5, "snail 0.75 x ring 1.5 = 1.125 (stack, no clobber)");
        sm.Remove(ring); Check(Math.Abs(sm.Effective - 0.75f) < 1e-5, "ring ends: snail still applies");
        sm.Set("scarab", 0f); Check(sm.Effective == 0f, "Time Scarab x0 stops the clock");
        sm.Clear(); sm.Set("a", 2f); sm.Set("b", 2f); Check(sm.Effective == 3f, "clamped to 3");
        sm.Set("a", 1f); Check(sm.Count == 2 && sm.Effective == 2f, "same key replaces, never double-counts");
    }
}
