using UnityEngine;

[CreateAssetMenu(fileName = "TaxExemption", menuName = "Overtime/Cards/Effects/Tax Exemption")]
public class TaxExemptionCardSO : EffectCardSO
{
    // Owned effect: ITRSService ends the card when the exempt (0$) bill is resolved, and OnRemoved withdraws an unused exemption.
    public override void OnPlaced(CardItem card)
    {
        if (ITRSService.Instance != null) ITRSService.Instance.SkipNextBill(card);
    }

    public override void OnRemoved(CardItem card)
    {
        if (ITRSService.Instance != null) ITRSService.Instance.WithdrawSkipNextBill(card);
    }
}
