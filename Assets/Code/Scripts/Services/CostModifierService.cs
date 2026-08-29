using UnityEngine;

public class CostModifierService : MonoBehaviour
{
    public static CostModifierService Instance { get; private set; }
    
    public enum Target { Ticket, CardPack }

    private float _ticketMultiplier = 1f;
    private float _cardPackMultiplier = 1f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public float GetMultiplier(Target target) => target switch
    {
        Target.Ticket => _ticketMultiplier,
        Target.CardPack => _cardPackMultiplier,
        _ => 1f
    };

    public void SetMultiplier(Target target, float value)
    {
        switch (target)
        {
            case Target.Ticket: _ticketMultiplier = value; break;
            case Target.CardPack: _cardPackMultiplier = value; break;
        }
    }
}
