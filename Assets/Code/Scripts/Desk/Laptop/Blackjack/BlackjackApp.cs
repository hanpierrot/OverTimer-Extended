using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Overtime.Blackjack;

/// <summary>
/// Blackjack as a laptop app. Ante is paid in CLOCK SECONDS, payout is MONEY.
/// This is the only time -> money converter in the game (DESIGN.md I3, 5.5).
///
/// This class owns: ante charging, payout, safety rails, UI.
/// BlackjackGame owns: cards and rules. Keep that split.
/// </summary>
public class BlackjackApp : LaptopApp
{
    [Header("UI")]
    [SerializeField] private Button dealButton;
    [SerializeField] private Button hitButton;
    [SerializeField] private Button standButton;
    [SerializeField] private Button doubleButton;
    [SerializeField] private TMP_Text doubleButtonLabel;
    [SerializeField] private TMP_Text playerValueLabel;
    [SerializeField] private TMP_Text dealerValueLabel;
    [SerializeField] private TMP_Text anteLabel;
    [SerializeField] private TMP_Text spentLabel;
    [SerializeField] private TMP_Text receivedLabel;
    [SerializeField] private CardRowView playerRow;
    [SerializeField] private CardRowView dealerRow;

    // Simple "WIN" badges - just shown/hidden on whichever side won, no text
    // changes. All the actual wording (payout amount, push, bust, the
    // "NOT ENOUGH TIME" warning) goes through winningOutcomeLabel instead,
    // which shows the static payout table the rest of the time.
    [SerializeField] private GameObject playerWinLabel;
    [SerializeField] private GameObject dealerWinLabel;
    [SerializeField] private TMP_Text winningOutcomeLabel;
    [Tooltip("Hot Table streak readout. Shown only while the streak is 1 or more")]
    [SerializeField] private TMP_Text streakLabel;

    private GameConfig Config => GameManager.Instance.GameConfig;

    private BlackjackGame _game;
    private bool _resolving;
    private float _totalSpent;
    private int _totalReceived;
    private string _payoutInfoText;

    // Hot Table: wins in a row this sitting. Instance field on purpose - it dies with the app's state,
    // never survives a scene reload, and closing the app resets it (OnAppClosed).
    private int _streak;

    /// <summary>Bonus on the next win, as a fraction (0.2 = +20%).</summary>
    public static float StreakBonus(GameConfig cfg, int streak) =>
        Mathf.Min(streak * cfg.blackjackStreakStep, cfg.blackjackStreakCap);

    /// <summary>
    /// Money for a resolved hand, and the streak after it. Win or natural: base x (doubled ? 2 : 1) x (1 + bonus),
    /// then streak + 1. Push: base x (doubled ? 2 : 1), streak unchanged. Anything else: 0 and the streak resets.
    /// Money only - the clock is never touched here.
    /// </summary>
    public static int SettleHand(GameConfig cfg, Outcome result, bool doubled, ref int streak)
    {
        int mult = doubled ? 2 : 1;
        switch (result)
        {
            case Outcome.PlayerNatural:
            case Outcome.PlayerWin:
            case Outcome.DealerBust:
                int baseAmount = result == Outcome.PlayerNatural ? cfg.blackjackNaturalPayout : cfg.blackjackWinPayout;
                int payout = Mathf.RoundToInt(baseAmount * mult * (1f + StreakBonus(cfg, streak)));
                streak++;
                return payout;
            case Outcome.Push:
                return cfg.blackjackPushPayout * mult;
            default:
                streak = 0;
                return 0;
        }
    }

    private float CurrentAnte
    {
        get
        {
            float mult = CostModifierService.Instance != null
                ? CostModifierService.Instance.GetMultiplier(CostModifierService.Target.Blackjack)
                : 1f;
            return Mathf.Round(Config.blackjackAnte * mult);
        }
    }
    
    // Disabled by the "Connection Lost" debuff. Does NOT disable mining.
    public bool Banned { get; private set; }
    
    private void Start() => EnsureInitialized();

    /// <summary>
    /// Can't rely on Start() alone: LaptopController.Start() deactivates every
    /// app on scene load, and if that happens before Unity gets to this
    /// object's own Start() in the frame's Start-phase, Unity skips it and
    /// defers it until the app is reactivated - but OnAppOpened() (called the
    /// instant RequestOpen reactivates it) runs synchronously before that
    /// deferred Start() gets a chance to fire, so _game would still be null.
    /// Called from both Start() and OnAppOpened() and guarded so it only
    /// runs once, whichever gets there first.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_game != null) return;

        _game = new BlackjackGame(RngService.Instance.Random);

        dealButton.onClick.AddListener(OnDeal);
        hitButton.onClick.AddListener(OnHit);
        standButton.onClick.AddListener(OnStand);
        doubleButton.onClick.AddListener(OnDouble);

        // Static for the session - ante/payouts don't change mid-run.
        _payoutInfoText = $"win ${Config.blackjackWinPayout} · push ${Config.blackjackPushPayout} · bj ${Config.blackjackNaturalPayout}";
        winningOutcomeLabel.text = _payoutInfoText;

        HideWinBadges();
    }

    // ---------- Safety rails ----------

    /// <summary>
    /// The ante must never be able to kill the player outright.
    /// Requires a margin above the ante so a loss is survivable.
    /// </summary>
    private bool CanAffordAnte =>
        ClockService.Instance.CurrentSeconds >= CurrentAnte + Config.blackjackClockMargin;

    private bool Available => !Banned && !_resolving && !GameManager.Instance.IsGameOver;

    // ---------- Actions ----------

    private void OnDeal()
    {
        if (!Available || _game.State != HandState.Idle) return;
        if (!CanAffordAnte)
        {
            Flash("NOT ENOUGH TIME");
            HintService.Instance?.Show(HintId.NotEnoughTime, Mathf.CeilToInt(CurrentAnte + Config.blackjackClockMargin));
            return;
        }

        ClockService.Instance.Spend(CurrentAnte, "blackjack ante");
        _totalSpent += CurrentAnte;
        HintService.Instance?.ShowOnce(HintId.BlackjackLeaveMidHand);
        HideWinBadges();
        winningOutcomeLabel.text = _payoutInfoText; // clear any lingering flash/warning
        bool charlieHand = CheatFlags.ConsumeCharlie()
                           || RngService.Instance.Random.NextDouble() < Config.blackjackCharlieChance;
        if (charlieHand && Debug.isDebugBuild) Debug.Log("[Ending] Blackjack: this hand is rigged for a Charlie");
        _game.Deal(charlieHand ? Config.blackjackCharlieCards : 0);

        if (_game.State == HandState.Resolved)
            StartCoroutine(Resolve());   // natural on the deal

        Refresh();
    }

    private void OnHit()
    {
        if (!Available) return;

        _game.Hit();
        
        if (_game.PlayerHand.Count >= 4 && HintService.Instance != null)
            HintService.Instance.ShowAlways(HintId.CharlieTease);
        
        if (_game.HasCharlie(Config.blackjackCharlieCards))
        {
            Refresh();
            GameManager.Instance.GameOver(EndReason.BlackjackCharlieWin);
            return;
        }
        
        if (_game.State == HandState.Resolved) StartCoroutine(Resolve());
        Refresh();
    }

    private void OnStand()
    {
        if (!Available) return;

        _game.Stand();
        StartCoroutine(Resolve());
        Refresh();
    }

    private void OnDouble()
    {
        if (!Available || !_game.CanDouble) return;

        // Doubling costs a SECOND ante in clock seconds. This is the most
        // expensive decision in the game and it should feel like it.
        if (!CanAffordAnte)
        {
            Flash("NOT ENOUGH TIME");
            HintService.Instance?.Show(HintId.NotEnoughTime, Mathf.CeilToInt(CurrentAnte + Config.blackjackClockMargin));
            return;
        }

        ClockService.Instance.Spend(CurrentAnte, "blackjack double");
        _totalSpent += CurrentAnte;
        _game.DoubleDown();
        StartCoroutine(Resolve());
        Refresh();
    }

    // ---------- Resolution ----------

    private IEnumerator Resolve()
    {
        _resolving = true;
        Refresh();

        // Reveal beat. Keep this SHORT - the clock is running and dead
        // time on a resolved hand feels like theft.
        yield return new WaitForSeconds(Config.blackjackRevealDelay);

        int payout = SettleHand(Config, _game.Result, _game.DoubledDown, ref _streak);
        RefreshStreak();

        if (payout > 0)
        {
            MoneyService.Instance.Add(payout, "blackjack");
            _totalReceived += payout;
        }

        winningOutcomeLabel.text = payout > 0 ? $"{_game.ResultText}  +${payout}" : _game.ResultText;

        // Push still pays out (a partial refund), so it reads as a player-side
        // outcome same as a win - only an outright dealer win or player bust
        // shows on the dealer's side.
        bool dealerSide = _game.Result is Outcome.DealerWin or Outcome.PlayerBust;
        playerWinLabel.SetActive(!dealerSide);
        dealerWinLabel.SetActive(dealerSide);

        yield return new WaitForSeconds(Config.blackjackResultDelay);

        winningOutcomeLabel.text = _payoutInfoText; // back to the payout table, WIN badge stays until next deal
        _game.Reset();
        _resolving = false;
        Refresh();
    }

    // ---------- App lifecycle ----------

    public override void OnAppOpened()
    {
        EnsureInitialized();
        HintService.Instance?.ShowOnce(HintId.BlackjackOpen);
        Refresh();
    }

    public override void OnAppClosed()
    {
        _streak = 0; // closing ends the hot streak, mid-hand forfeit included
        if (_game == null) return; // never opened, nothing to forfeit

        // Closing the app mid-hand FORFEITS the ante and the hand.
        // Intentional: the player already paid, and letting them park a
        // live hand while they mine would break the lockout.
        bool idle = _game.State == HandState.Idle && !_resolving;
        if (!idle)
        {
            StopAllCoroutines();
            _game.Reset();
            _resolving = false;
            HideWinBadges();
            winningOutcomeLabel.text = _payoutInfoText;
        }
        if (streakLabel != null) RefreshStreak();
    }

    // ---------- UI ----------

    private void RefreshStreak()
    {
        if (streakLabel == null) return;
        streakLabel.gameObject.SetActive(_streak >= 1);
        if (_streak >= 1)
            streakLabel.text = $"Hot Table x{_streak}  +{Mathf.RoundToInt(StreakBonus(Config, _streak) * 100f)}% next win";
    }

    private void Refresh()
    {
        RefreshStreak();
        bool idle = _game.State == HandState.Idle && !_resolving;
        bool playing = _game.State == HandState.PlayerTurn && !_resolving;

        dealButton.gameObject.SetActive(idle);
        hitButton.gameObject.SetActive(playing);
        standButton.gameObject.SetActive(playing);
        doubleButton.gameObject.SetActive(playing);

        dealButton.interactable   = idle && Available;
        hitButton.interactable    = playing && _game.CanHit;
        standButton.interactable  = playing;
        doubleButton.interactable = playing && _game.CanDouble;

        anteLabel.text = Banned
            ? "CONNECTION LOST"
            : $"ANTE {CurrentAnte:0}s"; 
        doubleButtonLabel.text = $"DOUBLE -{CurrentAnte:0}s";

        spentLabel.text = $"Spent {_totalSpent:0}s";
        receivedLabel.text = $"Received ${_totalReceived}";

        playerRow.Render(_game.PlayerHand, hideIndex: -1);
        dealerRow.Render(_game.DealerHand, hideIndex: _game.DealerHoleHidden ? 1 : -1);

        playerValueLabel.text = _game.PlayerHand.Count == 0
            ? ""
            : "YOU " + (BlackjackGame.IsSoft(_game.PlayerHand) ? "soft " : "") + _game.PlayerValue;

        dealerValueLabel.text = _game.DealerHand.Count == 0
            ? ""
            : "DEALER " + (_game.DealerHoleHidden ? _game.DealerUpValue + " + ?" : _game.DealerValue.ToString());
    }

    private void HideWinBadges()
    {
        playerWinLabel.SetActive(false);
        dealerWinLabel.SetActive(false);
    }

    /// <summary>Flashes a warning where the payout table normally sits.</summary>
    private void Flash(string message) => winningOutcomeLabel.text = message;
}
