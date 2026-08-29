using UnityEngine;

[CreateAssetMenu(fileName = "InsiderTip", menuName = "Overtime/Cards/Effects/Insider Tip")]
public class InsiderTipCardSO : EffectCardSO
{
    public override void OnPlaced(CardItem card)
    {
        ScratchTicket.QueuePreRevealWindow();

        void HandleDelivered()
        {
            ScratchTicket.OnPreRevealDelivered -= HandleDelivered;
            card.EndEffect();
        }
        ScratchTicket.OnPreRevealDelivered += HandleDelivered;
    }

    public override void OnRemoved(CardItem card) { }
}
