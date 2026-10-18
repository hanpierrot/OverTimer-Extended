using UnityEngine;

/// <summary>
/// Cable Eater (the asset keeps its old name "CableEater": the Collection saves unlocks by asset name).
/// The phone is forced off for the duration and its power button is dead. When the card ends the phone stays off
/// until the player boots it. Pawning the card early frees the phone straight away.
/// </summary>
[CreateAssetMenu(fileName = "DoNotDisturb", menuName = "Overtime/Cards/Effects/Do Not Disturb")]
public class DoNotDisturbCardSO : EffectCardSO
{
    [SerializeField] private float duration = 30f;

    public override void OnPlaced(CardItem card)
    {
        if (PhoneController.Instance != null) PhoneController.Instance.ForceOff(card);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (PhoneController.Instance != null) PhoneController.Instance.ReleaseForceOff(card);
    }
}
