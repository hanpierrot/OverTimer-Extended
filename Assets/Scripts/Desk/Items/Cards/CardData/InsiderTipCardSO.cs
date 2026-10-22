using UnityEngine;

[CreateAssetMenu(fileName = "InsiderTip", menuName = "Overtime/Cards/Effects/Insider Tip")]
public class InsiderTipCardSO : EffectCardSO
{
    // Owned effect: ScratchTicket ends the card when the revealed window is delivered, and OnRemoved withdraws an unused tip.
    public override void OnPlaced(CardItem card) => ScratchTicket.QueuePreRevealWindow(card);

    public override void OnRemoved(CardItem card) => ScratchTicket.WithdrawPreRevealWindow(card);
}
