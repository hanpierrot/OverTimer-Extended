using System.Collections.Generic;
using UnityEngine;

public class CostModifierService : MonoBehaviour
{
    public static CostModifierService Instance { get; private set; }
    
    public enum Target { Ticket, CardPack, Blackjack, Billiards }
    
    private readonly Dictionary<Target, float> _multipliers = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public float GetMultiplier(Target target) =>
        _multipliers.TryGetValue(target, out float value) ? value : 1f;

    public void SetMultiplier(Target target, float value) => _multipliers[target] = value;
}
