using UnityEngine;

/// <summary>
/// Single source of truth for every tuning value in the game (DESIGN.md I10, §5).
/// Only Ari edits this asset - Han requests value changes in chat (DESIGN.md §7.3).
/// No economy literals belong in any other script.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Overtime/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Global (§5.1)")]
    public float startClock = 120f;
    public float winClock = 600f;
    public float loseClock = 0f;
    public int startMoney = 40;
    public float exchangeRate = 1f; // $1 = 1s
    public int meterMinFeed = 10;

    [Header("Mining (§5.2)")]
    public int[] blockPayout = { 1, 3, 4, 5, 6, 8, 9, 10, 12, 13 };
    public int veinLengthMin = 6;
    public int veinLengthMax = 10;
    public float veinRespawn = 1.5f;
    
    [Header("Ticket Stack (§5.3)")]
    public int tripleMatchCost = 25;
    public int tripleMatchBaseReward = 55;

    public int luckyNineCost = 80;
    public int luckyNineBaseReward = 0;

    [Header("Card Box (§5.4)")]
    public int handCap = 5;
    public int packCost = 35;
    
    [Header("Card Box - Rarity Weights (§5.4)")]
    public float commonWeight = 60f;
    public float rareWeight = 25f;
    public float superRareWeight = 12f;
    public float ultrRareWeight = 3f;

    [Header("Blackjack (§5.5 / BLACKJACK.md §4)")]
    public float blackjackAnte = 15f;
    public float blackjackClockMargin = 10f;
    public int blackjackWinPayout = 120;
    public int blackjackPushPayout = 60;
    public int blackjackNaturalPayout = 200;
    public float blackjackRevealDelay = 0.6f;
    public float blackjackResultDelay = 1.2f;
    public float laptopSwitchTime = 1.0f;

    [Header("ITRS (§5.6)")]
    public float billInterval = 45f;
    [Range(0f, 1f)] public float billRate = 0.20f;
    public int billMinimum = 20;
    public float shortfallPenalty = 2f;

    [Header("Phone (§5.7)")]
    public float ringIntervalMin = 30f;
    public float ringIntervalMax = 60f;
    public float ringEffectMultiplier = 1.5f;
    public float answerTime = 1.5f;
    public float maxRingDuration = 10f;

    [Header("Annoyance Manager (§5.7)")]
    public float annoyanceMinGap = 12f;

    [Header("Egg Timer (§5.8)")]
    public float crankTime = 2f;
    public float timePayout = 3f;
    
    [Header("Cat")]
    public float catSitDurationMin = 3f;
    public float catSitDurationMax = 8f;
    public float catBlockChance = 0.4f;
    public float catBlockDurationMin = 4f;
    public float catBlockDurationMax = 8f;
    
    [Header("Click Task")]
    public int clickTaskMinClicks = 3;
    public int clickTaskMaxClicks = 8;
    public float clickTaskLockDuration = 3f;
    public int clickTaskMoneyReward = 5;

    [Header("Picture Click Task")]
    public int pictureClickTaskMinClicks = 3;
    public int pictureClickTaskMaxClicks = 8;
    public float pictureClickTaskLockDuration = 3f;
    public int pictureClickTaskMoneyReward = 5;

    [Header("Power Bank Task")]
    public float powerBankCrankTime = 2f;
    public int powerBankMoneyReward = 10;
    public float powerBankFullHoldDelay = 1.5f;
    public float powerBankCooldown = 5f;
    public float powerBankRestTime = 2f;
    
    [Header("Wire Task")]
    public int wireTaskMoneyReward = 20;
    
    [Header("Pawn Value")]
    public int eggTimerPawnValue = 150;
    public int cardBoxPawnValue = 180;
    public int powerBankPawnValue = 45;
    public int clickTaskPawnValue = 40;
    public int pictureClickTaskPawnValue = 40;
    public int wireTaskPawnValue = 15;
    public int tripleStackPawnValue = 50;
    public int nineStackPawnValue = 100;
    public int junkSmallPawnValue = 5;
    public int junkMediumPawnValue = 15;
    public int junkLargePawnValue = 30;
    public int unrevealedCardSellValue = 10;
    public int laptopPawnValue = 300;

    [Header("Billiards - economy")]
    public int     billiardsBetMin  = 10;
    public int     billiardsBetMax  = 40;
    public int     billiardsBetStep = 5;
    public int[]   billiardsParOffsets = { 4, 2, 0 };                    // easy, medium, hard
    [Tooltip("Expected NET dollars per second of play, losses included, by difficulty (easy, medium, hard)")]
    public float[] billiardsTargetRate = { 1.5f, 3f, 4f };
    [Tooltip("Assumed chance of winning a table, by difficulty. A guess until Phase 9 measures it; lower = bigger payout")]
    public float[] billiardsAssumedWinRate = { 0.85f, 0.65f, 0.5f };
    [Tooltip("Guess. Measure in Phase 9 and update.")]
    public float   billiardsSecondsPerShot = 7f;
    public AimAssistLevel[] billiardsAssist =
        { AimAssistLevel.FullGhost, AimAssistLevel.ContactPoint, AimAssistLevel.DirectionOnly };

    [Header("Billiards - rules (Q2)")]
    public bool billiardsStrictFirstContact   = true;
    public bool billiardsFinalWrongPocketFails = true;

    [Header("Billiards - physics. Changing ANY of these invalidates stored solutions: re-run the verifier.")]
    [Tooltip("Table units; felt is 2 x 1. TBD from art once Q1 is answered: (ballCanvasPx / 2) / feltCanvasWidth * 2")]
    public float billiardsBallRadius         = 0.0714f;  // assumed Q1 default: felt 588 wide, 42 px ball
    [Tooltip("Corner pocket capture radius. Needs >= 1.9 x ballRadius or the jaws bounce balls out of straight shots")]
    public float billiardsCornerPocketRadius = 0.15f;
    [Tooltip("Side pocket capture radius. Needs >= 1.4 x ballRadius")]
    public float billiardsSidePocketRadius   = 0.12f;
    [Tooltip("Must satisfy maxShotSpeed / 120 < ballRadius or balls can tunnel through cushions")]
    public float billiardsMaxShotSpeed       = 5f;       // table units per second at power 1
    public float billiardsRollingDecel       = 1.2f;     // table units per second squared
    public float billiardsBallRestitution    = 0.95f;
    public float billiardsCushionRestitution = 0.75f;
    public float billiardsStopSpeed          = 0.01f;    // below this a ball snaps to rest
    public float billiardsMaxShotSeconds     = 20f;      // safety cap: force-settle a shot after this long

    /// Call once at app start. Catches arrays resized wrong in the Inspector.
    public bool ValidateBilliards(out string problem)
    {
        problem = null;
        if (billiardsParOffsets == null || billiardsParOffsets.Length != 3) problem = "billiardsParOffsets needs 3 entries";
        else if (billiardsTargetRate == null || billiardsTargetRate.Length != 3) problem = "billiardsTargetRate needs 3 entries";
        else if (billiardsAssumedWinRate == null || billiardsAssumedWinRate.Length != 3) problem = "billiardsAssumedWinRate needs 3 entries";
        else if (billiardsAssumedWinRate[0] <= 0f || billiardsAssumedWinRate[1] <= 0f || billiardsAssumedWinRate[2] <= 0f
              || billiardsAssumedWinRate[0] > 1f || billiardsAssumedWinRate[1] > 1f || billiardsAssumedWinRate[2] > 1f) problem = "billiardsAssumedWinRate entries must be in (0, 1]";
        else if (billiardsAssist == null || billiardsAssist.Length != 3) problem = "billiardsAssist needs 3 entries";
        else if (billiardsBetMin <= 0 || billiardsBetMax < billiardsBetMin) problem = "bet min/max invalid";
        else if (billiardsMaxShotSpeed / 120f >= billiardsBallRadius) problem = "maxShotSpeed / 120 must be < ballRadius (tunneling)";
        else if (billiardsCornerPocketRadius < 1.9f * billiardsBallRadius) problem = "cornerPocketRadius must be >= 1.9 x ballRadius (jaws would bounce balls out of corner pockets)";
        else if (billiardsSidePocketRadius < 1.4f * billiardsBallRadius) problem = "sidePocketRadius must be >= 1.4 x ballRadius (jaws would bounce balls out of side pockets)";
        return problem == null;
    }
}

public enum AimAssistLevel { DirectionOnly, ContactPoint, FullGhost }
