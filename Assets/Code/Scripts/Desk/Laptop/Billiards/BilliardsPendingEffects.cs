using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Effects queued by cards for the Billiards app (Phase 8). Static because the app's GameObject
/// may not have Awoken yet when a card is placed. Reset on EVERY scene load, because Restart is a
/// scene reload and statics - including static events - would otherwise leak into the next run.
public static class BilliardsPendingEffects
{
    public static int  ExtraShots;
    public static bool TrickShotPreview;
    public static bool CueSlip;

    /// Cards subscribe to end themselves when their effect is used (same pattern as Overtime/Butterfingers).
    public static event Action ExtraShotsConsumed;
    public static event Action TrickShotConsumed;
    public static event Action CueSlipConsumed;

    public static int TakeExtraShots()
    {
        int n = ExtraShots;
        ExtraShots = 0;
        if (n > 0) ExtraShotsConsumed?.Invoke();
        return n;
    }

    public static bool TakeTrickShot()
    {
        if (!TrickShotPreview) return false;
        TrickShotPreview = false;
        TrickShotConsumed?.Invoke();
        return true;
    }

    public static bool TakeCueSlip()
    {
        if (!CueSlip) return false;
        CueSlip = false;
        CueSlipConsumed?.Invoke();
        return true;
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
        ExtraShots = 0;
        TrickShotPreview = false;
        CueSlip = false;
        ExtraShotsConsumed = null;
        TrickShotConsumed = null;
        CueSlipConsumed = null;
    }
}
