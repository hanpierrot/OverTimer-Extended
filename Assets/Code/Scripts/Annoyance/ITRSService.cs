using System;
using UnityEngine;

/// <summary>
/// Internal Time Revenue Service (DESIGN.md §5.6). Assesses a cut of money earned since the last bill every
/// billInterval seconds of running clock, but never while a bill is still unpaid (P6). Each bill carries a TaxDeadline:
/// taxPayWindow seconds of running clock to pay it, in the Taxes app or on the desk paper copy (paying either resolves
/// both). Unpaid at 0, it costs amount x shortfallPenalty seconds of clock, once. There is no Ignore button (Q1).
/// </summary>
public class ITRSService : MonoBehaviour
{
    public static ITRSService Instance { get; private set; }

    /// <summary>A bill awaiting the player - the Taxes app and the desk Tax panel hook here.</summary>
    public readonly struct Assessment
    {
        public readonly int earnedSinceNotice;
        public readonly float rate;
        public readonly int minimum;
        public readonly int amountDue;
        public readonly float penaltyPerDollar;

        public Assessment(int earnedSinceNotice, float rate, int minimum, int amountDue, float penaltyPerDollar)
        {
            this.earnedSinceNotice = earnedSinceNotice;
            this.rate = rate;
            this.minimum = minimum;
            this.amountDue = amountDue;
            this.penaltyPerDollar = penaltyPerDollar;
        }
    }

    /// <summary>Fired when a new bill needs the player's attention.</summary>
    public event Action<Assessment> OnAssessmentIssued;
    public event Action OnAssessmentResolved;
    /// <summary>Fired when the open bill's amount or time changes (Audit doubling it, a payment plan).</summary>
    public event Action AssessmentChanged;

    public bool HasPendingAssessment { get; private set; }
    public Assessment CurrentAssessment { get; private set; }
    /// <summary>The amount to pay right now (an Audit card can double it after issue).</summary>
    public int CurrentAmountDue => _deadline != null ? _deadline.Amount : 0;
    /// <summary>Running-clock seconds left to pay the open bill.</summary>
    public float SecondsLeft => _deadline != null ? _deadline.SecondsLeft : 0f;

    private float timer;
    private TaxDeadline _deadline;
    private int earnedSinceLastBill;
    private int lastKnownMoney;
    private bool skipNextBill;
    private readonly System.Collections.Generic.List<CardItem> skipCards = new System.Collections.Generic.List<CardItem>();
    private CardItem[] exemptCards = System.Array.Empty<CardItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Not OnEnable: Awake order across scripts isn't guaranteed (see HANDOFF.md §3),
        // so MoneyService.Instance can still be null there. Start() only runs after
        // every object's Awake has completed, so the singleton is always ready here.
        lastKnownMoney = MoneyService.Instance.Current;
        MoneyService.Instance.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDestroy()
    {
        if (MoneyService.Instance != null) MoneyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int current, string reason)
    {
        int delta = current - lastKnownMoney;
        lastKnownMoney = current;
        if (delta > 0 && reason != MoneyService.StakeRefundReason) earnedSinceLastBill += delta;
    }

    private void Update()
    {
        float gameDelta = ClockService.Instance.GameDelta;     // 0 while the clock is paused (a call) or the game is over
        if (gameDelta <= 0f) return;

        if (_deadline != null) _deadline.Tick(gameDelta);       // may resolve the bill (penalty)
        if (HasPendingAssessment) return;                        // P6: never issue over an unpaid bill

        timer += gameDelta;
        if (timer >= GameManager.Instance.GameConfig.billInterval)
        {
            timer = 0f;
            IssueAssessment();
        }
    }

    private void IssueAssessment()
    {
        var config = GameManager.Instance.GameConfig;
        int earned = earnedSinceLastBill;
        earnedSinceLastBill = 0;

        int amountDue;
        if (skipNextBill)
        {
            skipNextBill = false;
            amountDue = 0;
            exemptCards = skipCards.ToArray();                   // this bill is the exemption: resolving it ends these cards
            skipCards.Clear();
        }
        else
        {
            amountDue = Mathf.Max(config.billMinimum, Mathf.RoundToInt(earned * config.billRate));
        }

        CurrentAssessment = new Assessment(earned, config.billRate, config.billMinimum, amountDue, config.shortfallPenalty);
        HasPendingAssessment = true;
        _deadline = new TaxDeadline(amountDue, config.taxPayWindow);
        _deadline.Resolved += HandleDeadlineResolved;

        var phone = PhoneController.Instance;
        if (phone != null)
            phone.Inbox.Add(MessageKind.Tax, $"Tax bill ${amountDue}", $"Pay within {config.taxPayWindow:0} seconds.", "app:taxes");

        OnAssessmentIssued?.Invoke(CurrentAssessment);
    }

    /// <summary>Pay button (Taxes app or desk notice) - fails (no-op) if the player can't afford it.</summary>
    public bool Pay()
    {
        if (!HasPendingAssessment || _deadline == null) return false;
        int amount = _deadline.Amount;
        if (amount > 0 && !MoneyService.Instance.TrySpend(amount, "ITRS bill"))
        {
            if (HintService.Instance != null) HintService.Instance.Show(HintId.TaxCannotAfford);
            return false;
        }

        _deadline.MarkPaid();                                    // -> HandleDeadlineResolved
        return true;
    }

    /// <summary>Unpaid at 0 s: the full amount converts into a clock penalty, once.</summary>
    private void HandleDeadlineResolved(TaxDeadline deadline)
    {
        if (deadline.State == TaxDeadline.Outcome.Penalized && deadline.Amount > 0)
            ClockService.Instance.Spend(deadline.Amount * CurrentAssessment.penaltyPerDollar, "ITRS shortfall");

        _deadline = null;
        Resolve();
    }

    private void Resolve()
    {
        HasPendingAssessment = false;
        OnAssessmentResolved?.Invoke();

        var exempt = exemptCards;
        exemptCards = System.Array.Empty<CardItem>();
        foreach (var card in exempt) if (card != null) card.EndEffect();
    }

    /// <summary>ITRS "payment plan" call: more time to pay the open bill.</summary>
    public void ExtendCurrentBill(float seconds)
    {
        if (_deadline == null) return;
        _deadline.Extend(seconds);
        AssessmentChanged?.Invoke();
    }

    /// <summary>Tax Exemption buff - the next scheduled bill is skipped entirely. The card that queued it is ended when
    /// that exempt bill is resolved; a card that goes away first withdraws (pawned), so the exemption goes with it.</summary>
    public void SkipNextBill(CardItem owner = null)
    {
        skipNextBill = true;
        if (owner != null && !skipCards.Contains(owner)) skipCards.Add(owner);
    }

    public void WithdrawSkipNextBill(CardItem owner)
    {
        if (owner == null) return;
        if (skipCards.Remove(owner) && skipCards.Count == 0) skipNextBill = false;
    }

    /// <summary>Audit debuff - a new assessment lands immediately, bypassing the schedule.</summary>
    public void ForceBillNow()
    {
        if (HasPendingAssessment) return; // don't stomp a notice already awaiting the player
        timer = 0f;
        IssueAssessment();
    }

    public void DoubleCurrentBill()
    {
        if (!HasPendingAssessment || _deadline == null) return;

        _deadline.Double();
        var a = CurrentAssessment;
        CurrentAssessment = new Assessment(a.earnedSinceNotice, a.rate, a.minimum, _deadline.Amount, a.penaltyPerDollar);
        AssessmentChanged?.Invoke();                             // open panels refresh (fixes the stale Audit amount)
    }
}
