using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The ONLY money -> clock time converter in the game (DESIGN.md I3).
/// $1 = 1s via GameConfig.exchangeRate. Nothing else may add clock seconds
/// from money - route new systems through this instead of CountdownTimer directly.
/// </summary>
public class Meter : MonoBehaviour
{
    public static Meter Instance { get; private set; } 
    
    [SerializeField] private Button feedButton;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    public bool IsLocked { get; private set; }
    public void SetLocked(bool locked) => IsLocked = locked;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        feedButton.onClick.AddListener(HandleFeedClicked);
    }

    private void Start()
    {
        // Read in Start, not OnEnable: the order of OnEnable between the Meter and MoneyService is not guaranteed.
        MoneyService.Instance.OnMoneyChanged += HandleMoneyChanged;
        UpdateDisplay(MoneyService.Instance.Current);
    }

    private void OnDestroy()
    {
        if (MoneyService.Instance != null)
            MoneyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
        if (Instance == this) Instance = null;
    }

    private void HandleFeedClicked()
    {
        // The Meter sits above most panels, so it stays clickable while the CardPanel freezes game time.
        // The player never acts while game time is frozen (I5).
        if (Time.timeScale == 0f) return;

        if (IsLocked)
        {
            HintService.Instance?.Show(HintId.MeterLocked);
            return;
        }

        GameConfig config = GameManager.Instance.GameConfig;
        int current = MoneyService.Instance.Current;

        if (current < config.meterMinFeed)
        {
            HintService.Instance?.Show(HintId.MeterMinFeed, config.meterMinFeed);
            return;
        }

        if (MoneyService.Instance.TrySpend(current, "meter"))
        {
            float seconds = current * config.exchangeRate;
            ClockService.Instance.AddSeconds(seconds, "meter");
        }
    }

    private void HandleMoneyChanged(int newAmount, string reason)
    {
        UpdateDisplay(newAmount);
    }

    private void UpdateDisplay(int amount)
    {
        if (moneyText == null) return;
        moneyText.text = $"${amount}";
    }
}
