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
    
    [Header("Click Task")]
    public int clickTaskMinClicks = 3;
    public int clickTaskMaxClicks = 8;
    public float clickTaskLockDuration = 3f;
}
