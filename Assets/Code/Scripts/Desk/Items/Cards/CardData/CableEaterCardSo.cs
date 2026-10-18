using UnityEngine;

[CreateAssetMenu(fileName = "CableEater", menuName = "Overtime/Cards/Effects/Cable Eater")]
public class CableEaterCardSo : EffectCardSO
{
    [SerializeField] private float duration = 30f;

    public override void OnPlaced(CardItem card)
    {
        if (Phone.Instance != null) Phone.Instance.Silence(duration);
        LaptopLock.Add(card);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (Phone.Instance != null) Phone.Instance.Unsilence();
        LaptopLock.Remove(card);
    }
}
