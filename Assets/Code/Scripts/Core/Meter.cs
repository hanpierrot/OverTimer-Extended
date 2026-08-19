using System;
using TMPro;
using UnityEngine;

/// <summary>
/// The ONLY money -> clock time converter in the game (DESIGN.md I3).
/// $1 = 1s via GameConfig.exchangeRate. Nothing else may add clock seconds
/// from money - route new systems through this instead of CountdownTimer directly.
/// </summary>
[RequireComponent(typeof(PointerReceiver))]
[RequireComponent(typeof(Collider2D))]
public class Meter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    
    private PointerReceiver receiver;
    
    private void Awake()
    {
        receiver = GetComponent<PointerReceiver>();
    }

    private void Start()
    {
        UpdateDisplay(MoneyService.Instance.Current);
    }
    
    private void OnEnable()
    {
        receiver.ClickDown += HandleClick;
        MoneyService.Instance.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDisable()
    {
        receiver.ClickDown -= HandleClick;
        if (MoneyService.Instance != null)
            MoneyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }
    
    private void HandleClick(Vector2 worldPos)
    {
        GameConfig config = GameManager.Instance.GameConfig;
        int current = MoneyService.Instance.Current;

        if (current < config.meterMinFeed) return;

        if (MoneyService.Instance.TrySpend(current, "meter"))
        {
            float seconds = current * config.exchangeRate;
            CountdownTimer.Instance.AddSeconds(seconds, "meter");
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
