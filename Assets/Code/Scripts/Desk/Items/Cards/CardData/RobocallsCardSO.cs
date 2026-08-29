using UnityEngine;

[CreateAssetMenu(fileName = "Robocalls", menuName = "Overtime/Cards/Effects/Robocalls")]
public class RobocallsCardSO : EffectCardSO
{
    [SerializeField] private float duration = 30f;

    public override void OnPlaced(CardItem card)
    {
        if (Phone.Instance != null) Phone.Instance.StartRobocalls(duration);
        card.ScheduleAutoRemove(duration);
    }

    public override void OnRemoved(CardItem card)
    {
        if (Phone.Instance != null) Phone.Instance.StopRobocalls();
    }
}
