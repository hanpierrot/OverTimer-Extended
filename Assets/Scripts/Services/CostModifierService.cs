using System.Collections.Generic;
using UnityEngine;

public class CostModifierService : MonoBehaviour
{
    public static CostModifierService Instance { get; private set; }

    public enum Target { Ticket, CardPack, Blackjack, Billiards }
    
    // One multiplier per (target, key). Two cards on the same target both count and each one ends on its own, so the first
    // expiry no longer resets the other. The multiplier of a target is the product of its keys.
    private readonly Dictionary<Target, Dictionary<object, float>> _multipliers = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public float GetMultiplier(Target target)
    {
        if (!_multipliers.TryGetValue(target, out var keyed)) return 1f;

        float product = 1f;
        foreach (float value in keyed.Values) product *= value;
        return product;
    }

    /// <summary>Registers the multiplier of `key` (a card) on a target. The same key again replaces its own value.</summary>
    public void SetMultiplier(Target target, object key, float value)
    {
        if (key == null) return;

        if (!_multipliers.TryGetValue(target, out var keyed))
            _multipliers[target] = keyed = new Dictionary<object, float>();
        keyed[key] = value;
    }

    /// <summary>Removes the multiplier of `key`. Removing a key that is not there does nothing.</summary>
    public void ClearMultiplier(Target target, object key)
    {
        if (key != null && _multipliers.TryGetValue(target, out var keyed)) keyed.Remove(key);
    }
}
