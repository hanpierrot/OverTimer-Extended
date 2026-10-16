using UnityEngine;

/// Trick Shot (buff): on the next shot, while aiming, faint balls show where every ball will end up. The card waits
/// until a shot is fired, then ends itself. Pawning it first withdraws the effect.
[CreateAssetMenu(fileName = "BilliardsTrickShot", menuName = "Overtime/Cards/Effects/Billiards Trick Shot")]
public class BilliardsTrickShotCardSO : EffectCardSO
{
    public override void OnPlaced(CardItem card) => BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.TrickShot, 1f, card);

    public override void OnRemoved(CardItem card) => BilliardsPendingEffects.Withdraw(card);
}
