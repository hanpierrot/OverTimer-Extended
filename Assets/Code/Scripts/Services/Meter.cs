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
    [SerializeField] private Button feedButton;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    private void Awake()
    {
        feedButton.onClick.AddListener(HandleFeedClicked);
    }

    private void Start()
    {
        UpdateDisplay(MoneyService.Instance.Current);
    }
    
    private void OnEnable()
    {
        MoneyService.Instance.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDisable()
    {
        if (MoneyService.Instance != null)
            MoneyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleFeedClicked()
    {
        GameConfig config = GameManager.Instance.GameConfig;
        int current = MoneyService.Instance.Current;

        if (current < config.meterMinFeed) return;

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
