using System.Collections.Generic;
using UnityEngine;

public class CostModifierService : MonoBehaviour
{
    public static CostModifierService Instance { get; private set; }
<<<<<<< HEAD

    // Append only: enums serialize as integers, so inserting a value would silently re-point existing card assets.
    public enum Target { Ticket, CardPack, Billiards }

    private float _ticketMultiplier = 1f;
    private float _cardPackMultiplier = 1f;
    private float _billiardsMultiplier = 1f;

=======
    
    public enum Target { Ticket, CardPack, Blackjack, Billiards }
    
    private readonly Dictionary<Target, float> _multipliers = new();
    
>>>>>>> main
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

<<<<<<< HEAD
    public float GetMultiplier(Target target) => target switch
    {
        Target.Ticket => _ticketMultiplier,
        Target.CardPack => _cardPackMultiplier,
        Target.Billiards => _billiardsMultiplier,
        _ => 1f
    };

    public void SetMultiplier(Target target, float value)
    {
        switch (target)
        {
            case Target.Ticket: _ticketMultiplier = value; break;
            case Target.CardPack: _cardPackMultiplier = value; break;
            case Target.Billiards: _billiardsMultiplier = value; break;
        }
    }
=======
    public float GetMultiplier(Target target) =>
        _multipliers.TryGetValue(target, out float value) ? value : 1f;

    public void SetMultiplier(Target target, float value) => _multipliers[target] = value;
>>>>>>> main
}
