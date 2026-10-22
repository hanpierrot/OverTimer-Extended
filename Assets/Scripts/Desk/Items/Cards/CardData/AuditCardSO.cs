using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Audit", menuName = "Overtime/Cards/Effects/Audit")]
public class AuditCardSO : EffectCardSO
{
    // One handler per placed card. The asset is shared by every Audit card, and CardItems are pooled, so the
    // handler must be removed in OnRemoved: otherwise it would later end whatever card reuses that CardItem.
    private readonly Dictionary<CardItem, Action> _handlers = new Dictionary<CardItem, Action>();

    public override void OnPlaced(CardItem card)
    {
        if (ITRSService.Instance == null) return;

        if (ITRSService.Instance.HasPendingAssessment)
            ITRSService.Instance.DoubleCurrentBill();
        else
            ITRSService.Instance.ForceBillNow();

        Detach(card);

        Action handler = null;
        handler = () =>
        {
            if (ITRSService.Instance != null) ITRSService.Instance.OnAssessmentResolved -= handler;
            _handlers.Remove(card);
            card.EndEffect();
        };
        _handlers[card] = handler;
        ITRSService.Instance.OnAssessmentResolved += handler;
    }

    public override void OnRemoved(CardItem card) => Detach(card);

    private void Detach(CardItem card)
    {
        if (!_handlers.TryGetValue(card, out Action handler)) return;

        _handlers.Remove(card);
        if (ITRSService.Instance != null) ITRSService.Instance.OnAssessmentResolved -= handler;
    }
}
