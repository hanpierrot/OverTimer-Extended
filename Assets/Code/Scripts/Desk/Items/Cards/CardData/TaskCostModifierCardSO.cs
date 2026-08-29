using UnityEngine;

[CreateAssetMenu(fileName = "TaskCostModifier", menuName = "Overtime/Cards/Effects/Task Cost Modifier")]
public class TaskCostModifierCardSO : EffectCardSO
{
    [SerializeField] private CostModifierService.Target target;
    [SerializeField] private float costMultiplier = 1.5f;
    [SerializeField] private float duration = 20f;

    public override void OnPlaced(CardItem card)
    {
        if (CostModifierService.Instance != null) CostModifierService.Instance.SetMultiplier(target, costMultiplier);
        card.ScheduleAutoRemove(duration);
    }
    
    public override void OnRemoved(CardItem card)
    {
        if (CostModifierService.Instance != null) CostModifierService.Instance.SetMultiplier(target, 1f);
    }
}
