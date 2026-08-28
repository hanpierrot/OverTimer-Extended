using UnityEngine;

[CreateAssetMenu(fileName = "ClockSpeed", menuName = "Overtime/Cards/Effects/Clock Speed")]
public class ClockSpeedCardSO : EffectCardSO
{
    [SerializeField] private float duration = 15f;
    [SerializeField] private float tickMultiplier = 0.75f;
    
    public override void OnPlaced(CardItem card)
    {
        if (ClockService.Instance != null) ClockService.Instance.TickMultiplier = tickMultiplier;
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (ClockService.Instance != null) ClockService.Instance.TickMultiplier = 1f;
    }
}
