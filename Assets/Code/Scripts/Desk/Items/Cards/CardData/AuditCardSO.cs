using UnityEngine;

[CreateAssetMenu(fileName = "Audit", menuName = "Overtime/Cards/Effects/Audit")]
public class AuditCardSO : EffectCardSO
{
    public override void OnPlaced(CardItem card)
    {
        if (ITRSService.Instance == null) return;
        
        if (ITRSService.Instance.HasPendingAssessment)
            ITRSService.Instance.DoubleCurrentBill();
        else
            ITRSService.Instance.ForceBillNow();

        void HandleResolved()
        {
            ITRSService.Instance.OnAssessmentResolved -= HandleResolved;
            card.EndEffect();
        }
        ITRSService.Instance.OnAssessmentResolved += HandleResolved;
    }
    
    public override void OnRemoved(CardItem card) { }
}
