using UnityEngine;

[CreateAssetMenu(fileName = "TaxExemption", menuName = "Overtime/Cards/Effects/Tax Exemption")]
public class TaxExemptionCardSO : EffectCardSO
{
    public override void OnPlaced(CardItem card)
    {
        if (ITRSService.Instance == null) return;
        
        ITRSService.Instance.SkipNextBill();

        void HandleResolved()
        {
            ITRSService.Instance.OnAssessmentResolved -= HandleResolved;
            card.EndEffect();
        }
        ITRSService.Instance.OnAssessmentResolved += HandleResolved;
    }
    
    public override void OnRemoved(CardItem card) { }
}
