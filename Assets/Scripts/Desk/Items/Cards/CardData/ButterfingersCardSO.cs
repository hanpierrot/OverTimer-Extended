using UnityEngine;

[CreateAssetMenu(fileName = "Butterfingers", menuName = "Overtime/Cards/Effects/Butterfingers")]
public class ButterfingersCardSO : EffectCardSO
{
    // Owned effect: MiningApp ends the card when the next vein slips, and OnRemoved withdraws an unused slip.
    public override void OnPlaced(CardItem card) => MiningApp.QueueNextVeinSlip(card);

    public override void OnRemoved(CardItem card) => MiningApp.WithdrawNextVeinEffects(card);
}
