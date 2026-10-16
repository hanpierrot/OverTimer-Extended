using UnityEngine;

/// Cue Slip (debuff): the next shot's power is off by up to +-powerJitter (a random amount, drawn from the run's seeded
/// RngService BEFORE the shot is quantized, so the simulation still gets an exact, replayable input). The card waits
/// until a shot is fired, then ends itself. Pawning it first withdraws the effect. Mark the asset as a debuff.
[CreateAssetMenu(fileName = "BilliardsCueSlip", menuName = "Overtime/Cards/Effects/Billiards Cue Slip")]
public class BilliardsCueSlipCardSO : EffectCardSO
{
    [SerializeField, Range(0.01f, 0.5f)] private float powerJitter = 0.15f;

    public override void OnPlaced(CardItem card) => BilliardsPendingEffects.Add(BilliardsPendingEffects.Kind.CueSlip, powerJitter, card);

    public override void OnRemoved(CardItem card) => BilliardsPendingEffects.Withdraw(card);
}
