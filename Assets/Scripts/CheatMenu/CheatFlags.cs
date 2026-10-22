using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dev-only "force the next X" switches read by production code. Everything is gated on Debug.isDebugBuild (Editor and
/// development builds), so a release build ignores every flag, and every flag resets on scene load (Restart).
/// </summary>
public static class CheatFlags
{
    public enum NextTicket { None, Jackpot, Cursed }

    public static bool ForceCharlie;               // next Blackjack deal only
    public static NextTicket NextLuckyNine;        // next Lucky Nine purchase only
    public static bool FreeTickets;                // purchases cost nothing (until switched off)
    public static bool ForceFullVein;              // every vein is maximum length (until switched off)
    public static int PendingMiningStreak = -1;    // applied when the next vein starts; -1 = none

    public static bool FreeTicketsOn => Debug.isDebugBuild && FreeTickets;
    public static bool FullVeinOn => Debug.isDebugBuild && ForceFullVein;

    public static bool ConsumeCharlie()
    {
        if (!Debug.isDebugBuild || !ForceCharlie) return false;
        ForceCharlie = false;
        return true;
    }

    public static NextTicket ConsumeNextLuckyNine()
    {
        if (!Debug.isDebugBuild) return NextTicket.None;
        var forced = NextLuckyNine;
        NextLuckyNine = NextTicket.None;
        return forced;
    }

    public static bool TryConsumeMiningStreak(out int streak)
    {
        streak = PendingMiningStreak;
        if (!Debug.isDebugBuild || streak < 0) return false;
        PendingMiningStreak = -1;
        return true;
    }

    public static void Reset()
    {
        ForceCharlie = false;
        NextLuckyNine = NextTicket.None;
        FreeTickets = false;
        ForceFullVein = false;
        PendingMiningStreak = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        Reset();
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode) => Reset();
}
