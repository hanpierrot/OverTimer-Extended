using UnityEngine;

[CreateAssetMenu(fileName = "TaskCostModifier", menuName = "Overtime/Cards/Effects/Task Cost Modifier")]
public class TaskCostModifierCardSO : EffectCardSO
{
    [SerializeField] private CostModifierService.Target target;
    [SerializeField] private float costMultiplier = 1.5f;
    [SerializeField] private float duration = 20f;

    // Blackjack and Billiards prices only matter once the app is licensed, so those cards are not rolled before that.
    public override bool IsAvailable
    {
        get
        {
            string app = target == CostModifierService.Target.Blackjack ? "Blackjack"
                       : target == CostModifierService.Target.Billiards ? "Billiards"
                       : null;
            return app == null || ShopService.Instance == null || ShopService.Instance.IconGate.IsLicensed(app);
        }
    }

    public override void OnPlaced(CardItem card)
    {
        if (CostModifierService.Instance != null) CostModifierService.Instance.SetMultiplier(target, card, costMultiplier);
        card.ScheduleAutoRemove(duration);
    }
    
    public override void OnRemoved(CardItem card)
    {
        if (CostModifierService.Instance != null) CostModifierService.Instance.ClearMultiplier(target, card);
    }
}
