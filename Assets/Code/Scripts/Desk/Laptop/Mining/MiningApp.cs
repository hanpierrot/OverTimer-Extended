using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The mining app: drag the pickaxe onto the ore block to hit it. Each vein is
/// a random-length (GameConfig.veinLengthMin..Max) walk through BlockConfig's
/// ordered block list, starting at index 0 (dirt) - fixed order, not random,
/// so a vein always progresses dirt -> wood -> ... -> ruby. Payout is
/// position-indexed from GameConfig.blockPayout, so quitting late in a vein
/// costs more (DESIGN.md §5.2). Finishing the vein banks the accumulated
/// money; closing the app mid-vein forfeits whatever's still unconfirmed,
/// same house rule as Blackjack forfeiting a live hand (BLACKJACK.md §2.3).
/// </summary>
public class MiningApp : LaptopApp
{
    [Header("Data")]
    [SerializeField] private BlockConfig blocks;

    [Header("UI")]
    [SerializeField] private PickaxeView pickaxe;
    [SerializeField] private Image oreImage;
    [SerializeField] private Image[] statusNodes;
    [SerializeField] private TMP_Text unconfirmedLabel;
    [SerializeField] private TMP_Text receivedLabel;
    [SerializeField] private Color filledColor = new Color(0f, 0.29644084f, 0.82641506f, 1f);
    [SerializeField] private Color unfilledColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private GameConfig config => GameManager.Instance.GameConfig;

    private List<BlockTypeDefinition> _vein;
    private int _blockIndex;
    private int _blockHp;
    private int _totalVeinHp;
    private int _damageDealt;
    private int _unconfirmed;
    private int _totalReceived;
    private int _fullVeinStreak;   // consecutive maximum-length veins banked without leaving the app (Mother Lode)

    private int MaxVeinLength => Mathf.Min(config.veinLengthMax, blocks.blocks.Length, statusNodes.Length);

    private static float _pendingNextVeinMultiplier = 1f;
    private static bool _pendingNextVeinSlip;

    // The cards that queued each effect. The app ends them when it uses the effect, and a card that goes away first
    // (pawned) withdraws its effect: before, the effect stayed queued and the card's event handler stayed subscribed, so
    // "pawn it, keep the effect" worked and a stale handler could end an unrelated card that reused the same CardItem.
    private static readonly List<CardItem> _multiplierCards = new List<CardItem>();
    private static readonly List<CardItem> _slipCards = new List<CardItem>();

    /// Still raised when a queued effect is used; the cards no longer rely on it (they are ended directly).
    public static event Action OnNextVeinEffectResolved;

    public static void QueueNextVeinMultiplier(float multiplier, CardItem owner = null)
    {
        _pendingNextVeinMultiplier = multiplier;
        if (owner != null && !_multiplierCards.Contains(owner)) _multiplierCards.Add(owner);
    }

    public static void QueueNextVeinSlip(CardItem owner = null)
    {
        _pendingNextVeinSlip = true;
        if (owner != null && !_slipCards.Contains(owner)) _slipCards.Add(owner);
    }

    /// A card's OnRemoved: whatever it queued and the app has not used yet is dropped (the last card of a kind clears the effect).
    public static void WithdrawNextVeinEffects(CardItem owner)
    {
        if (owner == null) return;
        if (_multiplierCards.Remove(owner) && _multiplierCards.Count == 0) _pendingNextVeinMultiplier = 1f;
        if (_slipCards.Remove(owner) && _slipCards.Count == 0) _pendingNextVeinSlip = false;
    }

    private static void EndOwners(List<CardItem> owners)
    {
        var done = owners.ToArray();
        owners.Clear();                                           // cleared first: EndEffect -> OnRemoved -> Withdraw must find nothing
        foreach (var card in done) if (card != null) card.EndEffect();
    }

    /// Restart is a scene reload and statics survive it: without this an unused card's effect (and its listeners) leaked into the next run.
    public static void ResetPending()
    {
        _pendingNextVeinMultiplier = 1f;
        _pendingNextVeinSlip = false;
        _multiplierCards.Clear();
        _slipCards.Clear();
        OnNextVeinEffectResolved = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void HookPendingReset()
    {
        ResetPending();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedReset;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedReset;
    }

    private static void OnSceneLoadedReset(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => ResetPending();
    
    private void Start()
    {
        pickaxe.Hit += OnHit;
        StartVein();
    }

    private void OnEnable()
    {
        // Deactivating the app kills RespawnAfterDelay, and Start won't run
        // again, so a vein finished just before closing would never restart.
        // On the very first activation _vein is still null and Start handles it.
        if (_vein != null && _blockIndex >= _vein.Count) StartVein();
    }

    private void StartVein()
    {
        var rng = RngService.Instance.Random;
        int length = rng.Next(config.veinLengthMin, config.veinLengthMax + 1);
        if (_fullVeinStreak > 0)
        {
            // After a full vein the chance of another replaces the normal 1 in 5 (it does not add to it).
            length = rng.NextDouble() < config.miningFullVeinStreakChance
                ? config.veinLengthMax
                : rng.Next(config.veinLengthMin, config.veinLengthMax);
        }
        length = Mathf.Min(length, blocks.blocks.Length, statusNodes.Length);

        _vein = new List<BlockTypeDefinition>(length);
        _totalVeinHp = 0;
        for (int i = 0; i < length; i++)
        {
            _vein.Add(blocks.blocks[i]);
            _totalVeinHp += blocks.blocks[i].hp;
        }

        _blockIndex = 0;
        _blockHp = _vein[0].hp;
        _damageDealt = 0;
        _unconfirmed = 0;

        // Only show as many nodes as this vein actually has, so a length-6
        // vein reads as "mining to 6," not as a 10-node bar stuck at 60%.
        for (int i = 0; i < statusNodes.Length; i++)
            statusNodes[i].gameObject.SetActive(i < length);

        RefreshOre();
        RefreshProgress();
        RefreshLabels();
    }

    private void OnHit()
    {
        // A hit landing during the veinRespawn delay (vein finished, next one
        // not generated yet) would otherwise index past the old vein's end.
        if (_vein == null || _blockIndex >= _vein.Count) return;

        var block = _vein[_blockIndex];
        int damage = Mathf.Min(block.pickaxeDamage, _blockHp);

        _blockHp -= damage;
        _damageDealt += damage;

        if (_blockHp <= 0)
        {
            int payoutIndex = Mathf.Min(_blockIndex, config.blockPayout.Length - 1);
            _unconfirmed += config.blockPayout[payoutIndex];
            HintService.Instance?.ShowOnce(HintId.MiningCloseLosesOre);
            _blockIndex++;

            if (_blockIndex >= _vein.Count)
            {
                if (BankVein())
                {
                    GameManager.Instance.GameOver(EndReason.MotherLodeWin);
                    return;   // the run is over: no next vein
                }
                StartCoroutine(RespawnAfterDelay());
                return;
            }

            _blockHp = _vein[_blockIndex].hp;
            RefreshOre();
        }

        RefreshProgress();
        RefreshLabels();
    }

    /// <returns>True when this vein completes the Mother Lode streak.</returns>
    private bool BankVein()
    {
        bool hadPendingEffect = _pendingNextVeinSlip || _pendingNextVeinMultiplier != 1f;

        bool slipUsed = false, multiplierUsed = false;
        if (_pendingNextVeinSlip)
        {
            _pendingNextVeinSlip = false;
            _unconfirmed = 0;
            slipUsed = true;
        }
        else
        {
            multiplierUsed = _pendingNextVeinMultiplier != 1f;
            int payout = Mathf.RoundToInt(_unconfirmed * _pendingNextVeinMultiplier);
            _pendingNextVeinMultiplier = 1f;
            MoneyService.Instance.Add(payout, "mining vein");
            _totalReceived += _unconfirmed;
            _unconfirmed = 0;
        }

        RefreshLabels();

        // The streak counts consecutive maximum-length veins; a slipped vein or any shorter vein breaks it.
        if (slipUsed) _fullVeinStreak = 0;
        else if (_vein.Count >= MaxVeinLength) _fullVeinStreak++;
        else _fullVeinStreak = 0;

        // End exactly the cards whose effect was just used (a slip no longer ends a Lucky Ruby that is still waiting).
        if (slipUsed) EndOwners(_slipCards);
        if (multiplierUsed) EndOwners(_multiplierCards);

        if (hadPendingEffect) OnNextVeinEffectResolved?.Invoke();

        bool won = _fullVeinStreak >= config.miningStreakTarget;
        if (!won && _fullVeinStreak == config.miningStreakTarget - 1 && HintService.Instance != null)
            HintService.Instance.Show(HintId.MotherLodeTease);
        return won;
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(config.veinRespawn);
        StartVein();
    }

    private void RefreshOre() => oreImage.sprite = _vein[_blockIndex].sprite;

    private void RefreshProgress()
    {
        float fraction = _totalVeinHp == 0 ? 0f : (float)_damageDealt / _totalVeinHp;
        int filled = Mathf.RoundToInt(fraction * _vein.Count);

        for (int i = 0; i < _vein.Count; i++)
            statusNodes[i].color = i < filled ? filledColor : unfilledColor;
    }

    private void RefreshLabels()
    {
        unconfirmedLabel.text = $"Unconfirmed: ${_unconfirmed}";
        receivedLabel.text = $"Received: ${_totalReceived}";
    }
    
    public override void OnAppOpened() => HintService.Instance?.ShowOnce(HintId.MiningOpen);

    public override void OnAppClosed()
    {
        _unconfirmed = 0;
        _fullVeinStreak = 0;   // leaving the app loses the streak
        RefreshLabels();
    }
}
