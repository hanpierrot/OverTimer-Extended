using System;

public enum CallerKind { Telemarketer, Client, Unknown, ITRS }
public enum CallTruth  { Telemarketer, Client, ITRS }   // what the call really is
public enum CallEnd    { Answered, Declined, Missed }

public sealed class CallInfo
{
    public CallerKind shownAs;   // what caller ID shows while ringing
    public CallTruth  truth;     // decided at ring time, revealed on answer
    public bool PausesClock => truth != CallTruth.Telemarketer;
}

/// Weighted caller pick. All randomness comes from the passed System.Random (RngService).
public static class CallerRoller
{
    /// itrsEligible: ITRS only calls while a bill is pending; otherwise its weight is dropped
    /// and the others are renormalized.
    public static CallInfo Roll(Random rng, PhoneConfig c, bool itrsEligible)
    {
        int wT = c.weightTelemarketer, wC = c.weightClient, wU = c.weightUnknown, wI = itrsEligible ? c.weightITRS : 0;
        int total = wT + wC + wU + wI;
        if (total <= 0) throw new InvalidOperationException("caller weights sum to 0");
        int r = rng.Next(total);

        if (r < wT) return new CallInfo { shownAs = CallerKind.Telemarketer, truth = CallTruth.Telemarketer };
        r -= wT;
        if (r < wC) return new CallInfo { shownAs = CallerKind.Client, truth = CallTruth.Client };
        r -= wC;
        if (r < wU)
        {
            bool spam = rng.NextDouble() < c.unknownSpamChance;
            return new CallInfo { shownAs = CallerKind.Unknown, truth = spam ? CallTruth.Telemarketer : CallTruth.Client };
        }
        return new CallInfo { shownAs = CallerKind.ITRS, truth = CallTruth.ITRS };
    }
}

/// Plain data mirror of the phone fields in GameConfig. Lets the pure classes be tested
/// without a ScriptableObject; build it from GameConfig once per scene.
public sealed class PhoneConfig
{
    public int   weightTelemarketer = 40, weightClient = 25, weightUnknown = 25, weightITRS = 10;
    public float unknownSpamChance  = 0.6f;
    public float ringIntervalMin = 30f, ringIntervalMax = 60f;   // existing GameConfig fields
    public float maxRingDuration = 10f;                          // existing
    public float callMinGap      = 12f;                          // existing annoyanceMinGap
    public float ringClockMultiplier = 1.5f;                     // existing ringEffectMultiplier
    public float telemarketerPitch = 4f;
    public float bootSeconds = 5f;
    public float beneficialCallTimeout = 8f;                     // unscaled seconds to choose before the caller hangs up
}
