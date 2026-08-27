using UnityEngine;

[CreateAssetMenu(fileName = "DoNotDisturb", menuName = "Overtime/Cards/Effects/Do Not Disturb")]
public class DoNotDisturbCardSO : EffectCardSO
{
    [SerializeField] private float duration = 30f;

    public override void OnPlaced(CardItem card)
    {
        if (Phone.Instance != null) Phone.Instance.Silence(duration);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (Phone.Instance != null) Phone.Instance.Unsilence();
    }
}
