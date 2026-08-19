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
    [Header("Services")]
    [SerializeField] private GameConfig config;
    [SerializeField] private CountdownTimer clock;
    [SerializeField] private MoneyService money;
    [SerializeField] private RngService rng;

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

    private BlackjackGame _game;
    private bool _resolving;
    private float _totalSpent;
    private int _totalReceived;
    private string _payoutInfoText;

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

        _game = new BlackjackGame(rng.Random);

        dealButton.onClick.AddListener(OnDeal);
        hitButton.onClick.AddListener(OnHit);
        standButton.onClick.AddListener(OnStand);
        doubleButton.onClick.AddListener(OnDouble);

        // Static for the session - ante/payouts don't change mid-run.
        _payoutInfoText = $"win ${config.blackjackWinPayout} · push ${config.blackjackPushPayout} · bj ${config.blackjackNaturalPayout}";
        winningOutcomeLabel.text = _payoutInfoText;
        doubleButtonLabel.text = $"DOUBLE -{config.blackjackAnte:0}s";

        HideWinBadges();
    }

    // ---------- Safety rails ----------

    /// <summary>
    /// The ante must never be able to kill the player outright.
    /// Requires a margin above the ante so a loss is survivable.
    /// </summary>
    private bool CanAffordAnte =>
        clock.CurrentSeconds >= config.blackjackAnte + config.blackjackClockMargin;

    private bool Available => !Banned && !_resolving;

    // ---------- Actions ----------

    private void OnDeal()
    {
        if (!Available || _game.State != HandState.Idle) return;
        if (!CanAffordAnte)
        {
            Flash("NOT ENOUGH TIME");
            return;
        }

        clock.Spend(config.blackjackAnte, "blackjack ante");
        _totalSpent += config.blackjackAnte;
        HideWinBadges();
        winningOutcomeLabel.text = _payoutInfoText; // clear any lingering flash/warning
        _game.Deal();

        if (_game.State == HandState.Resolved)
            StartCoroutine(Resolve());   // natural on the deal

        Refresh();
    }

    private void OnHit()
    {
        if (!Available) return;

        _game.Hit();
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
            return;
        }

        clock.Spend(config.blackjackAnte, "blackjack double");
        _totalSpent += config.blackjackAnte;
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
        yield return new WaitForSeconds(config.blackjackRevealDelay);

        int payout = _game.Payout(
            config.blackjackWinPayout,
            config.blackjackPushPayout,
            config.blackjackNaturalPayout);

        if (payout > 0)
        {
            money.Add(payout, "blackjack");
            _totalReceived += payout;
        }

        winningOutcomeLabel.text = payout > 0 ? $"{_game.ResultText}  +${payout}" : _game.ResultText;

        // Push still pays out (a partial refund), so it reads as a player-side
        // outcome same as a win - only an outright dealer win or player bust
        // shows on the dealer's side.
        bool dealerSide = _game.Result is Outcome.DealerWin or Outcome.PlayerBust;
        playerWinLabel.SetActive(!dealerSide);
        dealerWinLabel.SetActive(dealerSide);

        yield return new WaitForSeconds(config.blackjackResultDelay);

        winningOutcomeLabel.text = _payoutInfoText; // back to the payout table, WIN badge stays until next deal
        _game.Reset();
        _resolving = false;
        Refresh();
    }

    // ---------- Debuff hook ----------

    /// <summary>Called by the card system. Blocks blackjack ONLY, never mining.</summary>
    public void ApplyBan(float seconds) => StartCoroutine(BanRoutine(seconds));

    private IEnumerator BanRoutine(float seconds)
    {
        Banned = true;
        Refresh();
        yield return new WaitForSeconds(seconds);
        Banned = false;
        Refresh();
    }

    // ---------- App lifecycle ----------

    public override void OnAppOpened()
    {
        EnsureInitialized();
        Refresh();
    }

    public override void OnAppClosed()
    {
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
    }

    // ---------- UI ----------

    private void Refresh()
    {
        bool idle = _game.State == HandState.Idle && !_resolving;
        bool playing = _game.State == HandState.PlayerTurn && !_resolving;

        dealButton.gameObject.SetActive(idle);
        hitButton.gameObject.SetActive(playing);
        standButton.gameObject.SetActive(playing);
        doubleButton.gameObject.SetActive(playing);

        dealButton.interactable   = idle && Available && CanAffordAnte;
        hitButton.interactable    = playing && _game.CanHit;
        standButton.interactable  = playing;
        doubleButton.interactable = playing && _game.CanDouble && CanAffordAnte;

        anteLabel.text = Banned
            ? "CONNECTION LOST"
            : $"ANTE {config.blackjackAnte:0}s";

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
