using UnityEngine;

/// Chalk Up (buff): the next Billiards round starts with extra shots. The card waits on the desk until a round starts,
/// then ends itself (see BilliardsPendingEffects). Pawning it first withdraws the effect.
[CreateAssetMenu(fileName = "BilliardsExtraShot", menuName = "Overtime/Cards/Effects/Billiards Extra Shots")]
public class BilliardsExtraShotCardSO : EffectCardSO
{
    [SerializeField, Min(1)] private int extraShots = 1;

    public override void OnPlaced(CardItem card) => BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.ExtraShots, extraShots, card);

    public override void OnRemoved(CardItem card) => BilliardsPendingEffects.Withdraw(card);
}
