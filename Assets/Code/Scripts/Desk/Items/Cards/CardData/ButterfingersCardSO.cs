using UnityEngine;

[CreateAssetMenu(fileName = "Butterfingers", menuName = "Overtime/Cards/Effects/Butterfingers")]
public class ButterfingersCardSO : EffectCardSO
{
    public override void OnPlaced(CardItem card)
    {
        MiningApp.QueueNextVeinSlip();

        void HandleResolved()
        {
            MiningApp.OnNextVeinEffectResolved -= HandleResolved;
            card.EndEffect();
        }
        MiningApp.OnNextVeinEffectResolved += HandleResolved;
    }

    public override void OnRemoved(CardItem card) { }
}
