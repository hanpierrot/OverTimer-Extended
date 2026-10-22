using UnityEngine;

[CreateAssetMenu(fileName = "Overtime", menuName = "Overtime/Cards/Effects/Overtime")]
public class OvertimeCardSO : EffectCardSO
{
    [SerializeField] private float payoutMultiplier = 2f;

    // The card is the owner of the queued effect: MiningApp ends it when the next vein is banked, and OnRemoved
    // withdraws an effect that was never used (pawned, or the scene reloaded on Restart).
    public override void OnPlaced(CardItem card) => MiningApp.QueueNextVeinMultiplier(payoutMultiplier, card);

    public override void OnRemoved(CardItem card) => MiningApp.WithdrawNextVeinEffects(card);
}
