using UnityEngine;

/// <summary>
/// Cable Eater (the asset keeps its old name "CableEater": the Collection saves unlocks by asset name).
/// The phone is forced off for the duration and its power button is dead; the Laptop is locked too (LaptopLock).
/// When the card ends the phone stays off until the player boots it. Pawning the card early frees both straight away.
/// </summary>
[CreateAssetMenu(fileName = "CableEater", menuName = "Overtime/Cards/Effects/Cable Eater")]
public class CableEaterCardSo : EffectCardSO
{
    [SerializeField] private float duration = 30f;

    public override void OnPlaced(CardItem card)
    {
        if (PhoneController.Instance != null) PhoneController.Instance.ForceOff(card);
        LaptopLock.Add(card);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (PhoneController.Instance != null) PhoneController.Instance.ReleaseForceOff(card);
        LaptopLock.Remove(card);
    }
}
