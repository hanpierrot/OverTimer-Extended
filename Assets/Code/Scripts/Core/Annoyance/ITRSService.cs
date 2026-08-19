using System;
using UnityEngine;

/// <summary>
/// Internal Time Revenue Service (DESIGN.md §5.6). Assesses a cut of money earned
/// since the last bill on a fixed real-time interval, then holds - it does not
/// auto-resolve. The player must Pay() or Ignore() (via the Tax panel) before another
/// annoyance can fire; Ignore converts the full amount due into a clock penalty.
/// </summary>
public class ITRSService : MonoBehaviour
{
    public static ITRSService Instance { get; private set; }

    /// <summary>A bill awaiting the player's Pay/Ignore decision - Taxes/Tax panel UI hooks here.</summary>
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

    public bool HasPendingAssessment { get; private set; }
    public Assessment CurrentAssessment { get; private set; }

    private float timer;
    private int earnedSinceLastBill;
    private int lastKnownMoney;
    private bool skipNextBill;

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
        if (delta > 0) earnedSinceLastBill += delta;
    }

    private void Update()
    {
        if (!CountdownTimer.Instance.IsRunning || CountdownTimer.Instance.IsGameOver) return;

        timer += Time.deltaTime;
        if (timer >= GameManager.Instance.GameConfig.billInterval)
        {
            timer = 0f;
            TryIssueAssessment();
        }
    }

    private void TryIssueAssessment()
    {
        if (skipNextBill)
        {
            skipNextBill = false;
            earnedSinceLastBill = 0;
            return;
        }

        // Also covers "a notice is already pending" - AnnoyanceManager stays busy
        // until Pay/Ignore resolves it, so this naturally retries instead of piling up.
        if (AnnoyanceManager.Instance != null && !AnnoyanceManager.Instance.TryBegin("itrs"))
        {
            timer = GameManager.Instance.GameConfig.billInterval - 1f; // retry in ~1s rather than dropping the bill
            return;
        }

        IssueAssessment();
    }

    private void IssueAssessment()
    {
        int earned = earnedSinceLastBill;
        earnedSinceLastBill = 0;

        int amountDue = Mathf.Max(GameManager.Instance.GameConfig.billMinimum, Mathf.RoundToInt(earned * GameManager.Instance.GameConfig.billRate));

        CurrentAssessment = new Assessment(earned, GameManager.Instance.GameConfig.billRate, GameManager.Instance.GameConfig.billMinimum, amountDue, GameManager.Instance.GameConfig.shortfallPenalty);
        HasPendingAssessment = true;

        OnAssessmentIssued?.Invoke(CurrentAssessment);
    }

    /// <summary>Pay button - fails (no-op) if the player can't afford it.</summary>
    public bool Pay()
    {
        if (!HasPendingAssessment) return false;
        if (!MoneyService.Instance.TrySpend(CurrentAssessment.amountDue, "ITRS bill")) return false;

        Resolve();
        return true;
    }

    /// <summary>Ignore button - the full amount due converts straight into a clock penalty.</summary>
    public void Ignore()
    {
        if (!HasPendingAssessment) return;

        CountdownTimer.Instance.Spend(CurrentAssessment.amountDue * CurrentAssessment.penaltyPerDollar, "ITRS shortfall");
        Resolve();
    }

    private void Resolve()
    {
        HasPendingAssessment = false;
        AnnoyanceManager.Instance?.End("itrs");
    }

    /// <summary>Tax Exemption buff - the next scheduled bill is skipped entirely.</summary>
    public void SkipNextBill() => skipNextBill = true;

    /// <summary>Audit debuff - a new assessment lands immediately, bypassing the schedule.</summary>
    public void ForceBillNow()
    {
        if (HasPendingAssessment) return; // don't stomp a notice already awaiting the player
        timer = 0f;
        IssueAssessment();
    }
}
