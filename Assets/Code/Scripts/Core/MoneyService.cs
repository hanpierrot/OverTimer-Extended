using System;
using UnityEngine;

/// <summary>
/// Owns the player's money. This is the only script allowed to change Current -
/// everything else calls Add/TrySpend (DESIGN.md §7.2).
/// </summary>
public class MoneyService : MonoBehaviour
{
    public static MoneyService Instance { get; private set; }
    
    public int Current { get; private set; }

    /// <summary>Fired after every change. reason feeds the balance log (DESIGN.md §7.2).</summary>
    public event Action<int, string> OnMoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Current = GameManager.Instance.GameConfig != null ? GameManager.Instance.GameConfig.startMoney : 0;
    }

    public void Add(int amount, string reason)
    {
        if (amount <= 0) return;

        Current += amount;
        OnMoneyChanged?.Invoke(Current, reason);
    }

    /// <summary>Returns false if unaffordable. Never lets a caller go negative (DESIGN.md §7.2).</summary>
    public bool TrySpend(int amount, string reason)
    {
        if (amount <= 0 || amount > Current) return false;

        Current -= amount;
        OnMoneyChanged?.Invoke(Current, reason);
        return true;
    }
}
